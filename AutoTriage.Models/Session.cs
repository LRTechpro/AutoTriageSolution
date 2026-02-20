using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoTriage.Models
{
    /// <summary>
    /// Abstract base class for all session types (programming, voltage check, etc.).
    /// A session represents a logical grouping of log lines that belong to a single operation.
    /// </summary>
    public abstract class Session
    {
        /// <summary>
        /// Unique identifier for this session.
        /// </summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// Type of session (e.g., "Programming", "VoltageCheck", "DiagnosticScan").
        /// </summary>
        public abstract string SessionType { get; }

        /// <summary>
        /// Line number where this session started.
        /// </summary>
        public int StartLineNumber { get; set; }

        /// <summary>
        /// Line number where this session ended (inclusive).
        /// </summary>
        public int EndLineNumber { get; set; }

        /// <summary>
        /// Timestamp when this session started (if available).
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// Timestamp when this session ended (if available).
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Collection of all log lines that are part of this session.
        /// </summary>
        public List<LogLine> Lines { get; set; }

        /// <summary>
        /// Number of error-level findings within this session.
        /// </summary>
        public int ErrorCount { get; set; }

        /// <summary>
        /// Number of warning-level findings within this session.
        /// </summary>
        public int WarningCount { get; set; }

        /// <summary>
        /// Indicates whether this session completed successfully.
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Optional notes or summary about this session.
        /// </summary>
        public string Notes { get; set; } = string.Empty;

        /// <summary>
        /// Constructor initializes collections and generates a unique session ID.
        /// </summary>
        protected Session()
        {
            // Generate a unique session ID using GUID
            SessionId = Guid.NewGuid().ToString("N").Substring(0, 8);
            // Initialize line collection
            Lines = new List<LogLine>();
            // Default to empty notes
            Notes = string.Empty;
        }

        /// <summary>
        /// Calculates the duration of this session if start and end times are available.
        /// </summary>
        public TimeSpan? Duration
        {
            get
            {
                // Only calculate if both start and end times exist
                if (StartTime.HasValue && EndTime.HasValue)
                {
                    return EndTime.Value - StartTime.Value;
                }
                return null;
            }
        }

        /// <summary>
        /// Gets a human-readable summary of this session.
        /// Can be overridden by derived classes for custom summaries.
        /// </summary>
        public virtual string GetSummary()
        {
            // Build a summary string with key metrics
            var summary = $"{SessionType} Session {SessionId}\n";
            summary += $"Lines: {StartLineNumber}-{EndLineNumber} ({Lines.Count} total)\n";
            
            // Add timing information if available
            if (Duration.HasValue)
            {
                summary += $"Duration: {Duration.Value.TotalSeconds:F1} seconds\n";
            }
            
            // Add error/warning counts
            summary += $"Errors: {ErrorCount}, Warnings: {WarningCount}\n";
            summary += $"Status: {(IsSuccessful ? "Success" : "Failed/Incomplete")}";
            
            return summary;
        }

        /// <summary>
        /// Computes metrics for this session based on its lines.
        /// Should be called after all lines have been added to the session.
        /// </summary>
        public virtual void ComputeMetrics()
        {
            // Count errors and warnings from the lines in this session
            ErrorCount = Lines.Count(line => 
                line.DetectedSeverity == FindingSeverity.Error || 
                line.DetectedSeverity == FindingSeverity.Critical);
            
            WarningCount = Lines.Count(line => 
                line.DetectedSeverity == FindingSeverity.Warning);
            
            // Extract timestamps from first and last lines if available
            var linesWithTimestamps = Lines.Where(l => l.Timestamp.HasValue).ToList();
            if (linesWithTimestamps.Any())
            {
                StartTime = linesWithTimestamps.First().Timestamp;
                EndTime = linesWithTimestamps.Last().Timestamp;
            }
        }
    }
}
