using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AutoTriage.Models;

namespace AutoTriage.Analysis
{
    /// <summary>
    /// Compares multiple sessions and generates comparison reports.
    /// Useful for identifying differences between successful and failed programming attempts.
    /// </summary>
    public class SessionComparator
    {
        /// <summary>
        /// Compares two sessions and returns a detailed comparison report.
        /// </summary>
        /// <param name="session1">First session to compare</param>
        /// <param name="session2">Second session to compare</param>
        /// <returns>Formatted comparison report</returns>
        public string Compare(Session session1, Session session2)
        {
            // Build comparison report
            var report = new StringBuilder();

            report.AppendLine("=== SESSION COMPARISON ===\n");

            // Basic info comparison
            report.AppendLine($"Session 1: {session1.SessionType} ({session1.SessionId})");
            report.AppendLine($"Session 2: {session2.SessionType} ({session2.SessionId})\n");

            // Line range comparison
            report.AppendLine("--- Line Ranges ---");
            report.AppendLine($"Session 1: Lines {session1.StartLineNumber}-{session1.EndLineNumber} ({session1.Lines.Count} total)");
            report.AppendLine($"Session 2: Lines {session2.StartLineNumber}-{session2.EndLineNumber} ({session2.Lines.Count} total)");
            report.AppendLine();

            // Duration comparison
            if (session1.Duration.HasValue && session2.Duration.HasValue)
            {
                report.AppendLine("--- Duration ---");
                report.AppendLine($"Session 1: {session1.Duration.Value.TotalSeconds:F1} seconds");
                report.AppendLine($"Session 2: {session2.Duration.Value.TotalSeconds:F1} seconds");
                
                var durationDiff = session2.Duration.Value - session1.Duration.Value;
                report.AppendLine($"Difference: {durationDiff.TotalSeconds:F1} seconds");
                report.AppendLine();
            }

            // Error/Warning comparison
            report.AppendLine("--- Errors & Warnings ---");
            report.AppendLine($"Session 1: {session1.ErrorCount} errors, {session1.WarningCount} warnings");
            report.AppendLine($"Session 2: {session2.ErrorCount} errors, {session2.WarningCount} warnings");
            report.AppendLine($"Error Δ: {session2.ErrorCount - session1.ErrorCount:+#;-#;0}");
            report.AppendLine($"Warning Δ: {session2.WarningCount - session1.WarningCount:+#;-#;0}");
            report.AppendLine();

            // Status comparison
            report.AppendLine("--- Status ---");
            report.AppendLine($"Session 1: {(session1.IsSuccessful ? "✓ Success" : "✗ Failed/Incomplete")}");
            report.AppendLine($"Session 2: {(session2.IsSuccessful ? "✓ Success" : "✗ Failed/Incomplete")}");
            report.AppendLine();

            // Type-specific comparisons
            if (session1 is ProgrammingSession prog1 && session2 is ProgrammingSession prog2)
            {
                report.AppendLine(CompareProgrammingSessions(prog1, prog2));
            }
            else if (session1 is VoltageCheckSession volt1 && session2 is VoltageCheckSession volt2)
            {
                report.AppendLine(CompareVoltageSessions(volt1, volt2));
            }

            return report.ToString();
        }

        /// <summary>
        /// Compares multiple sessions (3 or more) and returns a summary table.
        /// </summary>
        /// <param name="sessions">List of sessions to compare</param>
        /// <returns>Formatted comparison table</returns>
        public string CompareMultiple(List<Session> sessions)
        {
            if (sessions == null || sessions.Count < 2)
            {
                return "Need at least 2 sessions to compare";
            }

            var report = new StringBuilder();

            report.AppendLine("=== MULTI-SESSION COMPARISON ===\n");

            // Create comparison table
            report.AppendLine($"{"Session ID",-12} {"Type",-15} {"Lines",-10} {"Duration",-12} {"Errors",-8} {"Warnings",-10} {"Status",-10}");
            report.AppendLine(new string('-', 87));

            foreach (var session in sessions)
            {
                var durationStr = session.Duration.HasValue 
                    ? $"{session.Duration.Value.TotalSeconds:F1}s" 
                    : "N/A";

                var statusStr = session.IsSuccessful ? "Success" : "Failed";

                report.AppendLine($"{session.SessionId,-12} {session.SessionType,-15} {session.Lines.Count,-10} {durationStr,-12} {session.ErrorCount,-8} {session.WarningCount,-10} {statusStr,-10}");
            }

            report.AppendLine();

            // Add aggregate statistics
            report.AppendLine("--- Aggregate Statistics ---");
            report.AppendLine($"Total Sessions: {sessions.Count}");
            report.AppendLine($"Successful: {sessions.Count(s => s.IsSuccessful)}");
            report.AppendLine($"Failed: {sessions.Count(s => !s.IsSuccessful)}");
            report.AppendLine($"Total Errors: {sessions.Sum(s => s.ErrorCount)}");
            report.AppendLine($"Total Warnings: {sessions.Sum(s => s.WarningCount)}");

            return report.ToString();
        }

        /// <summary>
        /// Compares two programming sessions with programming-specific metrics.
        /// </summary>
        private string CompareProgrammingSessions(ProgrammingSession prog1, ProgrammingSession prog2)
        {
            var report = new StringBuilder();

            report.AppendLine("--- Programming Details ---");

            // Module comparison
            report.AppendLine($"Target Module:");
            report.AppendLine($"  Session 1: {prog1.TargetModule}");
            report.AppendLine($"  Session 2: {prog2.TargetModule}");
            
            if (prog1.TargetModule != prog2.TargetModule)
            {
                report.AppendLine("  ⚠ Different modules programmed");
            }
            report.AppendLine();

            // Voltage comparison
            if (prog1.MinVoltage.HasValue && prog2.MinVoltage.HasValue)
            {
                report.AppendLine("Voltage Ranges:");
                report.AppendLine($"  Session 1: {prog1.MinVoltage:F2}V - {prog1.MaxVoltage:F2}V");
                report.AppendLine($"  Session 2: {prog2.MinVoltage:F2}V - {prog2.MaxVoltage:F2}V");
                report.AppendLine();

                report.AppendLine("Voltage Statistics:");
                report.AppendLine($"  Session 1: {prog1.TotalVoltageChecks} checks, {prog1.LowVoltageCount} low voltage events");
                report.AppendLine($"  Session 2: {prog2.TotalVoltageChecks} checks, {prog2.LowVoltageCount} low voltage events");
                
                var lowVoltageDiff = prog2.LowVoltageCount - prog1.LowVoltageCount;
                if (lowVoltageDiff > 0)
                {
                    report.AppendLine($"  ⚠ Session 2 had {lowVoltageDiff} more low voltage events");
                }
                report.AppendLine();
            }

            // NRC comparison
            report.AppendLine("Negative Response Codes:");
            report.AppendLine($"  Session 1: {prog1.NrcCount} NRCs");
            report.AppendLine($"  Session 2: {prog2.NrcCount} NRCs");
            
            var nrcDiff = prog2.NrcCount - prog1.NrcCount;
            if (nrcDiff != 0)
            {
                report.AppendLine($"  Δ: {nrcDiff:+#;-#;0} NRCs");
            }
            report.AppendLine();

            // Result comparison
            report.AppendLine("Programming Result:");
            report.AppendLine($"  Session 1: {prog1.Result}");
            report.AppendLine($"  Session 2: {prog2.Result}");

            return report.ToString();
        }

        /// <summary>
        /// Compares two voltage check sessions with voltage-specific metrics.
        /// </summary>
        private string CompareVoltageSessions(VoltageCheckSession volt1, VoltageCheckSession volt2)
        {
            var report = new StringBuilder();

            report.AppendLine("--- Voltage Check Details ---");

            if (volt1.MinVoltage.HasValue && volt2.MinVoltage.HasValue)
            {
                report.AppendLine("Voltage Statistics:");
                report.AppendLine($"  Session 1: Min={volt1.MinVoltage:F2}V, Max={volt1.MaxVoltage:F2}V, Avg={volt1.AverageVoltage:F2}V");
                report.AppendLine($"  Session 2: Min={volt2.MinVoltage:F2}V, Max={volt2.MaxVoltage:F2}V, Avg={volt2.AverageVoltage:F2}V");
                report.AppendLine();

                // Highlight significant differences
                var minDiff = volt2.MinVoltage.Value - volt1.MinVoltage.Value;
                var avgDiff = volt2.AverageVoltage.Value - volt1.AverageVoltage.Value;

                report.AppendLine("Differences:");
                report.AppendLine($"  Min Voltage Δ: {minDiff:+0.00;-0.00;0.00}V");
                report.AppendLine($"  Avg Voltage Δ: {avgDiff:+0.00;-0.00;0.00}V");
                report.AppendLine();

                report.AppendLine("Low Voltage Events:");
                report.AppendLine($"  Session 1: {volt1.LowVoltageEventCount} events");
                report.AppendLine($"  Session 2: {volt2.LowVoltageEventCount} events");
                
                var eventDiff = volt2.LowVoltageEventCount - volt1.LowVoltageEventCount;
                if (eventDiff > 0)
                {
                    report.AppendLine($"  ⚠ Session 2 had {eventDiff} more low voltage events");
                }
            }

            return report.ToString();
        }
    }
}
