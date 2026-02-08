namespace AutoTriage.Core
{
    /// <summary>
    /// Defines the severity levels that can be assigned to a log finding.
    /// 
    /// DESIGN INTENT:
    /// This enum is used to classify findings discovered during log analysis
    /// so that higher-risk issues (security, programming failure, ECU instability)
    /// can be prioritized during triage.
    /// 
    /// This enum intentionally remains simple and readable so it can be used by:
    /// - Automated analyzers
    /// - UI layers (coloring, sorting)
    /// - Reports and exports
    /// - Non-developer reviewers (technicians, managers)
    /// </summary>
    public enum FindingSeverity
    {
        /// <summary>
        /// Represents a successful or expected condition.
        /// 
        /// Examples:
        /// - Programming completed successfully
        /// - Session transitioned correctly
        /// - ECU responded as expected
        /// 
        /// These findings are typically informational and not actionable.
        /// </summary>
        Success = 0,

        /// <summary>
        /// Represents a non-fatal condition that may require attention.
        /// 
        /// Examples:
        /// - Temporary timeout
        /// - Retry condition
        /// - Unsupported request that does not block progress
        /// 
        /// Warnings should be reviewed but do not usually stop work.
        /// </summary>
        Warning = 1,

        /// <summary>
        /// Represents an error condition that caused an operation to fail.
        /// 
        /// Examples:
        /// - Failed diagnostic request
        /// - Programming step failure
        /// - Invalid response from ECU
        /// 
        /// Errors are actionable but may still be recoverable.
        /// </summary>
        Error = 2,

        /// <summary>
        /// Represents a critical "stop-the-line" condition.
        /// 
        /// Examples:
        /// - Watchdog resets or repeated ECU crashes
        /// - Secure boot or signature verification failures
        /// - SecurityAccess lockouts or brute-force patterns
        /// - Bus-off or gateway communication loss
        /// - Flash programming or OTA integrity failures
        /// 
        /// Critical findings indicate high risk to:
        /// - Vehicle safety
        /// - System security
        /// - ECU stability
        /// 
        /// These findings should be escalated immediately.
        /// </summary>
        Critical = 3
    }
}
