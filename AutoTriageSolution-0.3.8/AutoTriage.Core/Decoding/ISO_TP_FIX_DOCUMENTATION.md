# ISO-TP/UDS Decoder Architecture Fix

## Problem Statement

The AutoTriage UDS decoder was **mis-decoding** decimal CSV input like `"2,203,006,208"` as UDS diagnostic messages. This occurred because the decoder was attempting UDS parsing directly on raw bytes without first implementing the ISO-TP (ISO 15765-2) transport layer.

### Specific Failing Case

**Input:** `"2,203,006,208"` (decimal CSV format)

**Previous (WRONG) Behavior:**
- Converted to bytes: `[02, CB, 06, D0]`
- Attempted to decode `0x02` or `0xCB` as UDS Service IDs
- Incorrectly claimed this was a UDS diagnostic message

**Required (CORRECT) Behavior:**
- Convert to bytes: `[02, CB, 06, D0]`
- Parse as ISO-TP: Single Frame with PCI=0x02 (payload length 2)
- Extract payload: `[CB, 06]`
- Validate: `0xCB` is NOT a valid UDS SID
- Result: "ISO-TP frame with Non-UDS payload"

---

## Solution Architecture

The fix implements **proper protocol layering** following the automotive diagnostic stack:

```
┌────────────────────────────────────┐
│  Input String (Multiple Formats)  │
│  - Decimal CSV: "2,203,006,208"   │
│  - Hex bytes: "02 CB 06 D0"       │
│  - Binary: "00000010 11001011..." │
└────────────────────────────────────┘
                 ↓
┌────────────────────────────────────┐
│    ParseInputToBytes()             │
│    Normalizes input to byte[]      │
└────────────────────────────────────┘
                 ↓
┌────────────────────────────────────┐
│    ParseIsoTp()                    │
│    ISO 15765-2 Transport Layer     │
│    - Parses PCI byte               │
│    - Identifies frame type         │
│    - Extracts payload              │
└────────────────────────────────────┘
                 ↓
┌────────────────────────────────────┐
│    IsValidUdsSid()                 │
│    Validates payload[0] is UDS SID │
└────────────────────────────────────┘
                 ↓ (Only if valid SID)
┌────────────────────────────────────┐
│    TryDecodeAsUds()                │
│    ISO 14229 Application Layer     │
│    - Decodes UDS services          │
│    - Returns diagnostic info       │
└────────────────────────────────────┘
```

---

## Implementation Details

### 1. Input Parsing (`ParseInputToBytes`)

Handles multiple input formats deterministically:

```csharp
// Decimal CSV: "2,203,006,208" → [02, CB, 06, D0]
var result = ParseInputToBytes("2,203,006,208");

// Hex bytes with separators: "02 CB 06 D0" → [02, CB, 06, D0]
var result = ParseInputToBytes("02 CB 06 D0");

// Hex with 0x prefix: "0x02 0xCB 0x06 0xD0" → [02, CB, 06, D0]
var result = ParseInputToBytes("0x02 0xCB 0x06 0xD0");

// Continuous hex: "02CB06D0" → [02, CB, 06, D0]
var result = ParseInputToBytes("02CB06D0");
```

**Error Handling:**
- Invalid decimal values: "256" → Error: "exceeds byte range (0-255)"
- Invalid hex characters: "XY ZZ" → Error: "Invalid hex characters"
- Odd-length hex: "ABC" → Error: "not byte-aligned"

### 2. ISO-TP Frame Parsing (`ParseIsoTp`)

Implements ISO 15765-2 protocol specification:

#### Frame Type Detection (PCI Byte)

```
PCI Byte Format: [TTTT DDDD]
                 ↑    ↑
                 |    └─ Type-specific data (4 bits)
                 └────── Frame type (4 bits)
```

#### Supported Frame Types

**Single Frame (0x0n)**
```
Byte 0: 0x0n (n = payload length 0-7)
Bytes 1-n: Payload
Bytes n+1-end: Padding (ignored)

Example: [02 CB 06] → SF with 2-byte payload [CB 06]
```

**First Frame (0x1n)**
```
Byte 0-1: 0x1n LL (n + LL = total payload length)
Bytes 2-end: Initial payload data

Example: [10 14 49 02] → FF with 20-byte total payload
```

**Consecutive Frame (0x2n)**
```
Byte 0: 0x2n (n = sequence number 0-15)
Bytes 1-end: Continuation data

Example: [21 4A 54 33] → CF sequence 1 with data
```

**Flow Control (0x3n)**
```
Byte 0: 0x3n (n = flow status)
  0 = Continue to Send
  1 = Wait
  2 = Overflow
Byte 1: Block size
Byte 2: Separation time (ms)

Example: [30 00 00] → ContinueToSend
```

### 3. UDS SID Validation (`IsValidUdsSid`)

Before attempting UDS decode, validates that the first payload byte is a known UDS Service ID:

**Valid UDS SIDs:**
- **Request SIDs:** `0x10, 0x11, 0x14, 0x19, 0x22, 0x23, 0x24, 0x27, 0x28, 0x2A, 0x2C, 0x2E, 0x2F, 0x31, 0x34, 0x35, 0x36, 0x37, 0x38, 0x3D, 0x3E, 0x83, 0x84, 0x85, 0x86, 0x87`
- **Response SIDs:** Request + 0x40 (e.g., `0x50, 0x51, 0x62, 0x67`)
- **Negative Response:** `0x7F`

```csharp
// Example validations:
IsValidUdsSid(0x22) → true  (ReadDataByIdentifier request)
IsValidUdsSid(0x62) → true  (ReadDataByIdentifier response)
IsValidUdsSid(0x7F) → true  (Negative response)
IsValidUdsSid(0xCB) → false (Not a known UDS SID)
```

### 4. Protocol Layering Enforcement

```csharp
public DecodedPayload TryDecodeFromLine(string input)
{
    // Step 1: Parse input to bytes
    var parseResult = ParseInputToBytes(input);
    if (!parseResult.Success) 
        return Error(parseResult.Error);

    // Step 2: Parse ISO-TP frame
    var isoTpResult = ParseIsoTp(parseResult.Bytes);
    if (isoTpResult.FrameType == Invalid)
        return FallbackDecode(parseResult.Bytes);

    // Step 3: Check if Single Frame with complete payload
    if (isoTpResult.FrameType != SingleFrame || 
        isoTpResult.Confidence == "Partial")
    {
        return ConvertIsoTpResult(isoTpResult); // Multi-frame or incomplete
    }

    // Step 4: Validate UDS SID before attempting UDS decode
    byte firstByte = isoTpResult.Payload[0];
    if (!IsValidUdsSid(firstByte))
    {
        return NonUdsPayload(isoTpResult, firstByte); // ISO-TP but not UDS
    }

    // Step 5: Attempt UDS decode on validated payload
    var udsResult = TryDecodeAsUds(isoTpResult.Payload);
    return MergeIsoTpAndUdsResults(isoTpResult, udsResult);
}
```

---

## Testing

### Test Harness Usage

```csharp
using AutoTriage.Core.Decoding;

// Demonstrate the fix for the failing case
string demo = DecoderTestHarness.DemonstrateFix();
Console.WriteLine(demo);

// Run comparison tests (ISO-TP vs UDS)
string comparison = DecoderTestHarness.RunComparisonTests();
Console.WriteLine(comparison);

// Validate UDS still works correctly
string validation = DecoderTestHarness.ValidateUdsStillWorks();
Console.WriteLine(validation);
```

### Expected Test Results

#### Critical Fix Validation
```
Input: "2,203,006,208"
✅ PASS: Decoder correctly identified ISO-TP frame with non-UDS payload
✅ PASS: Did NOT mis-decode as UDS service 0x02 or 0xCB
✅ PASS: Properly validated SID before attempting UDS decode
```

#### ISO-TP Frame Tests
```
✅ ISO-TP Single Frame with non-UDS payload
✅ ISO-TP First Frame (multi-frame start)
✅ ISO-TP Consecutive Frame (continuation)
✅ ISO-TP Flow Control
```

#### UDS Validation Tests
```
✅ DiagnosticSessionControl (0x10/0x50)
✅ SecurityAccess (0x27/0x67)
✅ ReadDataByIdentifier (0x22/0x62)
✅ Negative Response (0x7F)
```

---

## API Changes

### New Public Methods

```csharp
// Explicit ISO-TP handling control
public DecodedPayload TryDecodeFromLine(string line, bool autoDetectIsoTp)

// Decode with ISO-TP control
public DecodedPayload TryDecodePayload(string payload, bool autoDetectIsoTp)
```

### New Result Type

```csharp
public class IsoTpResult
{
    public IsoTpFrameType FrameType { get; set; }
    public byte[] RawBytes { get; set; }
    public byte Pci { get; set; }
    public int PayloadLength { get; set; }
    public byte[] Payload { get; set; }
    public string Confidence { get; set; }
    public List<string> Reasons { get; set; }
    public string Summary { get; set; }
    // ... additional properties
}

public enum IsoTpFrameType
{
    Invalid,
    SingleFrame,
    FirstFrame,
    ConsecutiveFrame,
    FlowControl
}
```

### Output Changes

**Before (WRONG):**
```
Kind: UDS
Summary: ReadDataByIdentifier: DID 0xCB06
Confidence: Partial
```

**After (CORRECT):**
```
Kind: ISO-TP
Summary: ISO-TP Single Frame (2 byte payload) - Non-UDS payload
Confidence: Exact
Details:
  • ISO-TP Frame Type: SingleFrame
  • PCI Byte: 0x02
  • Payload length: 2 bytes
  • Extracted Payload: CB 06
Reasons:
  ⚠ Payload first byte 0xCB is not a valid UDS Service ID
```

---

## Backwards Compatibility

The decoder maintains full backwards compatibility:

1. **Default behavior unchanged:** `TryDecodeFromLine(line)` auto-detects ISO-TP
2. **Valid UDS messages still decode:** Messages with valid SIDs decode as before
3. **Fallback intact:** Non-ISO-TP inputs fall back to direct UDS/hex/ASCII decode
4. **Explicit control available:** New overloads allow disabling ISO-TP parsing

---

## Standards Compliance

- ✅ **ISO 15765-2:** Transport layer (ISO-TP framing)
- ✅ **ISO 14229:** Application layer (UDS diagnostic services)
- ✅ **Deterministic decoding:** No guessing; only proven facts
- ✅ **Clear confidence levels:** Exact, Partial, or Unknown
- ✅ **Transparent reasoning:** All limitations explained in `Reasons`

---

## Summary

This fix resolves the critical architecture flaw by implementing proper protocol layering:

1. ✅ Accepts decimal CSV format: `"2,203,006,208"`
2. ✅ Parses ISO-TP frames BEFORE attempting UDS decode
3. ✅ Validates UDS SIDs before claiming something is UDS
4. ✅ Clearly distinguishes ISO-TP frames from UDS messages
5. ✅ Provides transparent reasoning for all decoding decisions

The decoder now correctly handles the failing case while maintaining full compatibility with existing UDS message decoding.
