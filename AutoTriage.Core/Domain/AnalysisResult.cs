using System.Collections.Generic;

namespace AutoTriage.Core
{
    /// <summary>
    /// Represents the complete result of a log analysis operation.
    /// This class aggregates summary statistics and detailed findings
    /// produced by the LogAnalyzer.
    /// </summary>
    public class AnalysisResult
    {
        /// <summary>
        /// Total number of lines processed from the input log.
        /// </summary>
        public int TotalLines { get; set; }

        /// <summary>
        /// Number of critical-level findings detected.
        /// </summary>
        public int CriticalCount { get; set; }

        /// <summary>
        /// Number of error-level findings detected.
        /// </summary>
        public int ErrorCount { get; set; }

        /// <summary>
        /// Number of warning-level findings detected.
        /// </summary>
        public int WarningCount { get; set; }

        /// <summary>
        /// Number of success-level findings detected.
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// Overall health score calculated from analysis results.
        /// </summary>
        public int Score { get; set; }

        /// <summary>
        /// Collection of detailed findings discovered during analysis.
        /// </summary>
        public List<Finding> Findings { get; set; } = new List<Finding>();
        
        // NEW: Track ALL lines for keyword searching
        public List<LogLine> AllLines { get; set; } = new List<LogLine>();
    }

    /// <summary>
    /// Represents a single line from the log, regardless of whether it became a finding.
    /// </summary>
    public class LogLine
    {
        /// <summary>
        /// The line number in the original log file.
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// The raw text content of the log line.
        /// </summary>
        public string RawText { get; set; }

        /// <summary>
        /// The severity level detected for this log line, if any.
        /// </summary>
        public FindingSeverity DetectedSeverity { get; set; }

        /// <summary>
        /// Indicates if this log line is associated with a finding.
        /// </summary>
        public bool IsFinding { get; set; }
    }
}

