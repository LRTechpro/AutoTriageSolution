namespace AutoTriage.Models
{
    /// <summary>
    /// Represents a single line from a log file with its metadata.
    /// This is the most granular unit in the data model.
    /// </summary>
    public class LogLine
    {
        /// <summary>
        /// Line number within the source file (1-based indexing).
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// The complete raw text of this log line as it appeared in the file.
        /// </summary>
        public string RawText { get; set; }

        /// <summary>
        /// Reference back to the source log file this line came from.
        /// Useful for multi-file scenarios to identify the source.
        /// Null if not yet assigned to a log file.
        /// </summary>
        public LogFile? SourceFile { get; set; }

        /// <summary>
        /// Extracted timestamp from this line (if available).
        /// Null if no timestamp could be parsed.
        /// </summary>
        public System.DateTime? Timestamp { get; set; }

        /// <summary>
        /// Detected severity level for this line (from analysis).
        /// </summary>
        public FindingSeverity DetectedSeverity { get; set; }

        /// <summary>
        /// Indicates whether this line was flagged as a finding during analysis.
        /// </summary>
        public bool IsFinding { get; set; }

        /// <summary>
        /// Optional: extracted voltage value if this line contains voltage data.
        /// Null if no voltage detected.
        /// </summary>
        public double? VoltageValue { get; set; }

        /// <summary>
        /// Constructor with default values to prevent null reference errors.
        /// </summary>
        public LogLine()
        {
            // Initialize with empty string instead of null
            RawText = string.Empty;
            // Default to Info severity (lowest level)
            DetectedSeverity = FindingSeverity.Info;
            // Not a finding by default
            IsFinding = false;
        }
    }
}
