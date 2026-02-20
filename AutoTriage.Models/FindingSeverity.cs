namespace AutoTriage.Models
{
    /// <summary>
    /// Severity levels for findings discovered during log analysis.
    /// Ordered from most severe (Critical) to least severe (Info).
    /// </summary>
    public enum FindingSeverity
    {
        /// <summary>
        /// Informational message, no action required.
        /// </summary>
        Info = 0,

        /// <summary>
        /// Successful operation or validation passed.
        /// </summary>
        Success = 1,

        /// <summary>
        /// Warning condition that should be reviewed but not critical.
        /// </summary>
        Warning = 2,

        /// <summary>
        /// Error condition that requires attention.
        /// </summary>
        Error = 3,

        /// <summary>
        /// Critical failure that likely caused the issue being diagnosed.
        /// </summary>
        Critical = 4
    }
}
