# Ford CAN Frame Format Support

## Overview

The AutoTriage UDS decoder now correctly handles Ford diagnostic log format, which includes CAN frame headers before the actual UDS message data.

---

## Problem Statement

Ford diagnostic logs include 4-byte CAN frame headers that were causing the decoder to misidentify service IDs:

**Example from logs:** `000007D87F2231`

**Previous (WRONG) behavior:**
- Decoder read byte 0 (0x00) as the service ID
- Failed to recognize the message as a Negative Response

**Required (CORRECT) behavior:**
- Strip bytes 0-3 (CAN frame header: `00 00 07 D8`)
- Read byte 4 (0x7F) as the service ID
- Correctly decode as Negative Response

---

## Ford CAN Frame Structure

```
┌─────────────────────────────────────────────────────────────┐
│  Complete Message: 000007D87F2231                          │
└─────────────────────────────────────────────────────────────┘
         ↓
┌──────────────────────┬──────────────────────────────────────┐
│  CAN Frame Header    │  UDS Message Payload                 │
│  (4 bytes)           │  (Variable length)                   │
├──────────────────────┼──────────────────────────────────────┤
│  00 00 07 D8         │  7F 22 31                            │
│  ↑    ↑  └──┬──┘     │  ↑  ↑  ↑                             │
│  │    │     │        │  │  │  └─ NRC Code (0x31)            │
│  │    │     │        │  │  └──── Original Service (0x22)    │
│  │    │     │        │  └─────── Service ID (0x7F)          │
│  │    │     └─────── CAN ID                                 │
│  │    └───────────── Reserved (always 0x00)                 │
│  └────────────────── Reserved (always 0x00)                 │
└──────────────────────┴──────────────────────────────────────┘
```

### CAN ID Meanings

| CAN ID | Hex   | Direction         | Description              |
|--------|-------|-------------------|--------------------------|
| 2000   | 0x07D0| Request to ECU    | Tester → ECU             |
| 2008   | 0x07D8| Response from ECU | ECU → Tester             |

The decoder also recognizes the diagnostic CAN ID range **0x07D0 - 0x07DF** as valid.

---

## Implementation Details

### CAN Frame Detection

The decoder automatically detects Ford CAN frame headers using this pattern:

```csharp
if (bytes[0] == 0x00 && bytes[1] == 0x00)
{
    ushort canId = (ushort)((bytes[2] << 8) | bytes[3]);
    
    // Check if CAN ID is in diagnostic range
    if (canId >= 0x07D0 && canId <= 0x07DF)
    {
        // Strip 4-byte header, decode remaining bytes as UDS
        byte[] udsPayload = bytes[4..];
    }
}
```

### Message Flow

```
Input String
    ↓
Parse to Bytes: [00, 00, 07, D8, 7F, 22, 31]
    ↓
Detect CAN Header: YES (0x00 0x00 prefix + valid CAN ID)
    ↓
Extract CAN ID: 0x07D8 (Response from ECU)
    ↓
Strip Header: [7F, 22, 31]
    ↓
Decode UDS: Negative Response
    ↓
Output with CAN metadata
```

---

## Example Messages

### Example 1: Negative Response from ECU

**Input:** `000007D87F2231`

**Structure:**
```
Bytes 0-3: 00 00 07 D8  → CAN ID 0x07D8 (Response from ECU)
Byte 4:    7F           → Service ID: Negative Response
Byte 5:    22           → Original service: ReadDataByIdentifier (0x22)
Byte 6:    31           → NRC: 0x31 (Request Out of Range)
```

**Decoder Output:**
```
Kind: UDS
Summary: Negative Response: ReadDataByIdentifier rejected with RequestOutOfRange
Confidence: Exact

Fields:
  CanID: 0x7D8
  Direction: Response from ECU
  MessageType: NegativeResponse
  SID: 0x7F
  RequestSID: 0x22
  RequestService: ReadDataByIdentifier
  NRC: 0x31
  NRCName: RequestOutOfRange

Details:
  • CAN Frame: CAN ID: 0x7D8 (Response from ECU)
  • Original request: ReadDataByIdentifier (0x22)
  • Negative Response Code: RequestOutOfRange (0x31)
```

### Example 2: ReadDataByIdentifier Request to ECU

**Input:** `000007D02201F190`

**Structure:**
```
Bytes 0-3: 00 00 07 D0  → CAN ID 0x07D0 (Request to ECU)
Byte 4:    22           → Service ID: ReadDataByIdentifier
Bytes 5-6: F1 90        → DID: 0xF190 (VIN)
```

**Decoder Output:**
```
Kind: UDS
Summary: ReadDataByIdentifier: DID 0xF190 (VIN)
Confidence: Exact

Fields:
  CanID: 0x7D0
  Direction: Request to ECU
  ServiceName: ReadDataByIdentifier
  MessageType: Request
  SID: 0x22
  DID: 0xF190
  DIDName: VIN

Details:
  • CAN Frame: CAN ID: 0x7D0 (Request to ECU)
  • DID: VIN (0xF190)
```

### Example 3: DiagnosticSessionControl Request

**Input:** `000007D01003`

**Structure:**
```
Bytes 0-3: 00 00 07 D0  → CAN ID 0x07D0 (Request to ECU)
Byte 4:    10           → Service ID: DiagnosticSessionControl
Byte 5:    03           → Session Type: ExtendedDiagnosticSession
```

**Decoder Output:**
```
Kind: UDS
Summary: DiagnosticSessionControl: ExtendedDiagnosticSession
Confidence: Exact

Fields:
  CanID: 0x7D0
  Direction: Request to ECU
  ServiceName: DiagnosticSessionControl
  MessageType: Request
  SID: 0x10
  SessionType: 0x03
  SessionName: ExtendedDiagnosticSession

Details:
  • CAN Frame: CAN ID: 0x7D0 (Request to ECU)
  • Session: ExtendedDiagnosticSession (0x03)
```

---

## Testing

### Test Harness Usage

```csharp
using AutoTriage.Core.Decoding;

// Demonstrate Ford CAN frame handling
string demo = DecoderTestHarness.DemonstrateFordCanFrame();
Console.WriteLine(demo);

// Run comparison tests including Ford format
string comparison = DecoderTestHarness.RunComparisonTests();
Console.WriteLine(comparison);
```

### Built-in Self Tests

The decoder includes automated tests for Ford CAN frame format:

```csharp
var decoder = new AutomotivePayloadDecoder();
var results = decoder.RunSelfTests();

// Tests include:
// - "Ford CAN Frame - Negative Response (000007D87F2231)"
// - "Ford CAN Frame - Request (000007D02201F190)"
```

---

## Technical Details

### CAN Frame Header Detection Logic

The decoder uses conservative detection to avoid false positives:

1. **Prefix Check:** First two bytes must be `0x00 0x00`
2. **CAN ID Range:** Bytes 2-3 must form a CAN ID in diagnostic range (0x07D0-0x07DF)
3. **Minimum Length:** Message must have at least 5 bytes (4-byte header + 1-byte payload)

### Integration with Protocol Stack

The CAN frame stripping happens **before** ISO-TP and UDS parsing:

```
Input String
    ↓
ParseInputToBytes()  ← Handles decimal CSV, hex, binary formats
    ↓
DecodeBytes()
    ↓
DetectAndStripCanHeader()  ← NEW: Strips Ford CAN frame header
    ↓
ParseIsoTp()  ← Handles ISO 15765-2 transport layer
    ↓
IsValidUdsSid()  ← Validates service ID
    ↓
TryDecodeAsUds()  ← Decodes ISO 14229 application layer
```

This ensures proper layering:
1. **CAN Layer:** Frame identification and routing
2. **Transport Layer:** ISO-TP segmentation and reassembly
3. **Application Layer:** UDS diagnostic services

---

## Backwards Compatibility

The CAN frame detection is **automatic and non-breaking**:

- ✅ Messages **with** CAN headers: Automatically detected and stripped
- ✅ Messages **without** CAN headers: Work exactly as before
- ✅ ISO-TP frames: Continue to work normally
- ✅ Raw UDS messages: Continue to work normally

The decoder will **only** strip the header if:
1. It matches the Ford CAN frame pattern
2. The CAN ID is in the diagnostic range
3. There's sufficient payload data after the header

---

## Common Scenarios

### Scenario 1: ECU Rejects RDBI Request

**Log Entry:**
```
2024-01-15 14:30:45.123 [ECU Response] 000007D87F2231
```

**Interpretation:**
- ECU responded to a ReadDataByIdentifier (0x22) request
- Request was rejected with NRC 0x31 (Request Out of Range)
- Likely cause: Requested DID is not supported by this ECU

### Scenario 2: Successful Session Change

**Request:**
```
000007D01003
```

**Response:**
```
000007D850030032001F4
```

**Interpretation:**
- Tester requested ExtendedDiagnosticSession (0x10 0x03)
- ECU confirmed (0x50 = 0x10 + 0x40 response marker)
- Response includes timing parameters (P2/P2*)

### Scenario 3: Security Access Seed Request

**Request:**
```
000007D02701
```

**Response:**
```
000007D867011234567890ABCDEF
```

**Interpretation:**
- Tester requested security seed (0x27 0x01 = Level 1)
- ECU responded with 7-byte seed value

---

## Negative Response Codes (NRC)

The decoder includes comprehensive NRC mappings. Common codes in Ford systems:

| NRC  | Hex  | Name                                    | Meaning                                                  |
|------|------|-----------------------------------------|----------------------------------------------------------|
| 0x10 | 16   | GeneralReject                           | General rejection, no specific reason                    |
| 0x11 | 17   | ServiceNotSupported                     | Service ID not recognized                                |
| 0x12 | 18   | SubFunctionNotSupported                 | Sub-function not supported                               |
| 0x13 | 19   | IncorrectMessageLengthOrInvalidFormat   | Message has wrong length or invalid format               |
| 0x22 | 34   | ConditionsNotCorrect                    | Preconditions not met (e.g., wrong session)              |
| 0x31 | 49   | RequestOutOfRange                       | Parameter value out of range                             |
| 0x33 | 51   | SecurityAccessDenied                    | Security access not granted                              |
| 0x35 | 53   | InvalidKey                              | Security key is incorrect                                |
| 0x36 | 54   | ExceedNumberOfAttempts                  | Too many failed security attempts                        |
| 0x37 | 55   | RequiredTimeDelayNotExpired             | Must wait before retrying security access                |
| 0x78 | 120  | RequestCorrectlyReceived_ResponsePending| ECU is processing request (expect follow-up response)    |

---

## Summary

The decoder now correctly handles Ford diagnostic log format by:

1. ✅ **Detecting** CAN frame headers (0x00 0x00 [CAN_ID])
2. ✅ **Extracting** CAN ID and determining message direction
3. ✅ **Stripping** the 4-byte header before UDS parsing
4. ✅ **Decoding** the UDS payload correctly
5. ✅ **Displaying** CAN metadata alongside UDS information

This fix resolves the issue where `000007D87F2231` was incorrectly parsed, now properly identifying:
- CAN ID: 0x07D8 (Response from ECU)
- Service: 0x7F (Negative Response)
- Original Service: 0x22 (ReadDataByIdentifier)
- NRC: 0x31 (Request Out of Range)
