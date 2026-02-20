using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AutoTriage.Core.Decoding
{
    /// <summary>
    /// Deterministic automotive payload decoder.
    /// Never guesses - only returns proven facts from bytes + known dictionaries.
    /// </summary>
    public class AutomotivePayloadDecoder
    {
        #region Standard Dictionaries (ISO 14229)

        /// <summary>
        /// ISO 14229 UDS Service IDs (request SIDs)
        /// </summary>
        private static readonly Dictionary<byte, string> UdsServices = new Dictionary<byte, string>
        {
            { 0x10, "DiagnosticSessionControl" },
            { 0x11, "ECUReset" },
            { 0x14, "ClearDiagnosticInformation" },
            { 0x19, "ReadDTCInformation" },
            { 0x22, "ReadDataByIdentifier" },
            { 0x23, "ReadMemoryByAddress" },
            { 0x24, "ReadScalingDataByIdentifier" },
            { 0x27, "SecurityAccess" },
            { 0x28, "CommunicationControl" },
            { 0x2A, "ReadDataByPeriodicIdentifier" },
            { 0x2C, "DynamicallyDefineDataIdentifier" },
            { 0x2E, "WriteDataByIdentifier" },
            { 0x2F, "InputOutputControlByIdentifier" },
            { 0x31, "RoutineControl" },
            { 0x34, "RequestDownload" },
            { 0x35, "RequestUpload" },
            { 0x36, "TransferData" },
            { 0x37, "RequestTransferExit" },
            { 0x38, "RequestFileTransfer" },
            { 0x3D, "WriteMemoryByAddress" },
            { 0x3E, "TesterPresent" },
            { 0x83, "AccessTimingParameter" },
            { 0x84, "SecuredDataTransmission" },
            { 0x85, "ControlDTCSetting" },
            { 0x86, "ResponseOnEvent" },
            { 0x87, "LinkControl" }
        };

        /// <summary>
        /// ISO 14229 Negative Response Codes (NRC)
        /// </summary>
        private static readonly Dictionary<byte, string> NrcCodes = new Dictionary<byte, string>
        {
            { 0x10, "GeneralReject" },
            { 0x11, "ServiceNotSupported" },
            { 0x12, "SubFunctionNotSupported" },
            { 0x13, "IncorrectMessageLengthOrInvalidFormat" },
            { 0x14, "ResponseTooLong" },
            { 0x21, "BusyRepeatRequest" },
            { 0x22, "ConditionsNotCorrect" },
            { 0x24, "RequestSequenceError" },
            { 0x25, "NoResponseFromSubnetComponent" },
            { 0x26, "FailurePreventsExecutionOfRequestedAction" },
            { 0x31, "RequestOutOfRange" },
            { 0x33, "SecurityAccessDenied" },
            { 0x35, "InvalidKey" },
            { 0x36, "ExceedNumberOfAttempts" },
            { 0x37, "RequiredTimeDelayNotExpired" },
            { 0x70, "UploadDownloadNotAccepted" },
            { 0x71, "TransferDataSuspended" },
            { 0x72, "GeneralProgrammingFailure" },
            { 0x73, "WrongBlockSequenceCounter" },
            { 0x78, "RequestCorrectlyReceived_ResponsePending" },
            { 0x7E, "SubFunctionNotSupportedInActiveSession" },
            { 0x7F, "ServiceNotSupportedInActiveSession" }
        };

        /// <summary>
        /// Session types for DiagnosticSessionControl (0x10/0x50)
        /// </summary>
        private static readonly Dictionary<byte, string> SessionTypes = new Dictionary<byte, string>
        {
            { 0x01, "DefaultSession" },
            { 0x02, "ProgrammingSession" },
            { 0x03, "ExtendedDiagnosticSession" },
            { 0x04, "SafetySystemDiagnosticSession" }
        };

        /// <summary>
        /// Reset types for ECUReset (0x11/0x51)
        /// </summary>
        private static readonly Dictionary<byte, string> ResetTypes = new Dictionary<byte, string>
        {
            { 0x01, "HardReset" },
            { 0x02, "KeyOffOnReset" },
            { 0x03, "SoftReset" },
            { 0x04, "EnableRapidPowerShutDown" },
            { 0x05, "DisableRapidPowerShutDown" }
        };

        /// <summary>
        /// ReadDTCInformation subfunctions (0x19/0x59)
        /// </summary>
        private static readonly Dictionary<byte, string> ReadDtcSubfunctions = new Dictionary<byte, string>
        {
            { 0x01, "ReportNumberOfDTCByStatusMask" },
            { 0x02, "ReportDTCByStatusMask" },
            { 0x03, "ReportDTCSnapshotIdentification" },
            { 0x04, "ReportDTCSnapshotRecordByDTCNumber" },
            { 0x06, "ReportDTCExtendedDataRecordByDTCNumber" },
            { 0x0A, "ReportSupportedDTC" }
        };

        /// <summary>
        /// RoutineControl types (0x31/0x71)
        /// </summary>
        private static readonly Dictionary<byte, string> RoutineControlTypes = new Dictionary<byte, string>
        {
            { 0x01, "StartRoutine" },
            { 0x02, "StopRoutine" },
            { 0x03, "RequestRoutineResults" }
        };

        /// <summary>
        /// Configurable DID (Data Identifier) dictionary - empty by default
        /// Add your OEM-specific DIDs here
        /// </summary>
        public static Dictionary<ushort, string> DidDictionary = new Dictionary<ushort, string>
        {
            // Example entries (commonly standard):
            { 0xF187, "VehicleManufacturerSparePartNumber" },
            { 0xF18A, "VehicleManufacturerECUSoftwareNumber" },
            { 0xF18C, "ECUSerialNumber" },
            { 0xF190, "VIN" },
            { 0xF191, "VehicleManufacturerECUHardwareNumber" },
            { 0xF194, "SupplierSpecific" },
            { 0xF19E, "ActiveDiagnosticSession" }
            // Add more OEM-specific DIDs as needed
        };

        /// <summary>
        /// Configurable Routine ID dictionary - empty by default
        /// Add your OEM-specific routine IDs here
        /// </summary>
        public static Dictionary<ushort, string> RoutineIdDictionary = new Dictionary<ushort, string>
        {
            // Example entries:
            { 0xFF00, "EraseMemory" },
            { 0xFF01, "CheckProgrammingDependencies" }
            // Add more OEM-specific routine IDs as needed
        };

        #endregion

        #region Public API

        /// <summary>
        /// Attempts to decode a payload from a log line.
        /// Implements proper protocol layering: Input → ISO-TP → UDS (conditional)
        /// </summary>
        public DecodedPayload TryDecodeFromLine(string line)
        {
            return TryDecodeFromLine(line, autoDetectIsoTp: true);
        }

        /// <summary>
        /// Attempts to decode a payload from a log line with explicit ISO-TP handling.
        /// </summary>
        public DecodedPayload TryDecodeFromLine(string line, bool autoDetectIsoTp)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return new DecodedPayload
                {
                    Kind = "UNKNOWN",
                    Summary = "Empty or null input",
                    InterpretationConfidence = "Unknown",
                    Reasons = new List<string> { "Input line is empty or whitespace" }
                };
            }

            // Extract payload candidates from line
            var candidates = ExtractPayloadCandidates(line);

            if (candidates.Count == 0)
            {
                // Try pure ASCII detection
                return TryDecodeAsAscii(line);
            }

            // Try decoding each candidate (longest first)
            foreach (var candidate in candidates.OrderByDescending(c => c.Length))
            {
                var result = TryDecodePayload(candidate, autoDetectIsoTp);
                if (result.Kind != "UNKNOWN")
                {
                    return result;
                }
            }

            // Fallback to first candidate or ASCII
            if (candidates.Count > 0)
            {
                return TryDecodePayload(candidates[0], autoDetectIsoTp);
            }

            return TryDecodeAsAscii(line);
        }

        /// <summary>
        /// Decodes a payload string directly (hex, binary, decimal CSV, or ASCII)
        /// Implements proper protocol layering: Input → ISO-TP → UDS (conditional)
        /// </summary>
        public DecodedPayload TryDecodePayload(string payload)
        {
            return TryDecodePayload(payload, autoDetectIsoTp: true);
        }

        /// <summary>
        /// Decodes a payload string with explicit ISO-TP handling
        /// </summary>
        public DecodedPayload TryDecodePayload(string payload, bool autoDetectIsoTp)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return CreateUnknownResult(Array.Empty<byte>(), "Empty payload");
            }

            // Try binary first (if it looks like binary)
            if (IsBinaryString(payload))
            {
                return TryDecodeAsBinary(payload, autoDetectIsoTp);
            }

            // Try parsing to bytes (handles hex, decimal CSV, etc.)
            var parseResult = ParseInputToBytes(payload);
            if (parseResult.Success)
            {
                return DecodeBytes(parseResult.Bytes, autoDetectIsoTp);
            }

            // Fallback to ASCII
            return TryDecodeAsAscii(payload);
        }

        #endregion

        #region Input Parsing

        /// <summary>
        /// Result of input parsing to bytes
        /// </summary>
        private class InputParseResult
        {
            public bool Success { get; set; }
            public byte[] Bytes { get; set; } = Array.Empty<byte>();
            public string Error { get; set; } = "";
            public string Format { get; set; } = "Unknown";
        }

        /// <summary>
        /// Parses input string to bytes, supporting multiple formats:
        /// - Decimal CSV: "2,203,006,208"
        /// - Hex bytes: "02 CB 06 D0", "02CB06D0", "0x02 0xCB 0x06 0xD0"
        /// </summary>
        private InputParseResult ParseInputToBytes(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return new InputParseResult
                {
                    Success = false,
                    Error = "Input is empty or whitespace"
                };
            }

            // Try decimal CSV first (e.g., "2,203,006,208")
            if (input.Contains(",") && !Regex.IsMatch(input, @"[A-Fa-f]"))
            {
                try
                {
                    var parts = input.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    var bytes = new List<byte>();

                    foreach (var part in parts)
                    {
                        var trimmed = part.Trim();
                        if (string.IsNullOrEmpty(trimmed))
                            continue;

                        if (uint.TryParse(trimmed, out uint value))
                        {
                            if (value <= 255)
                            {
                                bytes.Add((byte)value);
                            }
                            else
                            {
                                return new InputParseResult
                                {
                                    Success = false,
                                    Error = $"Decimal value {value} exceeds byte range (0-255)"
                                };
                            }
                        }
                        else
                        {
                            return new InputParseResult
                            {
                                Success = false,
                                Error = $"Invalid decimal value: '{trimmed}'"
                            };
                        }
                    }

                    if (bytes.Count == 0)
                    {
                        return new InputParseResult
                        {
                            Success = false,
                            Error = "No valid decimal bytes found in CSV input"
                        };
                    }

                    return new InputParseResult
                    {
                        Success = true,
                        Bytes = bytes.ToArray(),
                        Format = "DecimalCSV"
                    };
                }
                catch (Exception ex)
                {
                    return new InputParseResult
                    {
                        Success = false,
                        Error = $"Decimal CSV parsing failed: {ex.Message}"
                    };
                }
            }

            // Try hex parsing
            var hexResult = TryParseHexString(input);
            if (hexResult.Success)
            {
                return new InputParseResult
                {
                    Success = true,
                    Bytes = hexResult.Bytes,
                    Format = "Hex"
                };
            }

            return new InputParseResult
            {
                Success = false,
                Error = $"Could not parse as hex or decimal CSV: {hexResult.Error}"
            };
        }

        #endregion

        #region CAN Frame Header Handling

        /// <summary>
        /// Known CAN identifiers for Ford diagnostic messages
        /// </summary>
        private static class CanIdentifiers
        {
            public const ushort EcuRequest = 0x07D0;  // Request to ECU
            public const ushort EcuResponse = 0x07D8; // Response from ECU
        }

        /// <summary>
        /// Result of CAN frame header detection and stripping
        /// </summary>
        private class CanFrameResult
        {
            public bool HasCanHeader { get; set; }
            public ushort CanId { get; set; }
            public string Direction { get; set; } = "Unknown";
            public byte[] PayloadBytes { get; set; } = Array.Empty<byte>();
            public string Info { get; set; } = "";
        }

        /// <summary>
        /// Detects and strips CAN frame header (4 bytes: 0x00 0x00 [CAN_ID_HIGH] [CAN_ID_LOW])
        /// Common in Ford diagnostic logs where messages start with CAN identifier
        /// Example: "000007D87F2231" → CAN ID 0x07D8, Payload: 7F 22 31
        /// </summary>
        private CanFrameResult DetectAndStripCanHeader(byte[] bytes)
        {
            var result = new CanFrameResult
            {
                HasCanHeader = false,
                PayloadBytes = bytes
            };

            // Need at least 5 bytes for CAN header (4 bytes) + message (1+ bytes)
            if (bytes == null || bytes.Length < 5)
            {
                return result;
            }

            // Check for CAN frame header pattern: 0x00 0x00 [HIGH] [LOW]
            // Where [HIGH][LOW] forms the CAN identifier
            if (bytes[0] == 0x00 && bytes[1] == 0x00)
            {
                // Extract CAN ID from bytes 2-3
                ushort canId = (ushort)((bytes[2] << 8) | bytes[3]);

                // Check if this is a known Ford diagnostic CAN ID
                bool isKnownCanId = canId == CanIdentifiers.EcuRequest || 
                                   canId == CanIdentifiers.EcuResponse ||
                                   (canId >= 0x07D0 && canId <= 0x07DF); // Common diagnostic range

                if (isKnownCanId)
                {
                    result.HasCanHeader = true;
                    result.CanId = canId;

                    // Determine message direction
                    if (canId == CanIdentifiers.EcuRequest)
                    {
                        result.Direction = "Request to ECU";
                    }
                    else if (canId == CanIdentifiers.EcuResponse)
                    {
                        result.Direction = "Response from ECU";
                    }
                    else
                    {
                        result.Direction = $"CAN ID 0x{canId:X3}";
                    }

                    // Strip the 4-byte CAN header
                    result.PayloadBytes = new byte[bytes.Length - 4];
                    Array.Copy(bytes, 4, result.PayloadBytes, 0, result.PayloadBytes.Length);

                    result.Info = $"CAN ID: 0x{canId:X3} ({result.Direction})";
                }
            }

            return result;
        }

        #endregion

        #region ISO-TP (ISO 15765-2) Parsing

        /// <summary>
        /// Valid UDS Service IDs (SIDs) for validation
        /// </summary>
        private static readonly HashSet<byte> ValidUdsRequestSids = new HashSet<byte>
        {
            0x10, 0x11, 0x14, 0x19, 0x22, 0x23, 0x24, 0x27, 0x28,
            0x2A, 0x2C, 0x2E, 0x2F, 0x31, 0x34, 0x35, 0x36, 0x37,
            0x38, 0x3D, 0x3E, 0x83, 0x84, 0x85, 0x86, 0x87
        };

        /// <summary>
        /// Checks if a byte is a valid UDS SID (request, response, or negative response)
        /// </summary>
        private bool IsValidUdsSid(byte sid)
        {
            // Negative response
            if (sid == 0x7F)
                return true;

            // Response SIDs (request + 0x40)
            if (sid >= 0x40 && sid <= 0xBF)
            {
                byte requestSid = (byte)(sid - 0x40);
                return ValidUdsRequestSids.Contains(requestSid);
            }

            // Request SIDs
            return ValidUdsRequestSids.Contains(sid);
        }

        /// <summary>
        /// Parses ISO-TP (ISO 15765-2) frame from raw bytes
        /// Extracts PCI, frame type, and payload according to protocol specification
        /// </summary>
        private IsoTpResult ParseIsoTp(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return new IsoTpResult
                {
                    FrameType = IsoTpFrameType.Invalid,
                    RawBytes = bytes ?? Array.Empty<byte>(),
                    Confidence = "Unknown",
                    Summary = "Empty input",
                    Reasons = new List<string> { "No bytes to parse" }
                };
            }

            var result = new IsoTpResult
            {
                RawBytes = bytes,
                Pci = bytes[0]
            };

            // Parse PCI byte: upper 4 bits = frame type, lower 4 bits = type-specific data
            int frameType = (bytes[0] >> 4) & 0xF;

            switch (frameType)
            {
                case 0: // Single Frame
                    return ParseSingleFrame(bytes, result);

                case 1: // First Frame
                    return ParseFirstFrame(bytes, result);

                case 2: // Consecutive Frame
                    return ParseConsecutiveFrame(bytes, result);

                case 3: // Flow Control
                    return ParseFlowControlFrame(bytes, result);

                default:
                    result.FrameType = IsoTpFrameType.Invalid;
                    result.Confidence = "Unknown";
                    result.Summary = $"Unknown ISO-TP frame type: 0x{frameType:X}";
                    result.Reasons.Add($"Frame type {frameType} not defined in ISO 15765-2");
                    return result;
            }
        }

        private IsoTpResult ParseSingleFrame(byte[] bytes, IsoTpResult result)
        {
            result.FrameType = IsoTpFrameType.SingleFrame;
            result.PayloadLength = bytes[0] & 0xF;

            if (result.PayloadLength == 0)
            {
                result.Confidence = "Partial";
                result.Summary = "ISO-TP Single Frame (zero-length payload)";
                result.Reasons.Add("Payload length is 0 (unusual for valid frame)");
                result.Payload = Array.Empty<byte>();
                return result;
            }

            if (bytes.Length < 1 + result.PayloadLength)
            {
                result.Confidence = "Partial";
                result.Summary = $"ISO-TP Single Frame (incomplete - need {1 + result.PayloadLength} bytes, have {bytes.Length})";
                result.Reasons.Add($"Frame declares {result.PayloadLength} byte payload but only {bytes.Length - 1} bytes available");
                result.Payload = bytes.Skip(1).ToArray();
                return result;
            }

            result.Payload = new byte[result.PayloadLength];
            Array.Copy(bytes, 1, result.Payload, 0, result.PayloadLength);
            result.Confidence = "Exact";
            result.Summary = $"ISO-TP Single Frame ({result.PayloadLength} byte payload)";
            result.Details.Add($"Payload length: {result.PayloadLength} bytes");

            if (bytes.Length > 1 + result.PayloadLength)
            {
                int paddingBytes = bytes.Length - 1 - result.PayloadLength;
                result.Details.Add($"Padding: {paddingBytes} bytes (ignored)");
            }

            return result;
        }

        private IsoTpResult ParseFirstFrame(byte[] bytes, IsoTpResult result)
        {
            result.FrameType = IsoTpFrameType.FirstFrame;

            if (bytes.Length < 2)
            {
                result.Confidence = "Partial";
                result.Summary = "ISO-TP First Frame (incomplete - missing length byte)";
                result.Reasons.Add("Need at least 2 bytes for First Frame (PCI + length)");
                return result;
            }

            result.PayloadLength = ((bytes[0] & 0xF) << 8) | bytes[1];
            result.Confidence = "Partial";
            result.Summary = $"ISO-TP First Frame (total payload: {result.PayloadLength} bytes)";
            result.Details.Add($"Total payload length: {result.PayloadLength} bytes");
            result.Details.Add($"Initial data: {bytes.Length - 2} bytes");
            result.Reasons.Add("Multi-frame message - requires reassembly with Consecutive Frames");

            if (bytes.Length > 2)
            {
                result.Payload = new byte[bytes.Length - 2];
                Array.Copy(bytes, 2, result.Payload, 0, result.Payload.Length);
            }

            return result;
        }

        private IsoTpResult ParseConsecutiveFrame(byte[] bytes, IsoTpResult result)
        {
            result.FrameType = IsoTpFrameType.ConsecutiveFrame;
            result.SequenceNumber = bytes[0] & 0xF;
            result.Confidence = "Partial";
            result.Summary = $"ISO-TP Consecutive Frame (sequence: {result.SequenceNumber})";
            result.Details.Add($"Sequence number: {result.SequenceNumber}");
            result.Reasons.Add("Consecutive Frame - requires reassembly with First Frame");

            if (bytes.Length > 1)
            {
                result.Payload = new byte[bytes.Length - 1];
                Array.Copy(bytes, 1, result.Payload, 0, result.Payload.Length);
                result.Details.Add($"Data: {result.Payload.Length} bytes");
            }

            return result;
        }

        private IsoTpResult ParseFlowControlFrame(byte[] bytes, IsoTpResult result)
        {
            result.FrameType = IsoTpFrameType.FlowControl;
            result.FlowStatus = (byte)(bytes[0] & 0xF);
            result.Confidence = "Exact";

            string flowStatusName;
            switch (result.FlowStatus)
            {
                case 0:
                    flowStatusName = "ContinueToSend";
                    break;
                case 1:
                    flowStatusName = "Wait";
                    break;
                case 2:
                    flowStatusName = "Overflow";
                    break;
                default:
                    flowStatusName = $"Reserved/Unknown (0x{result.FlowStatus:X})";
                    break;
            }

            result.Summary = $"ISO-TP Flow Control ({flowStatusName})";
            result.Details.Add($"Flow status: {flowStatusName}");

            if (bytes.Length >= 2)
            {
                result.Details.Add($"Block size: {bytes[1]}");
            }
            if (bytes.Length >= 3)
            {
                result.Details.Add($"Separation time: {bytes[2]} ms");
            }

            return result;
        }

        /// <summary>
        /// Converts ISO-TP parsing result to DecodedPayload format
        /// Attempts UDS decoding on extracted payload if applicable
        /// </summary>
        private DecodedPayload ConvertIsoTpToDecodedPayload(IsoTpResult isoTpResult, bool attemptUdsDecoding)
        {
            var decodedPayload = new DecodedPayload
            {
                Bytes = isoTpResult.RawBytes,
                InterpretationConfidence = isoTpResult.Confidence,
                Details = new List<string>()
            };

            // Add ISO-TP frame information
            decodedPayload.Details.Add($"ISO-TP Frame Type: {isoTpResult.FrameType}");
            decodedPayload.Details.Add($"PCI Byte: 0x{isoTpResult.Pci:X2}");
            decodedPayload.Details.AddRange(isoTpResult.Details);

            // Check if we can attempt UDS decoding
            bool canAttemptUds = isoTpResult.FrameType == IsoTpFrameType.SingleFrame &&
                                 isoTpResult.Payload.Length > 0 &&
                                 isoTpResult.Confidence != "Partial";

            if (canAttemptUds && attemptUdsDecoding)
            {
                // Validate that first byte of payload is a valid UDS SID
                byte firstByte = isoTpResult.Payload[0];

                if (IsValidUdsSid(firstByte))
                {
                    // Attempt UDS decoding on the extracted payload
                    var udsResult = TryDecodeAsUds(isoTpResult.Payload);

                    if (udsResult.Kind == "UDS")
                    {
                        // Successfully decoded as UDS - merge ISO-TP details
                        udsResult.Details.InsertRange(0, decodedPayload.Details);
                        udsResult.Details.Add($"Extracted Payload: {BitConverter.ToString(isoTpResult.Payload).Replace("-", " ")}");
                        return udsResult;
                    }
                }
                else
                {
                    // Payload first byte is not a valid UDS SID
                    decodedPayload.Kind = "ISO-TP";
                    decodedPayload.Summary = $"{isoTpResult.Summary} - Non-UDS payload";
                    decodedPayload.Details.Add($"Extracted Payload: {BitConverter.ToString(isoTpResult.Payload).Replace("-", " ")}");
                    decodedPayload.Reasons.Add($"Payload first byte 0x{firstByte:X2} is not a valid UDS Service ID");
                    decodedPayload.Fields["PayloadHex"] = BitConverter.ToString(isoTpResult.Payload).Replace("-", " ");
                    return decodedPayload;
                }
            }

            // ISO-TP frame but cannot/should not attempt UDS decoding
            decodedPayload.Kind = "ISO-TP";
            decodedPayload.Summary = isoTpResult.Summary;

            if (isoTpResult.Payload.Length > 0)
            {
                decodedPayload.Details.Add($"Payload data: {BitConverter.ToString(isoTpResult.Payload).Replace("-", " ")}");
                decodedPayload.Fields["PayloadHex"] = BitConverter.ToString(isoTpResult.Payload).Replace("-", " ");
            }

            decodedPayload.Reasons.AddRange(isoTpResult.Reasons);

            return decodedPayload;
        }

        #endregion

        #region Payload Extraction

        private List<string> ExtractPayloadCandidates(string line)
        {
            var candidates = new List<string>();

            // Pattern 1: Hex bytes with various separators (e.g., "62 F1 90 31", "62-F1-90", "62:F1:90", "62,F1,90")
            var hexPatterns = new[]
            {
                @"(?:0[xX])?[0-9A-Fa-f]{2}(?:[\s,:\-]+(?:0[xX])?[0-9A-Fa-f]{2})+",  // Multi-byte with separators
                @"(?:0[xX])?[0-9A-Fa-f]{4,}",  // Continuous hex (min 4 chars = 2 bytes)
            };

            foreach (var pattern in hexPatterns)
            {
                var matches = Regex.Matches(line, pattern);
                foreach (Match match in matches)
                {
                    if (match.Value.Length >= 4) // At least 2 hex digits
                    {
                        candidates.Add(match.Value);
                    }
                }
            }

            // Pattern 2: Binary strings (e.g., "01001101 00100...")
            var binaryPattern = @"[01]{8}(?:[\s]+[01]{8})+";
            var binaryMatches = Regex.Matches(line, binaryPattern);
            foreach (Match match in binaryMatches)
            {
                candidates.Add(match.Value);
            }

            return candidates.Distinct().ToList();
        }

        #endregion

        #region Hex Parsing

        private class HexParseResult
        {
            public bool Success { get; set; }
            public byte[] Bytes { get; set; } = Array.Empty<byte>();
            public string Error { get; set; } = "";
        }

        private HexParseResult TryParseHexString(string hex)
        {
            try
            {
                // Normalize: remove common separators and 0x prefix
                var normalized = hex.Replace("0x", "")
                                    .Replace("0X", "")
                                    .Replace(" ", "")
                                    .Replace(",", "")
                                    .Replace(":", "")
                                    .Replace("-", "")
                                    .Replace("\t", "")
                                    .ToUpper();

                // Validate hex characters
                if (!Regex.IsMatch(normalized, @"^[0-9A-F]+$"))
                {
                    return new HexParseResult
                    {
                        Success = false,
                        Error = "Invalid hex characters"
                    };
                }

                // Check even length
                if (normalized.Length % 2 != 0)
                {
                    return new HexParseResult
                    {
                        Success = false,
                        Error = "Odd length hex string (not byte-aligned)"
                    };
                }

                // Convert to bytes
                var bytes = new byte[normalized.Length / 2];
                for (int i = 0; i < bytes.Length; i++)
                {
                    bytes[i] = Convert.ToByte(normalized.Substring(i * 2, 2), 16);
                }

                return new HexParseResult
                {
                    Success = true,
                    Bytes = bytes
                };
            }
            catch (Exception ex)
            {
                return new HexParseResult
                {
                    Success = false,
                    Error = $"Hex parsing failed: {ex.Message}"
                };
            }
        }

        #endregion

        #region Binary Decoding

        private bool IsBinaryString(string input)
        {
            var normalized = input.Replace(" ", "").Replace("\t", "");
            return normalized.Length >= 8 && Regex.IsMatch(normalized, @"^[01]+$");
        }

        private DecodedPayload TryDecodeAsBinary(string binary, bool autoDetectIsoTp)
        {
            try
            {
                var normalized = binary.Replace(" ", "").Replace("\t", "");

                if (normalized.Length % 8 != 0)
                {
                    return new DecodedPayload
                    {
                        Kind = "BINARY",
                        Summary = "Invalid binary string (not byte-aligned)",
                        InterpretationConfidence = "Unknown",
                        Reasons = new List<string> { $"Binary string length ({normalized.Length} bits) is not a multiple of 8" }
                    };
                }

                var bytes = new byte[normalized.Length / 8];
                for (int i = 0; i < bytes.Length; i++)
                {
                    bytes[i] = Convert.ToByte(normalized.Substring(i * 8, 8), 2);
                }

                // Decode the bytes
                var result = DecodeBytes(bytes, autoDetectIsoTp);
                result.Details.Insert(0, $"Converted from binary string ({normalized.Length} bits)");
                return result;
            }
            catch (Exception ex)
            {
                return new DecodedPayload
                {
                    Kind = "BINARY",
                    Summary = "Binary parsing failed",
                    InterpretationConfidence = "Unknown",
                    Reasons = new List<string> { $"Binary parse error: {ex.Message}" }
                };
            }
        }

        #endregion

        #region ASCII Decoding

        private DecodedPayload TryDecodeAsAscii(string input)
        {
            try
            {
                var bytes = Encoding.ASCII.GetBytes(input);
                var printableCount = bytes.Count(b => b >= 32 && b <= 126);
                var printableRatio = bytes.Length > 0 ? (double)printableCount / bytes.Length : 0;

                if (printableRatio > 0.8) // Mostly printable ASCII
                {
                    return new DecodedPayload
                    {
                        Kind = "ASCII",
                        Bytes = bytes,
                        Summary = $"ASCII text: \"{input}\"",
                        InterpretationConfidence = "Exact",
                        Details = new List<string>
                        {
                            $"Length: {bytes.Length} bytes",
                            $"Printable characters: {printableCount}/{bytes.Length} ({printableRatio:P0})"
                        },
                        Fields = new Dictionary<string, string>
                        {
                            { "Text", input },
                            { "Encoding", "ASCII" }
                        }
                    };
                }
                else
                {
                    return new DecodedPayload
                    {
                        Kind = "HEX",
                        Bytes = bytes,
                        Summary = "Raw bytes (low printable ratio)",
                        InterpretationConfidence = "Unknown",
                        Details = new List<string>
                        {
                            $"Printable characters: {printableCount}/{bytes.Length} ({printableRatio:P0})",
                            "Not recognized as ASCII, UDS, or valid hex"
                        },
                        Reasons = new List<string> { "Low printable character ratio for ASCII" }
                    };
                }
            }
            catch (Exception ex)
            {
                return CreateUnknownResult(Array.Empty<byte>(), $"ASCII decode error: {ex.Message}");
            }
        }

        #endregion

        #region Byte Decoding

        private DecodedPayload DecodeBytes(byte[] bytes, bool autoDetectIsoTp)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return CreateUnknownResult(bytes, "No bytes to decode");
            }

            // Step 1: Check for and strip CAN frame header (Ford diagnostic format)
            var canResult = DetectAndStripCanHeader(bytes);
            byte[] workingBytes = canResult.PayloadBytes;

            // Step 2: Try ISO-TP parsing first if enabled
            if (autoDetectIsoTp && workingBytes.Length >= 1)
            {
                var isoTpResult = ParseIsoTp(workingBytes);
                if (isoTpResult.FrameType != IsoTpFrameType.Invalid)
                {
                    // Successfully parsed as ISO-TP
                    var decoded = ConvertIsoTpToDecodedPayload(isoTpResult, autoDetectIsoTp);

                    // Add CAN frame information if present
                    if (canResult.HasCanHeader)
                    {
                        decoded.Details.Insert(0, $"CAN Frame: {canResult.Info}");
                        decoded.Fields["CanID"] = $"0x{canResult.CanId:X3}";
                        decoded.Fields["Direction"] = canResult.Direction;
                    }

                    return decoded;
                }
            }

            // Step 3: Fallback - Try direct UDS (for cases where ISO-TP is not used)
            var udsResult = TryDecodeAsUds(workingBytes);
            if (udsResult.Kind == "UDS")
            {
                // Add CAN frame information if present
                if (canResult.HasCanHeader)
                {
                    udsResult.Details.Insert(0, $"CAN Frame: {canResult.Info}");
                    udsResult.Fields["CanID"] = $"0x{canResult.CanId:X3}";
                    udsResult.Fields["Direction"] = canResult.Direction;
                }

                return udsResult;
            }

            // Check if it's UTF-8 text
            try
            {
                var utf8Text = Encoding.UTF8.GetString(bytes);
                var printableCount = utf8Text.Count(c => !char.IsControl(c));
                var printableRatio = utf8Text.Length > 0 ? (double)printableCount / utf8Text.Length : 0;

                if (printableRatio > 0.8)
                {
                    return new DecodedPayload
                    {
                        Kind = "ASCII",
                        Bytes = bytes,
                        Summary = $"UTF-8 text: \"{utf8Text}\"",
                        InterpretationConfidence = "Exact",
                        Details = new List<string>
                        {
                            $"Length: {bytes.Length} bytes",
                            $"Printable characters: {printableCount}/{utf8Text.Length}"
                        },
                        Fields = new Dictionary<string, string>
                        {
                            { "Text", utf8Text },
                            { "Encoding", "UTF-8" }
                        }
                    };
                }
            }
            catch { /* Not valid UTF-8 */ }

            // Fallback to raw hex
            return new DecodedPayload
            {
                Kind = "HEX",
                Bytes = bytes,
                Summary = $"Raw hex data ({bytes.Length} bytes)",
                InterpretationConfidence = "Unknown",
                Details = new List<string>
                {
                    "No known protocol detected",
                    "Data shown as hexadecimal"
                },
                Reasons = new List<string> { "Does not match UDS format or text encoding" }
            };
        }

        #endregion

        #region UDS Decoding

        private DecodedPayload TryDecodeAsUds(byte[] bytes)
        {
            if (bytes.Length == 0)
            {
                return CreateUnknownResult(bytes, "Empty payload");
            }

            byte sid = bytes[0];

            // Check for Negative Response (0x7F)
            if (sid == 0x7F)
            {
                return DecodeNegativeResponse(bytes);
            }

            // Check if it's a known UDS service (request or response)
            byte baseSid = (byte)(sid >= 0x40 && sid <= 0xBF ? sid - 0x40 : sid);
            bool isResponse = sid >= 0x40 && sid <= 0xBF;

            if (UdsServices.ContainsKey(baseSid))
            {
                return DecodeUdsService(bytes, baseSid, isResponse);
            }

            // Not recognized as UDS
            return CreateUnknownResult(bytes, "SID not recognized in UDS service table");
        }

        private DecodedPayload DecodeNegativeResponse(byte[] bytes)
        {
            var result = new DecodedPayload
            {
                Kind = "UDS",
                Bytes = bytes,
                Fields = new Dictionary<string, string>()
            };

            result.Fields["MessageType"] = "NegativeResponse";
            result.Fields["SID"] = "0x7F";

            if (bytes.Length < 3)
            {
                result.Summary = "Negative Response (incomplete)";
                result.InterpretationConfidence = "Partial";
                result.Reasons.Add("Insufficient length for negative response (need 3 bytes: 7F + RequestSID + NRC)");
                result.Details.Add($"Length: {bytes.Length} bytes (expected 3)");
                return result;
            }

            byte requestSid = bytes[1];
            byte nrc = bytes[2];

            result.Fields["RequestSID"] = $"0x{requestSid:X2}";
            result.Fields["RequestService"] = UdsServices.ContainsKey(requestSid) 
                ? UdsServices[requestSid] 
                : "UNKNOWN";

            result.Fields["NRC"] = $"0x{nrc:X2}";
            result.Fields["NRCName"] = NrcCodes.ContainsKey(nrc) 
                ? NrcCodes[nrc] 
                : "UNKNOWN_NRC";

            result.Summary = $"Negative Response: {result.Fields["RequestService"]} rejected with {result.Fields["NRCName"]}";
            result.InterpretationConfidence = NrcCodes.ContainsKey(nrc) ? "Exact" : "Partial";

            result.Details.Add($"Original request: {result.Fields["RequestService"]} (0x{requestSid:X2})");
            result.Details.Add($"Negative Response Code: {result.Fields["NRCName"]} (0x{nrc:X2})");

            if (!NrcCodes.ContainsKey(nrc))
            {
                result.Reasons.Add($"NRC 0x{nrc:X2} not in standard ISO 14229 dictionary (may be OEM-specific)");
            }

            if (bytes.Length > 3)
            {
                result.Details.Add($"Additional data: {bytes.Length - 3} bytes (unusual for negative response)");
            }

            return result;
        }

        private DecodedPayload DecodeUdsService(byte[] bytes, byte baseSid, bool isResponse)
        {
            var serviceName = UdsServices[baseSid];

            // Dispatch to specific service decoder
            switch (baseSid)
            {
                case 0x10:
                    return DecodeDiagnosticSessionControl(bytes, isResponse, serviceName);
                case 0x11:
                    return DecodeEcuReset(bytes, isResponse, serviceName);
                case 0x14:
                    return DecodeClearDiagnosticInformation(bytes, isResponse, serviceName);
                case 0x19:
                    return DecodeReadDtcInformation(bytes, isResponse, serviceName);
                case 0x22:
                    return DecodeReadDataByIdentifier(bytes, isResponse, serviceName);
                case 0x2E:
                    return DecodeWriteDataByIdentifier(bytes, isResponse, serviceName);
                case 0x27:
                    return DecodeSecurityAccess(bytes, isResponse, serviceName);
                case 0x31:
                    return DecodeRoutineControl(bytes, isResponse, serviceName);
                default:
                    return DecodeGenericUdsService(bytes, baseSid, isResponse, serviceName);
            }
        }

        private DecodedPayload DecodeDiagnosticSessionControl(byte[] bytes, bool isResponse, string serviceName)
        {
            var result = CreateUdsResult(bytes, serviceName, isResponse);

            if (bytes.Length < 2)
            {
                result.Summary = $"{serviceName} (incomplete)";
                result.InterpretationConfidence = "Partial";
                result.Reasons.Add("Insufficient length for session type (need 2 bytes)");
                return result;
            }

            byte sessionType = bytes[1];
            result.Fields["SessionType"] = $"0x{sessionType:X2}";
            result.Fields["SessionName"] = SessionTypes.ContainsKey(sessionType) 
                ? SessionTypes[sessionType] 
                : "UNKNOWN_SESSION";

            result.Summary = $"{serviceName}: {result.Fields["SessionName"]}";
            result.InterpretationConfidence = SessionTypes.ContainsKey(sessionType) ? "Exact" : "Partial";
            result.Details.Add($"Session: {result.Fields["SessionName"]} (0x{sessionType:X2})");

            if (!SessionTypes.ContainsKey(sessionType))
            {
                result.Reasons.Add($"Session type 0x{sessionType:X2} not in standard dictionary (may be OEM-specific)");
            }

            if (isResponse && bytes.Length >= 6)
            {
                // Parse timing parameters (P2/P2* if present)
                ushort p2ServerMax = (ushort)((bytes[2] << 8) | bytes[3]);
                ushort p2StarServerMax = (ushort)((bytes[4] << 8) | bytes[5]);
                result.Fields["P2ServerMax"] = $"{p2ServerMax} ms";
                result.Fields["P2StarServerMax"] = $"{p2StarServerMax * 10} ms";
                result.Details.Add($"Timing: P2={p2ServerMax}ms, P2*={p2StarServerMax * 10}ms");
            }

            return result;
        }

        private DecodedPayload DecodeEcuReset(byte[] bytes, bool isResponse, string serviceName)
        {
            var result = CreateUdsResult(bytes, serviceName, isResponse);

            if (bytes.Length < 2)
            {
                result.Summary = $"{serviceName} (incomplete)";
                result.InterpretationConfidence = "Partial";
                result.Reasons.Add("Insufficient length for reset type (need 2 bytes)");
                return result;
            }

            byte resetType = bytes[1];
            result.Fields["ResetType"] = $"0x{resetType:X2}";
            result.Fields["ResetName"] = ResetTypes.ContainsKey(resetType) 
                ? ResetTypes[resetType] 
                : "UNKNOWN_RESET";

            result.Summary = $"{serviceName}: {result.Fields["ResetName"]}";
            result.InterpretationConfidence = ResetTypes.ContainsKey(resetType) ? "Exact" : "Partial";
            result.Details.Add($"Reset type: {result.Fields["ResetName"]} (0x{resetType:X2})");

            if (!ResetTypes.ContainsKey(resetType))
            {
                result.Reasons.Add($"Reset type 0x{resetType:X2} not in standard dictionary (may be OEM-specific)");
            }

            if (isResponse && bytes.Length > 2)
            {
                result.Details.Add($"Power down time: {bytes[2]} seconds");
                result.Fields["PowerDownTime"] = $"{bytes[2]} sec";
            }

            return result;
        }

        private DecodedPayload DecodeClearDiagnosticInformation(byte[] bytes, bool isResponse, string serviceName)
        {
            var result = CreateUdsResult(bytes, serviceName, isResponse);

            if (!isResponse && bytes.Length >= 4)
            {
                uint groupOfDtc = (uint)((bytes[1] << 16) | (bytes[2] << 8) | bytes[3]);
                result.Fields["GroupOfDTC"] = $"0x{groupOfDtc:X6}";
                result.Summary = $"{serviceName}: Group 0x{groupOfDtc:X6}";
                result.InterpretationConfidence = "Exact";
                result.Details.Add($"DTC group: 0x{groupOfDtc:X6}");
            }
            else if (isResponse)
            {
                result.Summary = $"{serviceName}: Success";
                result.InterpretationConfidence = "Exact";
            }
            else
            {
                result.Summary = $"{serviceName} (incomplete)";
                result.InterpretationConfidence = "Partial";
                result.Reasons.Add("Insufficient length for groupOfDTC (need 4 bytes)");
            }

            return result;
        }

        private DecodedPayload DecodeReadDtcInformation(byte[] bytes, bool isResponse, string serviceName)
        {
            var result = CreateUdsResult(bytes, serviceName, isResponse);

            if (bytes.Length < 2)
            {
                result.Summary = $"{serviceName} (incomplete)";
                result.InterpretationConfidence = "Partial";
                result.Reasons.Add("Insufficient length for subfunction (need 2 bytes)");
                return result;
            }

            byte subFunction = bytes[1];
            result.Fields["SubFunction"] = $"0x{subFunction:X2}";
            result.Fields["SubFunctionName"] = ReadDtcSubfunctions.ContainsKey(subFunction) 
                ? ReadDtcSubfunctions[subFunction] 
                : "UNKNOWN_SUBFUNCTION";

            result.Summary = $"{serviceName}: {result.Fields["SubFunctionName"]}";
            result.InterpretationConfidence = ReadDtcSubfunctions.ContainsKey(subFunction) ? "Exact" : "Partial";
            result.Details.Add($"Subfunction: {result.Fields["SubFunctionName"]} (0x{subFunction:X2})");

            if (!ReadDtcSubfunctions.ContainsKey(subFunction))
            {
                result.Reasons.Add($"Subfunction 0x{subFunction:X2} not in standard dictionary");
            }

            if (bytes.Length > 2)
            {
                result.Details.Add($"Additional data: {bytes.Length - 2} bytes (DTC records)");
                result.Fields["DataLength"] = $"{bytes.Length - 2} bytes";
            }

            return result;
        }

        private DecodedPayload DecodeReadDataByIdentifier(byte[] bytes, bool isResponse, string serviceName)
        {
            var result = CreateUdsResult(bytes, serviceName, isResponse);

            int minLength = isResponse ? 3 : 3; // Need SID + DID (2 bytes)

            if (bytes.Length < minLength)
            {
                result.Summary = $"{serviceName} (incomplete)";
                result.InterpretationConfidence = "Partial";
                result.Reasons.Add($"Insufficient length for DID (need {minLength} bytes)");
                return result;
            }

            ushort did = (ushort)((bytes[1] << 8) | bytes[2]);
            result.Fields["DID"] = $"0x{did:X4}";
            result.Fields["DIDName"] = DidDictionary.ContainsKey(did) 
                ? DidDictionary[did] 
                : "UNKNOWN_DID";

            result.Summary = $"{serviceName}: DID 0x{did:X4}";

            if (DidDictionary.ContainsKey(did))
            {
                result.Summary += $" ({DidDictionary[did]})";
                result.InterpretationConfidence = "Exact";
                result.Details.Add($"DID: {DidDictionary[did]} (0x{did:X4})");
            }
            else
            {
                result.InterpretationConfidence = "Partial";
                result.Details.Add($"DID: 0x{did:X4} (not in dictionary)");
                result.Reasons.Add($"DID 0x{did:X4} not in configured DID dictionary (interpretation unavailable)");
            }

            if (isResponse && bytes.Length > 3)
            {
                int dataLength = bytes.Length - 3;
                result.Fields["DataLength"] = $"{dataLength} bytes";
                result.Details.Add($"Data: {dataLength} bytes (raw hex: {BitConverter.ToString(bytes, 3).Replace("-", " ")})");
                
                // Don't interpret data unless DID is known
                if (!DidDictionary.ContainsKey(did))
                {
                    result.Reasons.Add("Data interpretation unavailable without known DID mapping");
                }
            }

            return result;
        }

        private DecodedPayload DecodeWriteDataByIdentifier(byte[] bytes, bool isResponse, string serviceName)
        {
            var result = CreateUdsResult(bytes, serviceName, isResponse);

            if (!isResponse && bytes.Length < 4)
            {
                result.Summary = $"{serviceName} (incomplete)";
                result.InterpretationConfidence = "Partial";
                result.Reasons.Add("Insufficient length for WDBI (need DID + at least 1 data byte)");
                return result;
            }

            if (bytes.Length >= 3)
            {
                ushort did = (ushort)((bytes[1] << 8) | bytes[2]);
                result.Fields["DID"] = $"0x{did:X4}";
                result.Fields["DIDName"] = DidDictionary.ContainsKey(did) 
                    ? DidDictionary[did] 
                    : "UNKNOWN_DID";

                result.Summary = $"{serviceName}: DID 0x{did:X4}";

                if (DidDictionary.ContainsKey(did))
                {
                    result.Summary += $" ({DidDictionary[did]})";
                    result.InterpretationConfidence = "Exact";
                    result.Details.Add($"DID: {DidDictionary[did]} (0x{did:X4})");
                }
                else
                {
                    result.InterpretationConfidence = "Partial";
                    result.Details.Add($"DID: 0x{did:X4} (not in dictionary)");
                    result.Reasons.Add($"DID 0x{did:X4} not in configured DID dictionary");
                }

                if (!isResponse && bytes.Length > 3)
                {
                    int dataLength = bytes.Length - 3;
                    result.Fields["DataLength"] = $"{dataLength} bytes";
                    result.Details.Add($"Write data: {dataLength} bytes");
                }
            }

            if (isResponse)
            {
                result.Summary += " - Write confirmed";
            }

            return result;
        }

        private DecodedPayload DecodeSecurityAccess(byte[] bytes, bool isResponse, string serviceName)
        {
            var result = CreateUdsResult(bytes, serviceName, isResponse);

            if (bytes.Length < 2)
            {
                result.Summary = $"{serviceName} (incomplete)";
                result.InterpretationConfidence = "Partial";
                result.Reasons.Add("Insufficient length for security level (need 2 bytes)");
                return result;
            }

            byte subFunction = bytes[1];
            bool isSeedRequest = (subFunction % 2) == 1;
            int level = (subFunction + 1) / 2;

            result.Fields["SubFunction"] = $"0x{subFunction:X2}";
            result.Fields["Level"] = level.ToString();
            result.Fields["Type"] = isSeedRequest ? "RequestSeed" : "SendKey";

            result.Summary = $"{serviceName}: {result.Fields["Type"]} (Level {level})";
            result.InterpretationConfidence = "Exact";
            result.Details.Add($"Security level: {level}");
            result.Details.Add($"Operation: {result.Fields["Type"]}");

            if (bytes.Length > 2)
            {
                int dataLength = bytes.Length - 2;
                result.Fields["DataLength"] = $"{dataLength} bytes";

                if (isSeedRequest && isResponse)
                {
                    result.Details.Add($"Seed: {dataLength} bytes (opaque - not validated)");
                    result.Reasons.Add("Seed bytes are opaque - no interpretation available");
                }
                else if (!isSeedRequest && !isResponse)
                {
                    result.Details.Add($"Key: {dataLength} bytes (opaque - cannot verify correctness)");
                    result.Reasons.Add("Key bytes are opaque - cannot determine validity without ECU response");
                }
            }

            return result;
        }

        private DecodedPayload DecodeRoutineControl(byte[] bytes, bool isResponse, string serviceName)
        {
            var result = CreateUdsResult(bytes, serviceName, isResponse);

            if (bytes.Length < 4)
            {
                result.Summary = $"{serviceName} (incomplete)";
                result.InterpretationConfidence = "Partial";
                result.Reasons.Add("Insufficient length for routine control (need 4 bytes: SID + Type + RoutineID)");
                return result;
            }

            byte routineType = bytes[1];
            ushort routineId = (ushort)((bytes[2] << 8) | bytes[3]);

            result.Fields["RoutineType"] = $"0x{routineType:X2}";
            result.Fields["RoutineTypeName"] = RoutineControlTypes.ContainsKey(routineType) 
                ? RoutineControlTypes[routineType] 
                : "UNKNOWN_TYPE";
            result.Fields["RoutineID"] = $"0x{routineId:X4}";
            result.Fields["RoutineIDName"] = RoutineIdDictionary.ContainsKey(routineId) 
                ? RoutineIdDictionary[routineId] 
                : "UNKNOWN_ROUTINE";

            result.Summary = $"{serviceName}: {result.Fields["RoutineTypeName"]} - Routine 0x{routineId:X4}";

            if (RoutineIdDictionary.ContainsKey(routineId))
            {
                result.Summary += $" ({RoutineIdDictionary[routineId]})";
                result.InterpretationConfidence = "Exact";
                result.Details.Add($"Routine: {RoutineIdDictionary[routineId]} (0x{routineId:X4})");
            }
            else
            {
                result.InterpretationConfidence = "Partial";
                result.Details.Add($"Routine ID: 0x{routineId:X4} (not in dictionary)");
                result.Reasons.Add($"Routine ID 0x{routineId:X4} not in configured routine dictionary");
            }

            result.Details.Add($"Control type: {result.Fields["RoutineTypeName"]}");

            if (bytes.Length > 4)
            {
                int dataLength = bytes.Length - 4;
                result.Fields["RoutineDataLength"] = $"{dataLength} bytes";
                result.Details.Add($"Routine data: {dataLength} bytes (opaque without routine definition)");
                result.Reasons.Add("Routine-specific data cannot be interpreted without routine specification");
            }

            return result;
        }

        private DecodedPayload DecodeGenericUdsService(byte[] bytes, byte baseSid, bool isResponse, string serviceName)
        {
            var result = CreateUdsResult(bytes, serviceName, isResponse);

            result.Summary = $"{serviceName}";
            result.InterpretationConfidence = "Partial";
            result.Details.Add($"Service ID: 0x{baseSid:X2}");
            result.Reasons.Add($"No specific decoder implemented for service 0x{baseSid:X2}");

            if (bytes.Length > 1)
            {
                result.Details.Add($"Payload: {bytes.Length - 1} bytes");
                result.Fields["PayloadLength"] = $"{bytes.Length - 1} bytes";
            }

            return result;
        }

        private DecodedPayload CreateUdsResult(byte[] bytes, string serviceName, bool isResponse)
        {
            var result = new DecodedPayload
            {
                Kind = "UDS",
                Bytes = bytes,
                Fields = new Dictionary<string, string>
                {
                    { "ServiceName", serviceName },
                    { "MessageType", isResponse ? "Response" : "Request" },
                    { "SID", $"0x{bytes[0]:X2}" }
                }
            };

            return result;
        }

        private DecodedPayload CreateUnknownResult(byte[] bytes, string reason)
        {
            return new DecodedPayload
            {
                Kind = "UNKNOWN",
                Bytes = bytes,
                Summary = "Unknown payload format",
                InterpretationConfidence = "Unknown",
                Reasons = new List<string> { reason }
            };
        }

        #endregion

        #region Built-in Test Vectors

        /// <summary>
        /// Runs built-in test vectors to validate decoder functionality
        /// Returns list of test results (pass/fail with details)
        /// </summary>
        public List<string> RunSelfTests()
        {
            var results = new List<string>();

            // Test 0: CRITICAL - Decimal CSV input that was mis-decoded
            results.Add(TestVector(
                "Decimal CSV ISO-TP (failing case)",
                "2,203,006,208",
                expectedKind: "ISO-TP",
                expectedConfidence: "Exact",
                expectedSummaryContains: "Non-UDS payload",
                expectedReasonContains: "not a valid UDS Service ID"
            ));

            // Test 0b: CRITICAL - Ford CAN frame format with Negative Response
            results.Add(TestVector(
                "Ford CAN Frame - Negative Response (000007D87F2231)",
                "000007D87F2231",
                expectedKind: "UDS",
                expectedConfidence: "Exact",
                expectedFields: new Dictionary<string, string>
                {
                    { "MessageType", "NegativeResponse" },
                    { "RequestSID", "0x22" },
                    { "NRC", "0x31" },
                    { "NRCName", "RequestOutOfRange" },
                    { "CanID", "0x7D8" },
                    { "Direction", "Response from ECU" }
                }
            ));

            // Test 0c: Ford CAN Frame - Request to ECU
            results.Add(TestVector(
                "Ford CAN Frame - Request (000007D02201F190)",
                "000007D02201F190",
                expectedKind: "UDS",
                expectedConfidence: "Partial",
                expectedFields: new Dictionary<string, string>
                {
                    { "ServiceName", "ReadDataByIdentifier" },
                    { "CanID", "0x7D0" },
                    { "Direction", "Request to ECU" }
                }
            ));

            // Test 1: Positive RDBI with known DID (wrapped in ISO-TP Single Frame)
            results.Add(TestVector(
                "ISO-TP Single Frame with UDS RDBI",
                "03 22 F1 90",
                expectedKind: "UDS",
                expectedConfidence: "Partial",
                expectedFields: new Dictionary<string, string>
                {
                    { "ServiceName", "ReadDataByIdentifier" },
                    { "DID", "0xF190" }
                }
            ));

            // Test 2: Negative response (wrapped in ISO-TP)
            results.Add(TestVector(
                "ISO-TP Single Frame with Negative Response",
                "03 7F 22 31",
                expectedKind: "UDS",
                expectedConfidence: "Exact",
                expectedFields: new Dictionary<string, string>
                {
                    { "MessageType", "NegativeResponse" },
                    { "RequestSID", "0x22" },
                    { "NRC", "0x31" },
                    { "NRCName", "RequestOutOfRange" }
                }
            ));

            // Test 3: ISO-TP First Frame (multi-frame)
            results.Add(TestVector(
                "ISO-TP First Frame",
                "10 14 49 02 01 31 47 31",
                expectedKind: "ISO-TP",
                expectedConfidence: "Partial",
                expectedSummaryContains: "First Frame",
                expectedReasonContains: "reassembly"
            ));

            // Test 4: ISO-TP Consecutive Frame
            results.Add(TestVector(
                "ISO-TP Consecutive Frame",
                "21 4A 54 33 48 35 38 35",
                expectedKind: "ISO-TP",
                expectedConfidence: "Partial",
                expectedSummaryContains: "Consecutive Frame",
                expectedReasonContains: "reassembly"
            ));

            // Test 5: ISO-TP Flow Control
            results.Add(TestVector(
                "ISO-TP Flow Control",
                "30 00 00",
                expectedKind: "ISO-TP",
                expectedConfidence: "Exact",
                expectedSummaryContains: "Flow Control"
            ));

            // Test 6: SecurityAccess seed request (in ISO-TP SF)
            results.Add(TestVector(
                "ISO-TP SF with SecurityAccess",
                "02 27 01",
                expectedKind: "UDS",
                expectedConfidence: "Exact",
                expectedFields: new Dictionary<string, string>
                {
                    { "ServiceName", "SecurityAccess" },
                    { "Type", "RequestSeed" }
                }
            ));

            // Test 7: DiagnosticSessionControl (in ISO-TP SF)
            results.Add(TestVector(
                "ISO-TP SF with SessionControl",
                "02 10 03",
                expectedKind: "UDS",
                expectedConfidence: "Exact",
                expectedFields: new Dictionary<string, string>
                {
                    { "ServiceName", "DiagnosticSessionControl" },
                    { "SessionName", "ExtendedDiagnosticSession" }
                }
            ));

            // Test 8: ASCII text (should not be decoded as ISO-TP/UDS)
            results.Add(TestVector(
                "ASCII text",
                "OK: CAL COMPLETE",
                expectedKind: "ASCII",
                expectedConfidence: "Exact",
                expectedFields: new Dictionary<string, string>
                {
                    { "Text", "OK: CAL COMPLETE" }
                }
            ));

            // Test 9: Hex format variants
            results.Add(TestVector(
                "Hex with 0x prefix",
                "0x02 0xCB 0x06 0xD0",
                expectedKind: "ISO-TP",
                expectedConfidence: "Exact",
                expectedSummaryContains: "Non-UDS payload"
            ));

            // Test 10: Binary string
            results.Add(TestVector(
                "Binary string to ISO-TP",
                "00000010 11001011 00000110 11010000",
                expectedKind: "ISO-TP",
                expectedConfidence: "Exact",
                expectedSummaryContains: "Non-UDS payload"
            ));

            return results;
        }

        private string TestVector(
            string testName,
            string input,
            string expectedKind,
            string expectedConfidence,
            Dictionary<string, string> expectedFields = null,
            string expectedReasonContains = null,
            string expectedSummaryContains = null)
        {
            try
            {
                var decoder = new AutomotivePayloadDecoder();
                var result = decoder.TryDecodeFromLine(input);

                var passed = true;
                var issues = new List<string>();

                if (result.Kind != expectedKind)
                {
                    passed = false;
                    issues.Add($"Kind mismatch: expected '{expectedKind}', got '{result.Kind}'");
                }

                if (result.InterpretationConfidence != expectedConfidence)
                {
                    passed = false;
                    issues.Add($"Confidence mismatch: expected '{expectedConfidence}', got '{result.InterpretationConfidence}'");
                }

                if (expectedFields != null)
                {
                    foreach (var field in expectedFields)
                    {
                        if (!result.Fields.ContainsKey(field.Key))
                        {
                            passed = false;
                            issues.Add($"Missing field: '{field.Key}'");
                        }
                        else if (result.Fields[field.Key] != field.Value)
                        {
                            passed = false;
                            issues.Add($"Field '{field.Key}' mismatch: expected '{field.Value}', got '{result.Fields[field.Key]}'");
                        }
                    }
                }

                if (expectedReasonContains != null)
                {
                    var foundReason = result.Reasons.Any(r => r.Contains(expectedReasonContains));
                    if (!foundReason)
                    {
                        passed = false;
                        issues.Add($"Expected reason containing '{expectedReasonContains}' not found");
                    }
                }

                if (expectedSummaryContains != null)
                {
                    if (!result.Summary.Contains(expectedSummaryContains))
                    {
                        passed = false;
                        issues.Add($"Expected summary containing '{expectedSummaryContains}', got '{result.Summary}'");
                    }
                }

                if (passed)
                {
                    return $"✅ PASS: {testName}";
                }
                else
                {
                    return $"❌ FAIL: {testName}\n   Issues: {string.Join("; ", issues)}";
                }
            }
            catch (Exception ex)
            {
                return $"❌ ERROR: {testName}\n   Exception: {ex.Message}";
            }
        }

        #endregion
    }
}
