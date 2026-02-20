using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using AutoTriage.Models;

namespace AutoTriage.Analysis
{
    /// <summary>
    /// Parses raw log files and converts them into structured LogFile/LogLine objects.
    /// Handles encoding detection, line normalization, and timestamp extraction.
    /// </summary>
    public class LogParser
    {
        /// <summary>
        /// Regex patterns for detecting timestamps in various common formats.
        /// </summary>
        private static readonly Regex[] TimestampPatterns = new[]
        {
            // ISO 8601: 2024-01-15T14:30:45.123Z or 2024-01-15 14:30:45.123
            new Regex(@"^\d{4}-\d{2}-\d{2}[T ]\d{2}:\d{2}:\d{2}(?:\.\d{1,6})?(?:Z|[+-]\d{2}:?\d{2})?"),
            
            // Common log format: 01/15/2024 14:30:45 or 01-15-2024 14:30:45
            new Regex(@"^\d{2}[/-]\d{2}[/-]\d{4}\s+\d{2}:\d{2}:\d{2}(?:\.\d{1,6})?"),
            
            // Time only: 14:30:45.123 or 14:30:45
            new Regex(@"^\d{2}:\d{2}:\d{2}(?:\.\d{1,6})?"),
            
            // Unix timestamp: [1234567890] or [1234567890.123]
            new Regex(@"^\[\d{10,13}(?:\.\d{1,6})?\]"),
            
            // Brackets with date: [2024-01-15 14:30:45]
            new Regex(@"^\[\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2}(?:\.\d{1,6})?\]"),
            
            // Month/Day format: Jan 15 14:30:45 or 01/15 14:30:45
            new Regex(@"^(?:[A-Z][a-z]{2}\s+\d{1,2}|\d{2}/\d{2})\s+\d{2}:\d{2}:\d{2}(?:\.\d{1,6})?"),
        };

        /// <summary>
        /// Regex pattern for detecting voltage values in log lines.
        /// Matches patterns like "12.5V", "Voltage: 13.2", "V=12.8", etc.
        /// </summary>
        private static readonly Regex VoltagePattern = new Regex(
            @"(?:voltage|volt|V)\s*[:=]?\s*(\d+\.?\d*)\s*V?",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        /// <summary>
        /// Parses a single log file from disk and returns a structured LogFile object.
        /// </summary>
        /// <param name="filePath">Full path to the log file</param>
        /// <returns>Parsed LogFile with all lines</returns>
        public LogFile ParseFile(string filePath)
        {
            // Validate that file exists
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Log file not found: {filePath}");
            }

            // Create the LogFile object with metadata
            var logFile = new LogFile(filePath);

            // Read file content with BOM-aware encoding detection
            string content;
            using (var reader = new StreamReader(filePath, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
            {
                content = reader.ReadToEnd();
                // Store the detected encoding name for metadata
                logFile.EncodingName = reader.CurrentEncoding.EncodingName;
            }

            // Normalize line endings (convert \r\n and \r to \n)
            content = content.Replace("\r\n", "\n").Replace("\r", "\n");

            // Remove non-printable characters that can break parsing (except tabs and newlines)
            content = new string(content.Where(c => 
                c == '\n' || 
                c == '\t' || 
                (c >= 32 && c < 127) || 
                c >= 128
            ).ToArray());

            // Split into individual lines
            string[] rawLines = content.Split(new[] { '\n' }, StringSplitOptions.None);

            // Parse each line and create LogLine objects
            for (int i = 0; i < rawLines.Length; i++)
            {
                // Create LogLine object for this line
                var logLine = new LogLine
                {
                    // Line numbers are 1-based (human-readable)
                    LineNumber = i + 1,
                    // Store the raw text
                    RawText = rawLines[i],
                    // Reference back to parent file
                    SourceFile = logFile
                };

                // Try to extract timestamp from this line
                logLine.Timestamp = ExtractTimestamp(rawLines[i]);

                // Try to extract voltage value from this line
                logLine.VoltageValue = ExtractVoltage(rawLines[i]);

                // Add the parsed line to the file's collection
                logFile.Lines.Add(logLine);
            }

            return logFile;
        }

        /// <summary>
        /// Parses multiple log files and returns a list of LogFile objects.
        /// </summary>
        /// <param name="filePaths">Array of file paths to parse</param>
        /// <returns>List of parsed LogFile objects</returns>
        public List<LogFile> ParseFiles(string[] filePaths)
        {
            // Create result list
            var logFiles = new List<LogFile>();

            // Parse each file
            foreach (var filePath in filePaths)
            {
                try
                {
                    // Parse the file and add to collection
                    var logFile = ParseFile(filePath);
                    logFiles.Add(logFile);
                }
                catch (Exception ex)
                {
                    // Log the error but continue with other files
                    Console.WriteLine($"Error parsing file {filePath}: {ex.Message}");
                    // You could also throw or collect errors for reporting
                }
            }

            return logFiles;
        }

        /// <summary>
        /// Extracts timestamp from a log line using various patterns.
        /// </summary>
        /// <param name="line">Raw log line text</param>
        /// <returns>Parsed DateTime or null if no timestamp found</returns>
        private DateTime? ExtractTimestamp(string line)
        {
            // Skip empty lines
            if (string.IsNullOrWhiteSpace(line))
                return null;

            // Try each timestamp pattern
            foreach (var pattern in TimestampPatterns)
            {
                var match = pattern.Match(line);
                if (match.Success)
                {
                    // Extract the matched timestamp string
                    var timestampStr = match.Value.Trim('[', ']', ' ');

                    // Try to parse it as a DateTime
                    if (DateTime.TryParse(timestampStr, out DateTime timestamp))
                    {
                        return timestamp;
                    }
                }
            }

            // No timestamp found
            return null;
        }

        /// <summary>
        /// Extracts voltage value from a log line if present.
        /// </summary>
        /// <param name="line">Raw log line text</param>
        /// <returns>Voltage value in volts, or null if not found</returns>
        private double? ExtractVoltage(string line)
        {
            // Skip empty lines
            if (string.IsNullOrWhiteSpace(line))
                return null;

            // Try to match voltage pattern
            var match = VoltagePattern.Match(line);
            if (match.Success && match.Groups.Count > 1)
            {
                // Extract the numeric voltage value
                var voltageStr = match.Groups[1].Value;
                
                // Parse as double
                if (double.TryParse(voltageStr, out double voltage))
                {
                    return voltage;
                }
            }

            // No voltage found
            return null;
        }
    }
}
