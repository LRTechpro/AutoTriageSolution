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
        /// - Battery voltage below optimal threshold but still functional
        /// - State of charge below recommended levels
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
        /// Errors represent actionable failures that need investigation.
        /// </summary>
        Error = 2,

        /// <summary>
        /// Represents a stop-the-line condition requiring immediate action.
        /// 
        /// Examples:
        /// - ECU watchdog reset / crash
        /// - Memory corruption detected
        /// - Secure boot failure
        /// - Critical voltage drop that may cause ECU brownout
        /// - State of charge critically low (system may shut down)
        /// - Security breach indicators
        /// - Flash programming pipeline failure
        /// 
        /// Critical findings indicate systemic issues that:
        /// - Threaten vehicle safety or reliability
        /// - May result in ECU instability or data loss
        /// - Indicate security compromise or tampering
        /// - Require immediate triage and escalation
        /// 
        /// These findings should block further operations until resolved.
        /// </summary>
        Critical = 3
    }
}
