using System;
using System.Collections.Generic;
using System.Linq;
using AutoTriage.Models;

namespace AutoTriage.Analysis
{
    /// <summary>
    /// Provides filtering capabilities for log lines based on keywords, severity, and other criteria.
    /// Tracks match counts for reporting.
    /// </summary>
    public class LogFilter
    {
        /// <summary>
        /// Stores the count of matches for each keyword after filtering.
        /// Key = keyword (case-insensitive), Value = number of matches
        /// </summary>
        public Dictionary<string, int> KeywordMatchCounts { get; private set; }

        /// <summary>
        /// Constructor initializes the match count dictionary.
        /// </summary>
        public LogFilter()
        {
            KeywordMatchCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Filters log lines by a set of keywords and returns matching lines.
        /// Updates KeywordMatchCounts with per-keyword statistics.
        /// </summary>
        /// <param name="lines">All log lines to filter</param>
        /// <param name="keywords">Array of keywords to search for</param>
        /// <returns>List of lines that match any keyword</returns>
        public List<LogLine> FilterByKeywords(IEnumerable<LogLine> lines, string[] keywords)
        {
            // Reset match counts
            KeywordMatchCounts.Clear();

            // Initialize count for each keyword
            foreach (var keyword in keywords)
            {
                KeywordMatchCounts[keyword] = 0;
            }

            // Result list
            var matchingLines = new List<LogLine>();

            // Check each line
            foreach (var line in lines)
            {
                var rawText = line.RawText ?? string.Empty;
                bool lineMatches = false;

                // Check against each keyword
                foreach (var keyword in keywords)
                {
                    // Case-insensitive search
                    if (rawText.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        // Increment count for this keyword
                        KeywordMatchCounts[keyword]++;
                        lineMatches = true;
                    }
                }

                // Add line to results if it matched any keyword
                if (lineMatches)
                {
                    matchingLines.Add(line);
                }
            }

            return matchingLines;
        }

        /// <summary>
        /// Filters log lines by severity level.
        /// </summary>
        /// <param name="lines">All log lines to filter</param>
        /// <param name="severities">Array of severity levels to include</param>
        /// <returns>List of lines matching the specified severities</returns>
        public List<LogLine> FilterBySeverity(IEnumerable<LogLine> lines, FindingSeverity[] severities)
        {
            // Create hashset for fast lookup
            var severitySet = new HashSet<FindingSeverity>(severities);

            // Filter lines where severity is in the set
            return lines.Where(line => severitySet.Contains(line.DetectedSeverity)).ToList();
        }

        /// <summary>
        /// Filters log lines that contain voltage data.
        /// </summary>
        /// <param name="lines">All log lines to filter</param>
        /// <returns>List of lines that have voltage values</returns>
        public List<LogLine> FilterByVoltage(IEnumerable<LogLine> lines)
        {
            // Return only lines with voltage data
            return lines.Where(line => line.VoltageValue.HasValue).ToList();
        }

        /// <summary>
        /// Filters log lines within a specific line number range.
        /// </summary>
        /// <param name="lines">All log lines to filter</param>
        /// <param name="startLine">Starting line number (inclusive)</param>
        /// <param name="endLine">Ending line number (inclusive)</param>
        /// <returns>List of lines within the range</returns>
        public List<LogLine> FilterByLineRange(IEnumerable<LogLine> lines, int startLine, int endLine)
        {
            // Return lines within the specified range
            return lines.Where(line => 
                line.LineNumber >= startLine && 
                line.LineNumber <= endLine
            ).ToList();
        }

        /// <summary>
        /// Filters log lines by timestamp range.
        /// </summary>
        /// <param name="lines">All log lines to filter</param>
        /// <param name="startTime">Starting timestamp (inclusive)</param>
        /// <param name="endTime">Ending timestamp (inclusive)</param>
        /// <returns>List of lines within the time range</returns>
        public List<LogLine> FilterByTimeRange(IEnumerable<LogLine> lines, DateTime startTime, DateTime endTime)
        {
            // Return lines with timestamps in the range
            return lines.Where(line =>
                line.Timestamp.HasValue &&
                line.Timestamp.Value >= startTime &&
                line.Timestamp.Value <= endTime
            ).ToList();
        }

        /// <summary>
        /// Combines multiple filter criteria with AND logic.
        /// </summary>
        /// <param name="lines">All log lines to filter</param>
        /// <param name="keywords">Keywords filter (empty = skip this filter)</param>
        /// <param name="severities">Severity filter (empty = skip this filter)</param>
        /// <param name="requireVoltage">If true, only include lines with voltage data</param>
        /// <returns>List of lines matching all specified criteria</returns>
        public List<LogLine> FilterCombined(
            IEnumerable<LogLine> lines,
            string[] keywords = null,
            FindingSeverity[] severities = null,
            bool requireVoltage = false)
        {
            // Start with all lines
            var result = lines.ToList();

            // Apply keyword filter if specified
            if (keywords != null && keywords.Length > 0)
            {
                result = FilterByKeywords(result, keywords);
            }

            // Apply severity filter if specified
            if (severities != null && severities.Length > 0)
            {
                result = FilterBySeverity(result, severities);
            }

            // Apply voltage filter if required
            if (requireVoltage)
            {
                result = FilterByVoltage(result);
            }

            return result;
        }

        /// <summary>
        /// Gets a formatted summary of keyword match statistics.
        /// </summary>
        /// <returns>Human-readable string with keyword counts</returns>
        public string GetKeywordMatchSummary()
        {
            if (KeywordMatchCounts.Count == 0)
            {
                return "No keyword filters active";
            }

            // Build summary string
            var summary = "Keyword Matches:\n";
            var totalMatches = 0;

            foreach (var kvp in KeywordMatchCounts.OrderByDescending(x => x.Value))
            {
                summary += $"  '{kvp.Key}': {kvp.Value} matches\n";
                totalMatches += kvp.Value;
            }

            summary += $"Total: {totalMatches} keyword matches";

            return summary;
        }
    }
}
