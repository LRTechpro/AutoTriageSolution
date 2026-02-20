using System;

namespace AutoTriage.Models
{
    /// <summary>
    /// Canonical data model for a parsed log line.
    /// Supports multi-log scenarios with consistent global numbering.
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// Global line number across ALL loaded logs (1..N).
        /// This number is immutable and never changes due to filtering or sorting.
        /// </summary>
        public int GlobalLineNumber { get; set; }

        /// <summary>
        /// Unique identifier for the source log this line came from.
        /// </summary>
        public Guid SourceLogId { get; set; }

        /// <summary>
        /// Name of the source log file or "Pasted (n)" for pasted content.
        /// </summary>
        public string SourceFileName { get; set; } = "";

        /// <summary>
        /// Line number within the source file (1..M for that specific file).
        /// </summary>
        public int SourceLocalLineNumber { get; set; }

        /// <summary>
        /// Extracted timestamp from the log line (if available).
        /// </summary>
        public DateTime? Timestamp { get; set; }

        /// <summary>
        /// Detected severity level (Critical, Error, Warning, Success, Info, Unknown).
        /// </summary>
        public string Severity { get; set; } = "Unknown";

        /// <summary>
        /// Code or finding type (e.g., "NRC", "ERROR", "KEYWORD").
        /// </summary>
        public string Code { get; set; } = "";

        /// <summary>
        /// The complete raw text of this log line.
        /// </summary>
        public string RawText { get; set; } = "";

        /// <summary>
        /// Whether this line is marked as a finding.
        /// </summary>
        public bool IsFinding { get; set; }

        /// <summary>
        /// Display color for this row in the grid.
        /// </summary>
        public System.Drawing.Color RowColor { get; set; } = System.Drawing.Color.White;
    }
}
