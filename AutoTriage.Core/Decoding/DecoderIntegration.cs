using System;
using System.Collections.Generic;
using AutoTriage.Core.Decoding;

namespace AutoTriage.Core
{
    /// <summary>
    /// Integration helpers for automotive payload decoding in the AutoTriage pipeline
    /// </summary>
    public static class DecoderIntegration
    {
        private static readonly AutomotivePayloadDecoder _decoder = new AutomotivePayloadDecoder();

        /// <summary>
        /// Attempts to decode automotive payloads from a finding's line text.
        /// Returns null if no payload detected or decoding yields no useful information.
        /// </summary>
        public static DecodedPayload TryDecodeFromFinding(Finding finding)
        {
            if (finding == null || string.IsNullOrWhiteSpace(finding.LineText))
            {
                return null;
            }

            var result = _decoder.TryDecodeFromLine(finding.LineText);

            // Only return if we got something meaningful (not just UNKNOWN)
            if (result.Kind != "UNKNOWN" || result.Bytes.Length > 0)
            {
                return result;
            }

            return null;
        }

        /// <summary>
        /// Attempts to decode automotive payloads from any log line text.
        /// </summary>
        public static DecodedPayload TryDecodeFromLine(string lineText)
        {
            return _decoder.TryDecodeFromLine(lineText);
        }

        /// <summary>
        /// Gets a short summary suitable for tooltip or grid cell.
        /// Returns null if nothing useful to show.
        /// </summary>
        public static string GetShortSummary(Finding finding)
        {
            var decoded = TryDecodeFromFinding(finding);
            if (decoded == null || decoded.Kind == "UNKNOWN")
            {
                return null;
            }

            return $"[{decoded.Kind}] {decoded.Summary}";
        }

        /// <summary>
        /// Gets detailed formatted output for display in a message box or details pane.
        /// </summary>
        public static string GetDetailedOutput(Finding finding)
        {
            var decoded = TryDecodeFromFinding(finding);
            if (decoded == null)
            {
                return "No automotive payload detected in this line.";
            }

            return decoded.ToFormattedString();
        }

        /// <summary>
        /// Runs built-in self-tests for the decoder.
        /// Returns list of test results.
        /// </summary>
        public static List<string> RunDecoderSelfTests()
        {
            return _decoder.RunSelfTests();
        }
    }
}
