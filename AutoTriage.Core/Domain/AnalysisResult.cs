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
        public List<Finding> Findings { get; } = new List<Finding>();
    }
}

