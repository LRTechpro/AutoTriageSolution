using System;
using System.Collections.Generic;

namespace AutoTriage.Core.Decoding
{
    /// <summary>
    /// Represents the result of ISO-TP (ISO 15765-2) frame parsing
    /// </summary>
    public class IsoTpResult
    {
        /// <summary>
        /// Frame type: SingleFrame, FirstFrame, ConsecutiveFrame, FlowControl, or Invalid
        /// </summary>
        public IsoTpFrameType FrameType { get; set; }

        /// <summary>
        /// Raw bytes of the entire frame
        /// </summary>
        public byte[] RawBytes { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// The PCI (Protocol Control Information) byte
        /// </summary>
        public byte Pci { get; set; }

        /// <summary>
        /// Declared payload length (from PCI)
        /// </summary>
        public int PayloadLength { get; set; }

        /// <summary>
        /// Extracted payload bytes (without PCI and padding)
        /// </summary>
        public byte[] Payload { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Confidence level: Exact, Partial, or Unknown
        /// </summary>
        public string Confidence { get; set; } = "Unknown";

        /// <summary>
        /// Reasons for partial/unknown interpretation
        /// </summary>
        public List<string> Reasons { get; set; } = new List<string>();

        /// <summary>
        /// Human-readable summary
        /// </summary>
        public string Summary { get; set; } = "";

        /// <summary>
        /// Additional details about the frame
        /// </summary>
        public List<string> Details { get; set; } = new List<string>();

        /// <summary>
        /// Sequence number (for Consecutive Frames)
        /// </summary>
        public int? SequenceNumber { get; set; }

        /// <summary>
        /// Flow status (for Flow Control frames)
        /// </summary>
        public byte? FlowStatus { get; set; }

        /// <summary>
        /// Returns formatted string representation
        /// </summary>
        public string ToFormattedString()
        {
            var lines = new List<string>
            {
                "═══════════════════════════════════════════════════════════════",
                $"ISO-TP Frame Type: {FrameType}",
                $"Confidence: {Confidence}",
                $"Raw Bytes: {BitConverter.ToString(RawBytes).Replace("-", " ")}",
                $"PCI Byte: 0x{Pci:X2}",
                ""
            };

            if (PayloadLength > 0)
            {
                lines.Add($"Declared Payload Length: {PayloadLength} bytes");
            }

            if (Payload.Length > 0)
            {
                lines.Add($"Extracted Payload: {BitConverter.ToString(Payload).Replace("-", " ")}");
                lines.Add($"Payload Length: {Payload.Length} bytes");
            }

            if (SequenceNumber.HasValue)
            {
                lines.Add($"Sequence Number: {SequenceNumber.Value}");
            }

            if (FlowStatus.HasValue)
            {
                lines.Add($"Flow Status: 0x{FlowStatus.Value:X2}");
            }

            lines.Add("");
            lines.Add($"Summary: {Summary}");
            lines.Add("");

            if (Details.Count > 0)
            {
                lines.Add("Details:");
                foreach (var detail in Details)
                {
                    lines.Add($"  • {detail}");
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

            lines.Add("═══════════════════════════════════════════════════════════════");

            return string.Join(Environment.NewLine, lines);
        }
    }

    /// <summary>
    /// ISO-TP frame types
    /// </summary>
    public enum IsoTpFrameType
    {
        Invalid,
        SingleFrame,      // 0x0n
        FirstFrame,       // 0x1n
        ConsecutiveFrame, // 0x2n
        FlowControl       // 0x3n
    }
}
