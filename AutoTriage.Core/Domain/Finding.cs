namespace AutoTriage.Core
{
    /// <summary>
    /// Represents a single log entry that was identified during analysis.
    /// This class is a reference type used to transfer structured data
    /// from the DLL to the GUI application.
    /// </summary>
    public class Finding
    {
        /// <summary>
        /// The line number in the original log where this finding occurred.
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// The severity level assigned to this finding.
        /// </summary>
        public FindingSeverity Severity { get; set; }

        /// <summary>
        /// A short code used to categorize the type of finding.
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// The full log message associated with this finding.
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }
}

