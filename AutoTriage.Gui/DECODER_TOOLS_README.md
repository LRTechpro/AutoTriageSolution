# 🔧 Automotive Decoder Tools - Complete Guide

## Overview

The **Automotive Decoder Tools** is a comprehensive utility for decoding automotive diagnostic data, including UDS (Unified Diagnostic Services), ISO-TP transport frames, and Ford CAN diagnostic messages.

---

## 🚀 Features

### 1. **Auto-Detection**
- Automatically identifies input format (UDS, Hex, Binary, Base64, ASCII)
- Smart detection algorithm prioritizes UDS diagnostic codes
- One-click conversion after detection

### 2. **Multiple Conversion Types**
- **Hex ↔ ASCII**: Convert between hexadecimal and ASCII text
- **Binary ↔ Hex**: Convert between binary strings and hexadecimal
- **Base64 ↔ Text**: Encode/decode Base64 strings
- **UDS Code Decoder**: Full UDS diagnostic code interpretation with contextual explanations

### 3. **UDS Decoding**
The tool provides expert-level UDS decoding with:
- Service identification (requests and responses)
- Sub-function interpretation
- Negative Response Code (NRC) explanations
- Recommended actions for errors
- Contextual interpretations for each service

### 4. **Protocol Layer Handling**
- **ISO-TP (ISO 15765-2)**: Automatic detection and unwrapping of transport protocol frames
- **CAN Frame Headers**: Detection and stripping of Ford CAN diagnostic headers (4-byte format)
- **Multi-layer decoding**: CAN → ISO-TP → UDS

### 5. **Quick Samples**
Three instant-load sample buttons:
- **UDS**: Load a UDS negative response example
- **ISO-TP**: Load an ISO-TP single frame example
- **CAN Frame**: Load a Ford CAN diagnostic frame example

---

## 📖 Usage Guide

### Quick Start

1. **Paste or Type** your diagnostic data in the Input field
2. **Click "Auto Detect"** to automatically identify and convert
3. **View Results** in the Output field with detailed explanations

### Manual Conversion

1. Select a conversion type from the dropdown menu
2. Enter your input data
3. Click "Convert"
4. View the decoded output

### Swap Function

Use the "Swap" button to:
- Exchange input and output content
- Automatically reverse the conversion direction
- Quickly convert back and forth

---

## 🎯 Supported Input Formats

### Hexadecimal
- Space-separated: `7F 22 31`
- Hyphen-separated: `7F-22-31`
- Comma-separated: `7F,22,31`
- Colon-separated: `7F:22:31`
- Continuous: `7F2231`
- With 0x prefix: `0x7F 0x22 0x31`
- **Decimal CSV**: `127,34,49` (automatically detected!)

### Binary
- Space-separated: `01111111 00100010 00110001`
- Continuous: `011111110010001000110001`

### Base64
- Standard Base64 encoding
- Automatically validated and decoded

### ASCII Text
- Plain text strings
- Automatically detected when no other format matches

---

## 📚 UDS Diagnostic Code Examples

### Example 1: Negative Response
```
Input:  7F 22 31
Output: Detailed breakdown showing:
  • 0x7F = Negative Response indicator
  • 0x22 = ReadDataByIdentifier (rejected service)
  • 0x31 = RequestOutOfRange (error reason)
  • Full contextual explanation
  • Recommended action to fix the issue
```

### Example 2: Read VIN Request
```
Input:  22 F1 90
Output:
  • Service: ReadDataByIdentifier
  • DID: 0xF190 (VIN - Vehicle Identification Number)
  • Interpretation: Tester requesting VIN from ECU
```

### Example 3: Positive Response
```
Input:  50 03
Output:
  • Response to DiagnosticSessionControl (0x10 + 0x40 = 0x50)
  • Session Type: Extended Diagnostic Session
  • Interpretation: ECU confirmed session change
```

### Example 4: ISO-TP Wrapped UDS
```
Input:  03 7F 22 31
Output:
  • ISO-TP: Single Frame, 3-byte payload
  • UDS: Negative Response (7F 22 31)
  • Full multi-layer decode with both protocol details
```

### Example 5: Ford CAN Frame
```
Input:  00 00 07 D8 7F 22 31
Output:
  • CAN ID: 0x07D8 (Response from ECU)
  • Direction: Response from ECU
  • UDS Payload: Negative Response (7F 22 31)
  • Complete multi-layer interpretation
```

---

## 🔍 Understanding UDS Services

### Common UDS Services

| Service ID | Name | Purpose |
|------------|------|---------|
| 0x10 | DiagnosticSessionControl | Switch diagnostic sessions |
| 0x11 | ECUReset | Reset the ECU |
| 0x14 | ClearDiagnosticInformation | Clear DTCs |
| 0x19 | ReadDTCInformation | Read diagnostic trouble codes |
| 0x22 | ReadDataByIdentifier | Read specific data from ECU |
| 0x27 | SecurityAccess | Unlock security access |
| 0x2E | WriteDataByIdentifier | Write data to ECU |
| 0x31 | RoutineControl | Start/stop ECU routines |
| 0x34 | RequestDownload | Prepare for data download |
| 0x3E | TesterPresent | Keep-alive message |

### Response Types

- **Positive Response**: Service ID + 0x40 (e.g., 0x50 for DiagnosticSessionControl)
- **Negative Response**: Always starts with 0x7F, followed by requested service ID and NRC

### Common Negative Response Codes (NRCs)

| NRC | Name | Meaning |
|-----|------|---------|
| 0x11 | ServiceNotSupported | ECU doesn't support this service |
| 0x12 | SubFunctionNotSupported | Sub-function not available |
| 0x13 | IncorrectMessageLength | Message format is wrong |
| 0x22 | ConditionsNotCorrect | Preconditions not met |
| 0x31 | RequestOutOfRange | Parameters out of range |
| 0x33 | SecurityAccessDenied | Security access required |
| 0x35 | InvalidKey | Incorrect security key |
| 0x78 | ResponsePending | ECU is processing (not an error!) |

---

## 💡 Pro Tips

1. **Use Auto Detect** when unsure of the format - it's very accurate!
2. **NRC 0x78 is normal** - it means "wait, I'm processing your request"
3. **Ford CAN headers** (00 00 07 D0/07 D8) are automatically detected and stripped
4. **ISO-TP wrapping** is transparent - the tool unwraps it automatically
5. **Security Access** is a 2-step process: Request Seed (0x27 0x01) → Send Key (0x27 0x02)
6. **Extended Session** (0x10 0x03) enables access to more diagnostic services
7. **Decimal CSV format** like "2,203,006,208" is supported and auto-detected
8. The tool **never guesses** - it only returns proven facts from the data

---

## 🛠️ Button Reference

| Button | Icon | Function |
|--------|------|----------|
| **Auto Detect** | 🔍 | Automatically identify input format and convert |
| **Convert** | - | Convert using the selected conversion type |
| **Swap** | ⇅ | Swap input/output and reverse conversion direction |
| **Clear** | 🗑️ | Clear both input and output fields |
| **Examples** | 📚 | Show comprehensive usage examples |
| **Help** | ❓ | Display detailed help guide |

---

## 🎨 UI Features

### Dynamic Hints
The hint label at the top provides real-time feedback:
- **Blue**: Guidance on what to do next
- **Orange**: Waiting for more input
- **Green**: Ready to convert
- **Dark Slate Blue**: General tips

### Color-Coded Output
- **Input field**: White background for clear visibility
- **Output field**: Honeydew (light green) background to distinguish from input
- **Monospace font** (Consolas 10pt) for perfect hex byte alignment

### Quick Samples Panel
- Located below the main controls
- Three instant-load buttons for common scenarios
- Lavender background for visual distinction

---

## 🔧 Technical Details

### Architecture
The decoder uses a layered approach:
1. **Input Parsing**: Handles multiple formats (hex, decimal CSV, binary, Base64)
2. **CAN Frame Detection**: Strips Ford CAN headers if present
3. **ISO-TP Parsing**: Unwraps transport protocol frames
4. **UDS Decoding**: Interprets diagnostic services with full context

### Deterministic Design
The decoder follows a "no guessing" principle:
- Only returns proven facts from the data
- Uses official ISO 14229 (UDS) and ISO 15765-2 (ISO-TP) dictionaries
- Clear confidence ratings: "Exact", "Partial", or "Unknown"

### Performance
- Instant decoding for small to medium payloads
- Efficient parsing algorithms
- No external dependencies for core decoding

---

## ❓ FAQ

### Q: What's the difference between this and the main form's "include non-finding matches" checkbox?

**A:** These are **different features** in different parts of the application:

- **"Include non-finding matches" checkbox** (Main Form):
  - Located in the main AutoTriage log analyzer window
  - Controls whether non-diagnostic log lines are displayed in the results grid
  - When **checked**: Shows all log lines (findings + regular lines)
  - When **unchecked**: Shows only diagnostic findings
  - Use case: Toggle this when you want to see the complete log context around diagnostic issues

- **Decoder Tools** (This Window):
  - Separate diagnostic code decoder utility
  - Focused on converting and interpreting hex/UDS codes
  - No filtering - it decodes whatever you input
  - Accessed via "Decoder Tools" button in main form

### Q: Why does the "include non-finding matches" checkbox not work in Decoder Tools?

**A:** The Decoder Tools window **doesn't have** an "include non-finding matches" checkbox - that checkbox only exists in the main form (Form1.cs). The Decoder Tools is a conversion utility, not a log viewer, so filtering concepts don't apply here.

### Q: The "include non-finding matches" feature isn't doing anything!

**A:** Check these points:
1. Make sure you're looking at the **main form** (AutoTriage log analyzer), not the Decoder Tools window
2. The checkbox is located in the filter panel at the top of the main window
3. You need to **load a log file first** - the checkbox has no effect until you analyze a log
4. After checking/unchecking, the results grid should update automatically
5. Look at the status label - it shows how many matches were found
6. If it's still not working, check the `BuildDisplayedRows()` method in Form1.cs (lines 720-750)

### Q: Can I decode multiple UDS codes at once?

**A:** Currently, the tool decodes one code at a time. For batch processing, paste each code individually or separate them with line breaks and convert them one by one.

### Q: What if my format isn't supported?

**A:** Try these steps:
1. Use "Auto Detect" - it supports many formats
2. Manually clean up your input (remove extra characters)
3. Check the Examples for supported format patterns
4. If it's a special OEM format, you may need to pre-process it into hex

### Q: Why does my positive response show "Unknown DID"?

**A:** The tool uses a configurable DID (Data Identifier) dictionary. Standard DIDs like 0xF190 (VIN) are included, but OEM-specific DIDs need to be added to the dictionary in `AutomotivePayloadDecoder.cs`.

### Q: Can I add custom DIDs or Routine IDs?

**A:** Yes! Edit the `DidDictionary` and `RoutineIdDictionary` in `AutomotivePayloadDecoder.cs`. Add your OEM-specific mappings to get full interpretations.

---

## 📞 Support & Documentation

- **Source Code**: Check `AutoTriage.Gui\DecoderForm.cs` and `AutoTriage.Core\Decoding\AutomotivePayloadDecoder.cs`
- **Technical Docs**: See `README_DECODER.md` in the Decoding folder
- **ISO Standards**: 
  - ISO 14229: UDS specification
  - ISO 15765-2: ISO-TP diagnostic communication over CAN
- **Ford CAN Format**: See `FORD_CAN_FRAME_SUPPORT.md`

---

## 🎯 Accuracy Promise

This tool is designed for **100% accuracy**:
- ✅ Never guesses or invents interpretations
- ✅ Only returns proven facts from official protocol specs
- ✅ Clear confidence ratings for all results
- ✅ Transparent multi-layer decoding
- ✅ Extensive test coverage with validation vectors

---

## 📝 Version History

### Version 2.0 (Current)
- ✨ Added Help and Examples buttons
- ✨ Added Quick Samples panel for instant testing
- ✨ Dynamic hints based on input content
- ✨ Improved UI layout and color scheme
- ✨ Enhanced tooltips for all buttons
- ✨ Larger, more readable font sizes
- 🔧 Better window sizing and responsiveness

### Version 1.0
- Initial release with UDS decoding
- ISO-TP frame parsing
- Ford CAN frame support
- Basic conversion utilities

---

**Enjoy decoding! 🚗💻**
