using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoTriage.Models
{
    /// <summary>
    /// Represents a complete diagnostic case for a single vehicle.
    /// A case can contain multiple log files from different dates/sessions.
    /// This is the top-level container in the data model hierarchy.
    /// </summary>
    public class VehicleCase
    {
        /// <summary>
        /// Unique identifier for this vehicle case (e.g., VIN, case number).
        /// </summary>
        public string CaseId { get; set; } = string.Empty;

        /// <summary>
        /// Optional vehicle identification number for traceability.
        /// </summary>
        public string VIN { get; set; } = string.Empty;

        /// <summary>
        /// Optional customer or technician notes about the case.
        /// </summary>
        public string Notes { get; set; } = string.Empty;

        /// <summary>
        /// Collection of all log files associated with this vehicle case.
        /// Logs can span multiple days or diagnostic sessions.
        /// </summary>
        public List<LogFile> LogFiles { get; set; }

        /// <summary>
        /// Date when this case was created or first opened.
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Constructor initializes the LogFiles collection to prevent null reference errors.
        /// </summary>
        public VehicleCase()
        {
            // Initialize empty collection to avoid null checks
            LogFiles = new List<LogFile>();
            // Set default creation date to current time
            CreatedDate = DateTime.Now;
        }

        /// <summary>
        /// Gets the total number of lines across all log files in this case.
        /// </summary>
        public int TotalLineCount
        {
            get
            {
                // Sum up all lines from all log files
                return LogFiles.Sum(lf => lf.Lines.Count);
            }
        }

        /// <summary>
        /// Gets all log lines from all files in chronological order (by log date).
        /// </summary>
        public IEnumerable<LogLine> GetAllLines()
        {
            // Order log files by date first
            return LogFiles
                .OrderBy(lf => lf.LogDate)
                // Then flatten all lines from each file
                .SelectMany(lf => lf.Lines);
        }
    }
}
