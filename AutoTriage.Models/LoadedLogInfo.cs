using System;

namespace AutoTriage.Models
{
    /// <summary>
    /// Represents information about a loaded log source.
    /// </summary>
    public class LoadedLogInfo
    {
        /// <summary>
        /// Unique identifier for this log source.
        /// </summary>
        public Guid LogId { get; set; }

        /// <summary>
        /// Display name (filename or "Pasted (n)").
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Number of lines in this log.
        /// </summary>
        public int LineCount { get; set; }

        /// <summary>
        /// Starting global line number for this log.
        /// </summary>
        public int StartGlobalLineNumber { get; set; }

        /// <summary>
        /// Ending global line number for this log.
        /// </summary>
        public int EndGlobalLineNumber { get; set; }

        /// <summary>
        /// When this log was loaded.
        /// </summary>
        public DateTime LoadedAt { get; set; }

        /// <summary>
        /// Returns a string representation of the log information.
        /// </summary>
        /// <returns>A formatted string containing the log name and line count.</returns>
        public override string ToString()
        {
            return $"{Name} ({LineCount} lines)";
        }
    }
}
