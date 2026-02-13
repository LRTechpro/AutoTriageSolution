# NRC Code Quick Reference

## What Are NRC Codes?
**Negative Response Codes (NRC)** are standardized error codes defined in ISO 14229 (UDS - Unified Diagnostic Services). When an ECU cannot fulfill a diagnostic request, it responds with code **0x7F** followed by the NRC.

---

## Available Filters (6 Most Common)

### 0x31 - RequestOutOfRange
**Meaning**: Parameter value outside valid range  
**Common Causes**:
- Invalid DID (Data Identifier) requested
- Memory address out of bounds
- Routine ID not recognized
- Index parameter too large

**Example Log Lines**:
```
7F 22 31  → Negative Response: ReadDataByIdentifier rejected with RequestOutOfRange
000007D87F2231  → CAN frame with NRC 0x31
RequestOutOfRange detected in ECU response
```

---

### 0x33 - SecurityAccessDenied
**Meaning**: Insufficient security level for requested operation  
**Common Causes**:
- Trying to write data without unlocking ECU
- Security seed/key exchange not performed
- Expired security session
- Wrong security level active

**Example Log Lines**:
```
7F 2E 33  → Negative Response: WriteDataByIdentifier rejected with SecurityAccessDenied
SecurityAccessDenied - need to unlock first
Security level 0x01 required, currently at 0x00
```

---

### 0x22 - ConditionsNotCorrect
**Meaning**: ECU is not in correct state for this request  
**Common Causes**:
- ECU in wrong diagnostic session
- Vehicle speed too high
- Engine running (when it should be off)
- Temperature out of range
- Previous operation not completed

**Example Log Lines**:
```
7F 31 22  → RoutineControl rejected - ConditionsNotCorrect
Cannot erase memory while engine running
Diagnostic session must be Programming (0x02)
```

---

### 0x11 - ServiceNotSupported
**Meaning**: Service ID (SID) not implemented by this ECU  
**Common Causes**:
- Wrong ECU targeted (e.g., climate control doesn't support engine diagnostics)
- Service not available in this ECU variant
- Typo in service ID
- Outdated diagnostic specification

**Example Log Lines**:
```
7F 38 11  → Service 0x38 (RequestFileTransfer) not supported
ECU does not implement InputOutputControlByIdentifier
ServiceNotSupported - check diagnostic specification
```

---

### 0x35 - InvalidKey
**Meaning**: Security key doesn't match expected value  
**Common Causes**:
- Incorrect seed-to-key algorithm
- Wrong security key sent
- Key generation error
- Timing issue (sent key for wrong seed)

**Example Log Lines**:
```
7F 27 35  → SecurityAccess SendKey rejected - InvalidKey
Key 0x12345678 does not match expected value
Security unlock failed - invalid key
```

---

### 0x78 - RequestCorrectlyReceived_ResponsePending
**Meaning**: ECU received request but needs more time to respond  
**Common Causes**:
- Long-running operation (flash erase, routine execution)
- ECU busy processing
- Normal during programming sequences
- **Not an error** - informational only

**Example Log Lines**:
```
7F 31 78  → RoutineControl response pending (still executing)
ResponsePending - ECU busy with flash erase
Waiting for ECU to complete operation...
```

**Note**: This is the most common NRC and usually just means "please wait"

---

## Complete ISO 14229 NRC Table

| Code | Name | Description |
|------|------|-------------|
| 0x10 | GeneralReject | Request rejected, no specific reason |
| **0x11** | **ServiceNotSupported** | Service ID not implemented |
| 0x12 | SubFunctionNotSupported | Valid service but subfunction not supported |
| 0x13 | IncorrectMessageLengthOrInvalidFormat | Wrong message length or format |
| 0x14 | ResponseTooLong | Response would exceed buffer size |
| 0x21 | BusyRepeatRequest | ECU busy, retry later |
| **0x22** | **ConditionsNotCorrect** | ECU not in correct state |
| 0x24 | RequestSequenceError | Request out of sequence |
| 0x25 | NoResponseFromSubnetComponent | Internal ECU communication failed |
| 0x26 | FailurePreventsExecutionOfRequestedAction | Hardware failure prevents execution |
| **0x31** | **RequestOutOfRange** | Parameter out of range |
| **0x33** | **SecurityAccessDenied** | Security level insufficient |
| **0x35** | **InvalidKey** | Security key doesn't match |
| 0x36 | ExceedNumberOfAttempts | Too many failed security attempts |
| 0x37 | RequiredTimeDelayNotExpired | Must wait longer between attempts |
| 0x70 | UploadDownloadNotAccepted | Transfer not accepted |
| 0x71 | TransferDataSuspended | Data transfer suspended |
| 0x72 | GeneralProgrammingFailure | Flash programming failed |
| 0x73 | WrongBlockSequenceCounter | Data block out of sequence |
| **0x78** | **RequestCorrectlyReceived_ResponsePending** | Processing, please wait |
| 0x7E | SubFunctionNotSupportedInActiveSession | Subfunction not available in this session |
| 0x7F | ServiceNotSupportedInActiveSession | Service not available in this session |

**Bold** = Available as filter checkbox in AutoTriage

---

## How to Read NRC Messages

### Format 1: Raw Bytes (CAN/ISO-TP)
```
7F 22 31
│  │  └── NRC code (0x31 = RequestOutOfRange)
│  └───── Original Service ID that failed (0x22 = ReadDataByIdentifier)
└──────── Negative Response indicator (always 0x7F)
```

### Format 2: With CAN Header (Ford Format)
```
00 00 07 D8  7F 22 31
│           │  │  └── NRC code
│           │  └───── Original Service ID
│           └──────── Negative Response (0x7F)
└──────────────────── CAN ID 0x07D8 (ECU response)
```

### Format 3: Decoded Text
```
Negative Response: ReadDataByIdentifier rejected with RequestOutOfRange
                   ───────────────────          ─────────────────────
                   Original Service                   NRC Name
```

---

## Diagnostic Workflow

### Normal Success Flow
```
Request:  22 F1 90  (Read DID 0xF190 - VIN)
Response: 62 F1 90 31 47 31 4A 54 33 48 35 38...
          │  └─────┘ └────────────────────────┘
          │    DID           VIN data
          └── Positive Response (0x22 + 0x40 = 0x62)
```

### Error Flow (NRC Response)
```
Request:  22 F1 90
Response: 7F 22 31
          │  │  └── NRC 0x31 (RequestOutOfRange)
          │  └───── Service that failed (0x22)
          └──────── Negative Response indicator
```

---

## Troubleshooting Guide

### If you see 0x31 (RequestOutOfRange)
1. **Check DID value** - Is it valid for this ECU?
2. **Verify ECU type** - Right ECU model/year?
3. **Check session** - Some DIDs only available in extended diagnostic session
4. **Review specification** - Confirm DID exists in diagnostic spec

### If you see 0x33 (SecurityAccessDenied)
1. **Perform security unlock** - Send SecurityAccess seed/key exchange
2. **Check session** - Must be in correct diagnostic session (usually programming)
3. **Verify key algorithm** - Ensure seed-to-key conversion is correct
4. **Check timeout** - Security may have expired

### If you see 0x22 (ConditionsNotCorrect)
1. **Check diagnostic session** - Switch to extended or programming session
2. **Verify vehicle state** - Speed = 0, engine off, etc.
3. **Complete prerequisites** - Some operations require prior setup
4. **Review ECU state** - May be in fault mode or protected state

### If you see 0x11 (ServiceNotSupported)
1. **Verify service ID** - Typo in request?
2. **Check ECU capabilities** - Not all ECUs support all services
3. **Review spec** - Confirm service available for this ECU variant
4. **Try different ECU** - May have targeted wrong control module

### If you see 0x35 (InvalidKey)
1. **Verify seed** - Correct seed used for key calculation?
2. **Check algorithm** - Key generation logic correct?
3. **Timing** - Key must match most recent seed
4. **Retry** - Request new seed and recalculate

### If you see 0x78 (ResponsePending)
**This is normal!** Just wait for final response.
- **Typical duration**: 5-50ms for most operations
- **Long operations**: Flash erase can take 1-5 seconds
- **What to do**: Keep listening, final response will come

---

## Filter Strategy Examples

### Focus on Actual Errors (Hide ResponsePending)
**Goal**: Remove informational 0x78 messages  
**Action**: Uncheck `0x78` checkbox  
**Result**: Cleaner log showing only real problems

### Security Audit
**Goal**: Find all security-related failures  
**Action**:
1. Uncheck all except `0x33` and `0x35`
2. Keyword filter: "Security"
**Result**: Isolated view of security issues

### Debugging Failed Data Reads
**Goal**: Why aren't DIDs reading?  
**Action**:
1. Check `0x31` (invalid DID)
2. Check `0x33` (security blocked)
3. Check `0x22` (wrong state)
4. Uncheck everything else
**Result**: See exact failure reasons

### Programming Troubleshooting
**Goal**: Flash programming failing  
**Action**:
1. Filter for `0x72` (GeneralProgrammingFailure) - not in default 6
2. Use keyword: "programming", "flash", "0x34", "0x36"
3. Check `0x22` (conditions) and `0x33` (security)
**Result**: Programming-specific errors

---

## Integration with AutoTriage

### How NRC Filtering Works
1. **Text-based matching**: Searches line for NRC hex code or name
2. **Case-insensitive**: Finds "RequestOutOfRange" or "requestoutofrange"
3. **Multiple format support**:
   - Hex code: "0x31", "31"
   - Text name: "RequestOutOfRange"
   - In context: "7F 22 31", "rejected with RequestOutOfRange"

### Combining with Other Filters
```
Severity: [✓] Critical [✓] Error [ ] Warning [ ] Success
Keywords: "ECU", "Module"
NRC:      [✓] 0x31 [✓] 0x33 [ ] 0x78
Result:   Critical/Error ECU/Module lines with OutOfRange or SecurityDenied
```

### Performance Tips
- **Large logs**: Filter by NRC first to reduce dataset
- **Known issue**: Uncheck irrelevant codes to focus
- **Multiple issues**: Use keyword + NRC together
- **Export filtered results**: Copy from grid after filtering

---

## Resources

### Standards Documentation
- **ISO 14229**: UDS (Unified Diagnostic Services) specification
- **ISO 15765-2**: Diagnostic communication over CAN (ISO-TP)
- **SAE J1979**: OBD-II standard (subset of UDS)

### Related Tools
- **AutoTriage Decoder**: Decode raw payloads containing NRC
  - Access via: Tools → 🔧 Decoder Tools
  - Paste hex bytes to see full UDS breakdown
- **Decoder Self-Tests**: Verify NRC decoding working
  - Menu: Tools → Run Decoder Tests

### Further Reading
- `README_DECODER.md` - Decoder architecture and capabilities
- `FORD_CAN_FRAME_SUPPORT.md` - Ford CAN frame format details
- `ISO_TP_FIX_DOCUMENTATION.md` - ISO-TP protocol handling

---

## Quick Reference Card (Print/Pin)

```
╔═══════════════════════════════════════════════════════════╗
║           NRC CODES QUICK REFERENCE CARD                  ║
╠═══════════════════════════════════════════════════════════╣
║ 0x31 │ RequestOutOfRange         │ Bad parameter         ║
║ 0x33 │ SecurityAccessDenied      │ Need unlock           ║
║ 0x22 │ ConditionsNotCorrect      │ Wrong state           ║
║ 0x11 │ ServiceNotSupported       │ ECU can't do this     ║
║ 0x35 │ InvalidKey                │ Wrong security key    ║
║ 0x78 │ ResponsePending           │ Please wait (normal)  ║
╠═══════════════════════════════════════════════════════════╣
║ Format: 7F [SID] [NRC]                                    ║
║ Example: 7F 22 31 = Read DID failed (out of range)       ║
╚═══════════════════════════════════════════════════════════╝
```

---

**Last Updated**: 2025  
**Version**: AutoTriage v1.1.0  
**Author**: AutoTriage Development Team
