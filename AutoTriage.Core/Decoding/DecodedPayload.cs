using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoTriage.Core.Decoding
{
    /// <summary>
    /// Represents the result of a deterministic payload decode operation.
    /// No guessing - all fields are proven from bytes + known dictionaries.
    /// </summary>
    public class DecodedPayload
    {
        /// <summary>
        /// Type of payload detected: "UDS" | "ASCII" | "HEX" | "BINARY" | "UNKNOWN"
        /// </summary>
        public string Kind { get; set; } = "UNKNOWN";

        /// <summary>
        /// The raw bytes of the payload
        /// </summary>
        public byte[] Bytes { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// One-line summary of what was decoded
        /// </summary>
        public string Summary { get; set; } = "";

        /// <summary>
        /// Detailed bullet facts about the payload (proven only)
        /// </summary>
        public List<string> Details { get; set; } = new List<string>();

        /// <summary>
        /// Confidence level: "Exact" | "Partial" | "Unknown"
        /// </summary>
        public string InterpretationConfidence { get; set; } = "Unknown";

        /// <summary>
        /// Reasons for partial/unknown interpretation (missing bytes, unknown IDs, etc.)
        /// </summary>
        public List<string> Reasons { get; set; } = new List<string>();

        /// <summary>
        /// Structured fields extracted from the payload (e.g., SID, ServiceName, DID, NRC, etc.)
        /// </summary>
        public Dictionary<string, string> Fields { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Returns formatted output for display
        /// </summary>
        public string ToFormattedString()
        {
            var lines = new List<string>
            {
                $"═══════════════════════════════════════════════════════════════",
                $"Kind: {Kind}",
                $"Confidence: {InterpretationConfidence}",
                $"Raw Hex: {BitConverter.ToString(Bytes).Replace("-", " ")}",
                $"Length: {Bytes.Length} bytes",
                $"",
                $"Summary: {Summary}",
                $""
            };

            if (Details.Count > 0)
            {
                lines.Add("Details:");
                foreach (var detail in Details)
                {
                    lines.Add($"  • {detail}");
                }
                lines.Add("");
            }

            if (Fields.Count > 0)
            {
                lines.Add("Parsed Fields:");
                foreach (var field in Fields.OrderBy(f => f.Key))
                {
                    lines.Add($"  {field.Key}: {field.Value}");
                }
                lines.Add("");
            }

            if (Reasons.Count > 0)
            {
                lines.Add("Interpretation Limitations:");
                foreach (var reason in Reasons)
                {
                    lines.Add($"  ⚠ {reason}");
                }
                lines.Add("");
            }

            lines.Add($"═══════════════════════════════════════════════════════════════");

            return string.Join(Environment.NewLine, lines);
        }
    }
}
