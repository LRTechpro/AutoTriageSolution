using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoTriage.Core.Decoding
{
    /// <summary>
    /// Test harness for demonstrating ISO-TP/UDS decoder fixes
    /// </summary>
    public static class DecoderTestHarness
    {
        /// <summary>
        /// Demonstrates the fix for decimal CSV input "2,203,006,208"
        /// that was previously mis-decoded as UDS
        /// </summary>
        public static string DemonstrateFix()
        {
            var sb = new StringBuilder();
            var decoder = new AutomotivePayloadDecoder();

            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("DECODER FIX DEMONSTRATION: Decimal CSV Input");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine();

            // The failing case
            string input = "2,203,006,208";

            sb.AppendLine($"Input: \"{input}\"");
            sb.AppendLine();
            sb.AppendLine("EXPECTED BEHAVIOR:");
            sb.AppendLine("  - Parse as decimal CSV → bytes [02, CB, 06, D0]");
            sb.AppendLine("  - ISO-TP parse: Single Frame (PCI=0x02), payload length=2");
            sb.AppendLine("  - Extract payload: [CB, 06]");
            sb.AppendLine("  - UDS validation: 0xCB not in valid SID set");
            sb.AppendLine("  - Result: 'ISO-TP' with 'Non-UDS payload' message");
            sb.AppendLine();
            sb.AppendLine("───────────────────────────────────────────────────────────────");
            sb.AppendLine("ACTUAL DECODER OUTPUT:");
            sb.AppendLine("───────────────────────────────────────────────────────────────");
            sb.AppendLine();

            try
            {
                var result = decoder.TryDecodeFromLine(input);

                sb.AppendLine($"Kind: {result.Kind}");
                sb.AppendLine($"Summary: {result.Summary}");
                sb.AppendLine($"Confidence: {result.InterpretationConfidence}");
                sb.AppendLine($"Raw Bytes: {BitConverter.ToString(result.Bytes).Replace("-", " ")}");
                sb.AppendLine();

                if (result.Fields.Count > 0)
                {
                    sb.AppendLine("Fields:");
                    foreach (var field in result.Fields)
                    {
                        sb.AppendLine($"  {field.Key}: {field.Value}");
                    }
                    sb.AppendLine();
                }

                if (result.Details.Count > 0)
                {
                    sb.AppendLine("Details:");
                    foreach (var detail in result.Details)
                    {
                        sb.AppendLine($"  • {detail}");
                    }
                    sb.AppendLine();
                }

                if (result.Reasons.Count > 0)
                {
                    sb.AppendLine("Reasons:");
                    foreach (var reason in result.Reasons)
                    {
                        sb.AppendLine($"  ⚠ {reason}");
                    }
                    sb.AppendLine();
                }

                sb.AppendLine("───────────────────────────────────────────────────────────────");
                sb.AppendLine("VALIDATION:");
                sb.AppendLine("───────────────────────────────────────────────────────────────");

                bool isCorrect = result.Kind == "ISO-TP" && 
                                 result.Summary.Contains("Non-UDS") &&
                                 result.Reasons.Any(r => r.Contains("not a valid UDS Service ID"));

                if (isCorrect)
                {
                    sb.AppendLine("✅ PASS: Decoder correctly identified ISO-TP frame with non-UDS payload");
                    sb.AppendLine("✅ PASS: Did NOT mis-decode as UDS service 0x02 or 0xCB");
                    sb.AppendLine("✅ PASS: Properly validated SID before attempting UDS decode");
                }
                else
                {
                    sb.AppendLine("❌ FAIL: Decoder output does not match expected behavior");
                    if (result.Kind == "UDS")
                    {
                        sb.AppendLine("  ERROR: Incorrectly identified as UDS (should be ISO-TP)");
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"❌ ERROR: Exception occurred during decoding");
                sb.AppendLine($"  {ex.Message}");
                sb.AppendLine($"  {ex.StackTrace}");
            }

            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════════════════════════════");

            return sb.ToString();
        }

        /// <summary>
        /// Demonstrates Ford CAN frame format handling (0x07D8/0x07D0 prefix)
        /// </summary>
        public static string DemonstrateFordCanFrame()
        {
            var sb = new StringBuilder();
            var decoder = new AutomotivePayloadDecoder();

            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("FORD CAN FRAME FORMAT DEMONSTRATION");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine("Ford diagnostic logs include 4-byte CAN frame headers:");
            sb.AppendLine("  Bytes 0-3: [00 00 CAN_ID_HIGH CAN_ID_LOW]");
            sb.AppendLine("  - 0x07D8 = Response from ECU");
            sb.AppendLine("  - 0x07D0 = Request to ECU");
            sb.AppendLine();

            // Test case: Negative Response from ECU
            string input1 = "000007D87F2231";

            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("TEST 1: Negative Response from ECU");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine($"Input: \"{input1}\"");
            sb.AppendLine();
            sb.AppendLine("EXPECTED STRUCTURE:");
            sb.AppendLine("  Bytes 0-3: 00 00 07 D8 (CAN ID = 0x07D8, ECU Response)");
            sb.AppendLine("  Byte 4:    7F (Service ID = Negative Response)");
            sb.AppendLine("  Byte 5:    22 (Original service = ReadDataByIdentifier)");
            sb.AppendLine("  Byte 6:    31 (NRC = Request Out of Range)");
            sb.AppendLine();
            sb.AppendLine("───────────────────────────────────────────────────────────────");
            sb.AppendLine("ACTUAL DECODER OUTPUT:");
            sb.AppendLine("───────────────────────────────────────────────────────────────");

            try
            {
                var result = decoder.TryDecodeFromLine(input1);
                PrintDecodedResult(sb, result);

                sb.AppendLine("───────────────────────────────────────────────────────────────");
                sb.AppendLine("VALIDATION:");
                sb.AppendLine("───────────────────────────────────────────────────────────────");

                bool isCorrect = result.Kind == "UDS" &&
                                 result.Summary.Contains("Negative Response") &&
                                 result.Fields.ContainsKey("CanID") &&
                                 result.Fields["CanID"] == "0x7D8" &&
                                 result.Fields.ContainsKey("RequestSID") &&
                                 result.Fields["RequestSID"] == "0x22" &&
                                 result.Fields.ContainsKey("NRC") &&
                                 result.Fields["NRC"] == "0x31";

                if (isCorrect)
                {
                    sb.AppendLine("✅ PASS: CAN header correctly detected and stripped");
                    sb.AppendLine("✅ PASS: CAN ID 0x07D8 identified as 'Response from ECU'");
                    sb.AppendLine("✅ PASS: Service 0x7F recognized as Negative Response");
                    sb.AppendLine("✅ PASS: Original service 0x22 (RDBI) identified");
                    sb.AppendLine("✅ PASS: NRC 0x31 decoded as 'Request Out of Range'");
                }
                else
                {
                    sb.AppendLine("❌ FAIL: Decoder output does not match expected behavior");
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"❌ ERROR: {ex.Message}");
            }

            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("TEST 2: Request to ECU");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");

            string input2 = "000007D02201F190";
            sb.AppendLine($"Input: \"{input2}\"");
            sb.AppendLine();
            sb.AppendLine("EXPECTED STRUCTURE:");
            sb.AppendLine("  Bytes 0-3: 00 00 07 D0 (CAN ID = 0x07D0, Request to ECU)");
            sb.AppendLine("  Byte 4:    22 (Service ID = ReadDataByIdentifier)");
            sb.AppendLine("  Bytes 5-6: F1 90 (DID = 0xF190, VIN)");
            sb.AppendLine();
            sb.AppendLine("───────────────────────────────────────────────────────────────");
            sb.AppendLine("ACTUAL DECODER OUTPUT:");
            sb.AppendLine("───────────────────────────────────────────────────────────────");

            try
            {
                var result = decoder.TryDecodeFromLine(input2);
                PrintDecodedResult(sb, result);

                sb.AppendLine("───────────────────────────────────────────────────────────────");
                sb.AppendLine("VALIDATION:");
                sb.AppendLine("───────────────────────────────────────────────────────────────");

                bool isCorrect = result.Kind == "UDS" &&
                                 result.Summary.Contains("ReadDataByIdentifier") &&
                                 result.Fields.ContainsKey("CanID") &&
                                 result.Fields["CanID"] == "0x7D0" &&
                                 result.Fields.ContainsKey("DID") &&
                                 result.Fields["DID"] == "0xF190";

                if (isCorrect)
                {
                    sb.AppendLine("✅ PASS: CAN header correctly detected and stripped");
                    sb.AppendLine("✅ PASS: CAN ID 0x07D0 identified as 'Request to ECU'");
                    sb.AppendLine("✅ PASS: Service 0x22 recognized as ReadDataByIdentifier");
                    sb.AppendLine("✅ PASS: DID 0xF190 identified (VIN)");
                }
                else
                {
                    sb.AppendLine("❌ FAIL: Decoder output does not match expected behavior");
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"❌ ERROR: {ex.Message}");
            }

            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════════════════════════════");

            return sb.ToString();
        }

        private static void PrintDecodedResult(StringBuilder sb, DecodedPayload result)
        {
            sb.AppendLine($"Kind: {result.Kind}");
            sb.AppendLine($"Summary: {result.Summary}");
            sb.AppendLine($"Confidence: {result.InterpretationConfidence}");
            sb.AppendLine($"Raw Bytes: {BitConverter.ToString(result.Bytes).Replace("-", " ")}");
            sb.AppendLine();

            if (result.Fields.Count > 0)
            {
                sb.AppendLine("Fields:");
                foreach (var field in result.Fields)
                {
                    sb.AppendLine($"  {field.Key}: {field.Value}");
                }
                sb.AppendLine();
            }

            if (result.Details.Count > 0)
            {
                sb.AppendLine("Details:");
                foreach (var detail in result.Details)
                {
                    sb.AppendLine($"  • {detail}");
                }
                sb.AppendLine();
            }

            if (result.Reasons.Count > 0)
            {
                sb.AppendLine("Reasons:");
                foreach (var reason in result.Reasons)
                {
                    sb.AppendLine($"  ⚠ {reason}");
                }
                sb.AppendLine();
            }
        }

        /// <summary>
        /// Runs comparison tests showing correct ISO-TP → UDS vs ISO-TP → Non-UDS
        /// </summary>
        public static string RunComparisonTests()
        {
            var sb = new StringBuilder();
            var decoder = new AutomotivePayloadDecoder();

            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("ISO-TP/UDS DECODER COMPARISON TESTS");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine();

            var testCases = new[]
            {
                new { Name = "Decimal CSV → ISO-TP Non-UDS", Input = "2,203,006,208", ExpectedKind = "ISO-TP", ExpectedSummary = "Non-UDS" },
                new { Name = "Ford CAN Frame - Negative Response", Input = "000007D87F2231", ExpectedKind = "UDS", ExpectedSummary = "Negative Response" },
                new { Name = "Ford CAN Frame - RDBI Request", Input = "000007D02201F190", ExpectedKind = "UDS", ExpectedSummary = "ReadDataByIdentifier" },
                new { Name = "ISO-TP SF with UDS RDBI", Input = "03 22 F1 90", ExpectedKind = "UDS", ExpectedSummary = "ReadDataByIdentifier" },
                new { Name = "ISO-TP SF with Negative Response", Input = "03 7F 22 31", ExpectedKind = "UDS", ExpectedSummary = "Negative Response" },
                new { Name = "ISO-TP First Frame (Multi-frame)", Input = "10 14 49 02 01 31", ExpectedKind = "ISO-TP", ExpectedSummary = "First Frame" },
                new { Name = "ISO-TP Consecutive Frame", Input = "21 4A 54 33 48 35", ExpectedKind = "ISO-TP", ExpectedSummary = "Consecutive Frame" },
                new { Name = "ISO-TP Flow Control", Input = "30 00 00", ExpectedKind = "ISO-TP", ExpectedSummary = "Flow Control" },
                new { Name = "Hex with 0x prefix", Input = "0x02 0xCB 0x06 0xD0", ExpectedKind = "ISO-TP", ExpectedSummary = "Non-UDS" }
            };

            foreach (var testCase in testCases)
            {
                sb.AppendLine($"Test: {testCase.Name}");
                sb.AppendLine($"Input: {testCase.Input}");

                try
                {
                    var result = decoder.TryDecodeFromLine(testCase.Input);

                    bool kindMatch = result.Kind == testCase.ExpectedKind;
                    bool summaryMatch = result.Summary.Contains(testCase.ExpectedSummary);

                    string status = kindMatch && summaryMatch ? "✅ PASS" : "❌ FAIL";
                    sb.AppendLine($"{status} - Kind: {result.Kind}, Summary: {result.Summary}");

                    if (!kindMatch)
                    {
                        sb.AppendLine($"  Expected kind: {testCase.ExpectedKind}");
                    }
                    if (!summaryMatch)
                    {
                        sb.AppendLine($"  Expected summary to contain: {testCase.ExpectedSummary}");
                    }
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"❌ ERROR - {ex.Message}");
                }

                sb.AppendLine();
            }

            sb.AppendLine("═══════════════════════════════════════════════════════════════");

            return sb.ToString();
        }

        /// <summary>
        /// Validates that valid UDS messages wrapped in ISO-TP still decode correctly
        /// </summary>
        public static string ValidateUdsStillWorks()
        {
            var sb = new StringBuilder();
            var decoder = new AutomotivePayloadDecoder();

            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("VALIDATION: UDS Messages Still Decode Correctly");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine();

            var udsTestCases = new[]
            {
                new { Input = "02 10 03", Service = "DiagnosticSessionControl", Description = "Session Control Request" },
                new { Input = "06 50 03 00 32 01 F4", Service = "DiagnosticSessionControl", Description = "Session Control Response with timing" },
                new { Input = "02 27 01", Service = "SecurityAccess", Description = "Request Seed" },
                new { Input = "06 67 01 12 34 56 78", Service = "SecurityAccess", Description = "Seed Response" },
                new { Input = "03 22 F1 90", Service = "ReadDataByIdentifier", Description = "Read VIN" },
                new { Input = "03 7F 22 31", Service = "NegativeResponse", Description = "Request Out of Range" }
            };

            int passed = 0;
            int failed = 0;

            foreach (var testCase in udsTestCases)
            {
                sb.AppendLine($"Test: {testCase.Description}");
                sb.AppendLine($"Input: {testCase.Input}");

                try
                {
                    var result = decoder.TryDecodeFromLine(testCase.Input);

                    bool isUds = result.Kind == "UDS";
                    bool correctService = result.Summary.Contains(testCase.Service);

                    if (isUds && correctService)
                    {
                        sb.AppendLine($"✅ PASS - Correctly decoded as UDS: {result.Summary}");
                        passed++;
                    }
                    else
                    {
                        sb.AppendLine($"❌ FAIL - Kind: {result.Kind}, Summary: {result.Summary}");
                        sb.AppendLine($"  Expected: UDS with {testCase.Service}");
                        failed++;
                    }
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"❌ ERROR - {ex.Message}");
                    failed++;
                }

                sb.AppendLine();
            }

            sb.AppendLine("───────────────────────────────────────────────────────────────");
            sb.AppendLine($"Results: {passed} passed, {failed} failed");
            sb.AppendLine("───────────────────────────────────────────────────────────────");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");

            return sb.ToString();
        }
    }
}
