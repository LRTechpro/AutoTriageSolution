namespace AutoTriage.Core
{
    /// <summary>
    /// Defines the severity levels that can be assigned to a log finding.
    /// This enum is a value type and is used throughout the analysis process
    /// to classify the importance of detected log entries.
    /// </summary>
    public enum FindingSeverity
    {
        /// <summary>
        /// Represents a critical error or failure condition.
        /// </summary>
        Error,

        /// <summary>
        /// Represents a non-fatal condition that may require attention.
        /// </summary>
        Warning,

        /// <summary>
        /// Represents a successful or completed operation.
        /// </summary>
        Success
    }
}

