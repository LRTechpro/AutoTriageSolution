using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AutoTriage.Models;

namespace AutoTriage.Analysis
{
    /// <summary>
    /// Detects programming sessions within log files using pattern matching.
    /// Looks for session start/end markers and groups lines accordingly.
    /// </summary>
    public class SessionDetector
    {
        /// <summary>
        /// Keywords that indicate the start of a programming session.
        /// </summary>
        private static readonly string[] ProgrammingStartKeywords = new[]
        {
            "programming started",
            "begin programming",
            "flash start",
            "software download started",
            "update firmware",
            "programming sequence initiated"
        };

        /// <summary>
        /// Keywords that indicate the end of a programming session.
        /// </summary>
        private static readonly string[] ProgrammingEndKeywords = new[]
        {
            "programming complete",
            "programming success",
            "programming failed",
            "flash complete",
            "software download complete",
            "update complete",
            "programming aborted"
        };

        /// <summary>
        /// Keywords that indicate voltage monitoring activity.
        /// </summary>
        private static readonly string[] VoltageCheckKeywords = new[]
        {
            "voltage check",
            "battery voltage",
            "monitoring voltage",
            "voltage test",
            "checking battery"
        };

        /// <summary>
        /// Detects all programming sessions in a log file.
        /// </summary>
        /// <param name="logFile">The log file to analyze</param>
        /// <returns>List of detected programming sessions</returns>
        public List<ProgrammingSession> DetectProgrammingSessions(LogFile logFile)
        {
            // Create result list
            var sessions = new List<ProgrammingSession>();

            // Track current session being built
            ProgrammingSession currentSession = null;

            // Iterate through all lines in the log file
            for (int i = 0; i < logFile.Lines.Count; i++)
            {
                var line = logFile.Lines[i];
                var rawText = line.RawText ?? string.Empty;

                // Check if this line starts a new programming session
                bool isStartMarker = ProgrammingStartKeywords.Any(keyword =>
                    rawText.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);

                if (isStartMarker)
                {
                    // If there was a previous unclosed session, close it first
                    if (currentSession != null)
                    {
                        // Close the previous session at the line before this one
                        currentSession.EndLineNumber = i;
                        // Compute metrics for the completed session
                        currentSession.ComputeMetrics();
                        // Add to results
                        sessions.Add(currentSession);
                    }

                    // Start a new programming session
                    currentSession = new ProgrammingSession
                    {
                        StartLineNumber = line.LineNumber,
                        StartTime = line.Timestamp
                    };

                    // Try to extract target module from the line
                    currentSession.TargetModule = ExtractModuleName(rawText);
                }

                // Check if this line ends the current programming session
                bool isEndMarker = ProgrammingEndKeywords.Any(keyword =>
                    rawText.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);

                if (isEndMarker && currentSession != null)
                {
                    // Close the current session
                    currentSession.EndLineNumber = line.LineNumber;
                    currentSession.EndTime = line.Timestamp;

                    // Extract result status from end marker
                    currentSession.Result = ExtractResult(rawText);

                    // Add the line to the session
                    currentSession.Lines.Add(line);

                    // Compute all metrics for this session
                    currentSession.ComputeMetrics();

                    // Add completed session to results
                    sessions.Add(currentSession);

                    // Clear current session tracker
                    currentSession = null;
                }
                else if (currentSession != null)
                {
                    // This line belongs to the current active session
                    currentSession.Lines.Add(line);
                }
            }

            // Handle case where session was never explicitly closed
            if (currentSession != null)
            {
                // Close at last line
                currentSession.EndLineNumber = logFile.Lines.Last().LineNumber;
                currentSession.EndTime = logFile.Lines.LastOrDefault(l => l.Timestamp.HasValue)?.Timestamp;
                
                // Compute metrics
                currentSession.ComputeMetrics();
                
                // Add to results
                sessions.Add(currentSession);
            }

            return sessions;
        }

        /// <summary>
        /// Detects voltage check sessions in a log file.
        /// </summary>
        /// <param name="logFile">The log file to analyze</param>
        /// <returns>List of detected voltage check sessions</returns>
        public List<VoltageCheckSession> DetectVoltageCheckSessions(LogFile logFile)
        {
            // Create result list
            var sessions = new List<VoltageCheckSession>();

            // Track current voltage check session
            VoltageCheckSession currentSession = null;

            // Minimum number of consecutive voltage readings to form a session
            const int MinVoltageReadings = 3;

            // Iterate through all lines
            for (int i = 0; i < logFile.Lines.Count; i++)
            {
                var line = logFile.Lines[i];
                var rawText = line.RawText ?? string.Empty;

                // Check if this line contains voltage check keywords or has voltage data
                bool isVoltageRelated = VoltageCheckKeywords.Any(keyword =>
                    rawText.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    line.VoltageValue.HasValue;

                if (isVoltageRelated)
                {
                    if (currentSession == null)
                    {
                        // Start a new voltage check session
                        currentSession = new VoltageCheckSession
                        {
                            StartLineNumber = line.LineNumber,
                            StartTime = line.Timestamp
                        };
                    }

                    // Add this line to the current session
                    currentSession.Lines.Add(line);
                }
                else if (currentSession != null)
                {
                    // Non-voltage line encountered, check if we should close the session
                    // Only create a session if we have enough voltage readings
                    if (currentSession.Lines.Count(l => l.VoltageValue.HasValue) >= MinVoltageReadings)
                    {
                        // Close and save the session
                        currentSession.EndLineNumber = currentSession.Lines.Last().LineNumber;
                        currentSession.EndTime = currentSession.Lines.LastOrDefault(l => l.Timestamp.HasValue)?.Timestamp;
                        
                        // Compute metrics
                        currentSession.ComputeMetrics();
                        
                        // Add to results
                        sessions.Add(currentSession);
                    }

                    // Reset current session
                    currentSession = null;
                }
            }

            // Handle unclosed session at end of file
            if (currentSession != null &&
                currentSession.Lines.Count(l => l.VoltageValue.HasValue) >= MinVoltageReadings)
            {
                currentSession.EndLineNumber = currentSession.Lines.Last().LineNumber;
                currentSession.EndTime = currentSession.Lines.LastOrDefault(l => l.Timestamp.HasValue)?.Timestamp;
                currentSession.ComputeMetrics();
                sessions.Add(currentSession);
            }

            return sessions;
        }

        /// <summary>
        /// Detects all types of sessions in a vehicle case (all log files).
        /// </summary>
        /// <param name="vehicleCase">The vehicle case containing log files</param>
        /// <returns>List of all detected sessions</returns>
        public List<Session> DetectAllSessions(VehicleCase vehicleCase)
        {
            // Create combined list of all session types
            var allSessions = new List<Session>();

            // Process each log file
            foreach (var logFile in vehicleCase.LogFiles)
            {
                // Detect programming sessions
                var programmingSessions = DetectProgrammingSessions(logFile);
                allSessions.AddRange(programmingSessions);

                // Detect voltage check sessions
                var voltageSessions = DetectVoltageCheckSessions(logFile);
                allSessions.AddRange(voltageSessions);
            }

            // Sort all sessions by start line number
            return allSessions.OrderBy(s => s.StartLineNumber).ToList();
        }

        /// <summary>
        /// Extracts module/ECU name from a programming start line.
        /// </summary>
        private string ExtractModuleName(string line)
        {
            // Common module abbreviations
            var modules = new[] { "PCM", "TCM", "BCM", "ACM", "RCM", "IPC", "APIM", "ECU", "ECM" };

            // Check if any module name appears in the line
            foreach (var module in modules)
            {
                // Look for module name as a whole word
                var pattern = new Regex($@"\b{module}\b", RegexOptions.IgnoreCase);
                if (pattern.IsMatch(line))
                {
                    return module.ToUpper();
                }
            }

            return "Unknown";
        }

        /// <summary>
        /// Extracts result status from a programming end marker line.
        /// </summary>
        private string ExtractResult(string line)
        {
            // Check for success indicators
            if (line.IndexOf("success", StringComparison.OrdinalIgnoreCase) >= 0 ||
                line.IndexOf("complete", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Success";
            }

            // Check for failure indicators
            if (line.IndexOf("failed", StringComparison.OrdinalIgnoreCase) >= 0 ||
                line.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Failed";
            }

            // Check for abort indicators
            if (line.IndexOf("abort", StringComparison.OrdinalIgnoreCase) >= 0 ||
                line.IndexOf("cancel", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Aborted";
            }

            return "Unknown";
        }
    }
}
