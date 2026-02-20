# Automotive Payload Decoder - Usage Guide

## Overview

The **AutomotivePayloadDecoder** is a deterministic, no-guessing decoder for automotive diagnostic payloads in AutoTriage. It supports:

- **UDS (ISO 14229)** protocol messages
- **Hex byte strings** (with or without separators)
- **Binary strings** (e.g., "01001101 00100...")
- **ASCII/UTF-8 text**
- **Mixed log lines** (extracts payloads automatically)

## Key Principle: No Guessing

This decoder **never guesses**. All interpretations are based on:
- Standard ISO 14229 dictionaries (NRC codes, service IDs)
- Configurable DID/Routine dictionaries (empty by default)
- Proven bytes only

If a field cannot be determined from available data, it returns **"UNKNOWN"** with a reason.

---

## How to Use

### 1. In the GUI (Context Menu)

**Right-click any row in the results grid:**

- **🔍 Decode Payload (UDS/Automotive)** - Decodes the selected log line
- **🧪 Run Decoder Self-Tests** - Validates decoder with built-in test vectors

The decoder will:
1. Extract the most likely payload from the log line
2. Attempt to decode as UDS, binary, hex, or ASCII
3. Show structured results with confidence level and reasons

**Example Output:**
```
═══════════════════════════════════════════════════════════════
Kind: UDS
Confidence: Partial
Raw Hex: 62 F1 90 31 32 33 34
Length: 7 bytes

Summary: ReadDataByIdentifier: DID 0xF190

Details:
  • DID: 0xF190 (not in dictionary)
  • Data: 4 bytes (raw hex: 31 32 33 34)

Parsed Fields:
  DID: 0xF190
  DIDName: UNKNOWN_DID
  DataLength: 4 bytes
  MessageType: Response
  ServiceName: ReadDataByIdentifier
  SID: 0x62

Interpretation Limitations:
  ⚠ DID 0xF190 not in configured DID dictionary (interpretation unavailable)
  ⚠ Data interpretation unavailable without known DID mapping

═══════════════════════════════════════════════════════════════
```

### 2. Programmatic API

```csharp
using AutoTriage.Core.Decoding;

// Decode from a log line
var decoder = new AutomotivePayloadDecoder();
var result = decoder.TryDecodeFromLine("2024-01-15 14:30:45 [ECU] Response: 62 F1 90 31 32 33 34");

// Access structured data
Console.WriteLine($"Kind: {result.Kind}");  // "UDS"
Console.WriteLine($"Summary: {result.Summary}");
Console.WriteLine($"Confidence: {result.InterpretationConfidence}");

// Check specific fields
if (result.Fields.TryGetValue("DID", out var did))
{
    Console.WriteLine($"DID: {did}");
}

// Get formatted output
Console.WriteLine(result.ToFormattedString());
```

### 3. Integration with Analysis Pipeline

```csharp
using AutoTriage.Core.Decoding;

// Option A: Decode from Finding object
var decoded = DecoderIntegration.TryDecodeFromFinding(finding);
if (decoded != null && decoded.Kind == "UDS")
{
    // Add to finding details
    finding.Evidence += "\n\n" + decoded.ToFormattedString();
}

// Option B: Get short summary for grid
var summary = DecoderIntegration.GetShortSummary(finding);
if (summary != null)
{
    Console.WriteLine(summary);  // "[UDS] ReadDataByIdentifier: DID 0xF190"
}

// Option C: Get detailed output
var details = DecoderIntegration.GetDetailedOutput(finding);
MessageBox.Show(details, "Decoded Payload");
```

---

## Supported UDS Services

### Fully Decoded:

| Service ID | Name | Decodes |
|------------|------|---------|
| 0x10/0x50 | DiagnosticSessionControl | Session type, timing parameters |
| 0x11/0x51 | ECUReset | Reset type, power-down time |
| 0x14/0x54 | ClearDiagnosticInformation | DTC group |
| 0x19/0x59 | ReadDTCInformation | Subfunction, DTC records |
| 0x22/0x62 | ReadDataByIdentifier | DID (with dictionary lookup), data bytes |
| 0x2E/0x6E | WriteDataByIdentifier | DID (with dictionary lookup), write data |
| 0x27/0x67 | SecurityAccess | Level, seed/key (opaque) |
| 0x31/0x71 | RoutineControl | Routine type, routine ID (with dictionary lookup) |
| 0x7F | NegativeResponse | Original SID, NRC code (with standard dictionary) |

### Partially Decoded:

All other standard UDS services are recognized by SID but without deep parsing.

---

## Configurable Dictionaries

### DID Dictionary (Data Identifiers)

Add your OEM-specific DIDs to enable interpretation:

```csharp
// In AutomotivePayloadDecoder.cs
AutomotivePayloadDecoder.DidDictionary.Add(0xF190, "VIN");
AutomotivePayloadDecoder.DidDictionary.Add(0xF18C, "ECUSerialNumber");
AutomotivePayloadDecoder.DidDictionary.Add(0xF194, "SupplierSpecific");
```

**Default entries included:**
- 0xF187: VehicleManufacturerSparePartNumber
- 0xF18A: VehicleManufacturerECUSoftwareNumber
- 0xF18C: ECUSerialNumber
- 0xF190: VIN
- 0xF191: VehicleManufacturerECUHardwareNumber
- 0xF194: SupplierSpecific
- 0xF19E: ActiveDiagnosticSession

### Routine ID Dictionary

Add your OEM-specific routine IDs:

```csharp
// In AutomotivePayloadDecoder.cs
AutomotivePayloadDecoder.RoutineIdDictionary.Add(0xFF00, "EraseMemory");
AutomotivePayloadDecoder.RoutineIdDictionary.Add(0xFF01, "CheckProgrammingDependencies");
AutomotivePayloadDecoder.RoutineIdDictionary.Add(0x0203, "CheckMemory");
```

---

## Built-in Test Vectors

Run self-tests to validate decoder functionality:

```csharp
var testResults = DecoderIntegration.RunDecoderSelfTests();
foreach (var result in testResults)
{
    Console.WriteLine(result);
}
```

**Test vectors include:**
1. ✅ Positive RDBI with unknown DID: `62 F1 90 31 32 33 34`
2. ✅ Negative response: `7F 22 31` (RequestOutOfRange)
3. ✅ SecurityAccess seed request: `67 01 12 34 56 78`
4. ✅ RoutineControl with unknown routine: `71 01 FF 00 AA BB`
5. ✅ ASCII text: `OK: CAL COMPLETE`
6. ✅ Mixed line with timestamp: `2024-01-15 14:30:45.123 [ECU] Response: 50 01`
7. ✅ Incomplete negative response: `7F 22`
8. ✅ Binary string: `01010000 01010001`

---

## Confidence Levels

### Exact
- All bytes decoded successfully
- All IDs found in dictionaries
- Full interpretation available

### Partial
- Bytes decoded but some IDs not in dictionaries
- Or insufficient bytes for complete service decode
- **Reasons** list explains what's missing

### Unknown
- Payload format not recognized
- Or decoding failed
- **Reasons** list explains why

---

## Input Format Support

### Hex Strings

**Supported formats:**
```
62 F1 90 31 32 33 34          // Space-separated
62-F1-90-31-32-33-34          // Dash-separated
62:F1:90:31:32:33:34          // Colon-separated
62,F1,90,31,32,33,34          // Comma-separated
62F1901234                    // Continuous
0x62 0xF1 0x90                // With 0x prefix
```

### Binary Strings

**Supported formats:**
```
01010000 01010001             // Space-separated 8-bit groups
0101000001010001              // Continuous (must be multiple of 8 bits)
```

### Mixed Log Lines

The decoder automatically extracts payloads:
```
2024-01-15 14:30:45.123 [TESTER->ECU] Request: 22 F1 90
                                                 ^^^^^^^^^ Extracted
```

---

## Best Practices

### 1. Extend Dictionaries for Your ECU

Add your OEM-specific DIDs and routine IDs to get "Exact" confidence:

```csharp
// At application startup
AutomotivePayloadDecoder.DidDictionary.Add(0xFD01, "ManufacturerCalibrationData");
AutomotivePayloadDecoder.RoutineIdDictionary.Add(0x0301, "StartSelfTest");
```

### 2. Use Structured Fields

Don't parse the Summary string - use Fields dictionary:

```csharp
if (decoded.Fields.TryGetValue("NRC", out var nrc))
{
    // Handle negative response
}

if (decoded.Fields.TryGetValue("DID", out var did))
{
    // Handle RDBI response
}
```

### 3. Check Confidence and Reasons

```csharp
if (decoded.InterpretationConfidence == "Partial")
{
    // Show reasons to user
    foreach (var reason in decoded.Reasons)
    {
        Console.WriteLine($"⚠ {reason}");
    }
}
```

### 4. Validate with Self-Tests

Run self-tests after modifying dictionaries to ensure decoder still works:

```csharp
var results = DecoderIntegration.RunDecoderSelfTests();
Assert.All(results, r => r.StartsWith("✅ PASS"));
```

---

## Architecture

```
Form1.cs (GUI)
    ↓ Right-click row
    ↓ MenuDecodePayload_Click()
    ↓
DecoderIntegration.cs (Helper)
    ↓ TryDecodeFromLine()
    ↓
AutomotivePayloadDecoder.cs (Core)
    ├── ExtractPayloadCandidates()     // Find hex/binary in line
    ├── TryParseHexString()            // Normalize & validate
    ├── DecodeBytes()                  // Dispatch decoder
    │   ├── TryDecodeAsUds()
    │   │   ├── DecodeNegativeResponse()
    │   │   ├── DecodeDiagnosticSessionControl()
    │   │   ├── DecodeReadDataByIdentifier()
    │   │   ├── DecodeSecurityAccess()
    │   │   └── ... (other services)
    │   ├── TryDecodeAsAscii()
    │   └── TryDecodeAsBinary()
    └── Returns DecodedPayload
```

---

## Extending the Decoder

### Add a New UDS Service

1. Add service to `UdsServices` dictionary
2. Create decoder method:

```csharp
private DecodedPayload DecodeMyCustomService(byte[] bytes, bool isResponse, string serviceName)
{
    var result = CreateUdsResult(bytes, serviceName, isResponse);
    
    if (bytes.Length < X)
    {
        result.Summary = $"{serviceName} (incomplete)";
        result.InterpretationConfidence = "Partial";
        result.Reasons.Add("Insufficient length (need X bytes)");
        return result;
    }
    
    // Parse fields
    byte myField = bytes[1];
    result.Fields["MyField"] = $"0x{myField:X2}";
    
    // Set summary and confidence
    result.Summary = $"{serviceName}: ...";
    result.InterpretationConfidence = "Exact";
    
    return result;
}
```

3. Add to dispatcher in `DecodeUdsService()`:

```csharp
return baseSid switch
{
    // ... existing cases
    0xXX => DecodeMyCustomService(bytes, isResponse, serviceName),
    _ => DecodeGenericUdsService(bytes, baseSid, isResponse, serviceName)
};
```

### Add Custom Dictionaries

Create your own lookup tables:

```csharp
private static readonly Dictionary<byte, string> MyCustomCodes = new Dictionary<byte, string>
{
    { 0x01, "Code1" },
    { 0x02, "Code2" }
};
```

---

## Troubleshooting

### Decoder returns "UNKNOWN"

**Check:**
1. Is the payload valid hex? (even length, valid characters)
2. Does the SID exist in `UdsServices` dictionary?
3. Run self-tests to validate decoder is working

### Confidence is "Partial"

**Check `Reasons` list:**
- DID not in dictionary → Add to `DidDictionary`
- Routine ID not in dictionary → Add to `RoutineIdDictionary`
- Insufficient bytes → Payload is incomplete

### Self-tests fail

**Likely causes:**
- Dictionary modified incorrectly
- Core decoder logic changed
- Check expected values in test vectors

---

## Files

### Core Decoder
- `AutoTriage.Core/Decoding/DecodedPayload.cs` - Result class
- `AutoTriage.Core/Decoding/AutomotivePayloadDecoder.cs` - Main decoder
- `AutoTriage.Core/Decoding/DecoderIntegration.cs` - Integration helpers

### GUI Integration
- `AutoTriage.Gui/Form1.cs` - Context menu handlers

---

## License & Support

Part of the AutoTriage project. For issues or feature requests, see the main repository.

**No external dependencies** - pure .NET 8+ implementation.
