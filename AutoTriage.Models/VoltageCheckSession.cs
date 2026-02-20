using System;
using System.Linq;

namespace AutoTriage.Models
{
    /// <summary>
    /// Represents a voltage monitoring/check session or event.
    /// This tracks periods where voltage is being actively monitored.
    /// </summary>
    public class VoltageCheckSession : Session
    {
        /// <summary>
        /// Returns the session type identifier.
        /// </summary>
        public override string SessionType => "VoltageCheck";

        /// <summary>
        /// Minimum voltage detected during this monitoring period.
        /// </summary>
        public double? MinVoltage { get; set; }

        /// <summary>
        /// Maximum voltage detected during this monitoring period.
        /// </summary>
        public double? MaxVoltage { get; set; }

        /// <summary>
        /// Average voltage during this monitoring period.
        /// </summary>
        public double? AverageVoltage { get; set; }

        /// <summary>
        /// Number of voltage samples taken.
        /// </summary>
        public int SampleCount { get; set; }

        /// <summary>
        /// Number of times voltage dropped below acceptable threshold.
        /// </summary>
        public int LowVoltageEventCount { get; set; }

        /// <summary>
        /// Threshold voltage value used to determine "low voltage" condition.
        /// Typically 12.0V for automotive systems.
        /// </summary>
        public double LowVoltageThreshold { get; set; }

        /// <summary>
        /// Constructor initializes voltage check fields with defaults.
        /// </summary>
        public VoltageCheckSession()
        {
            // Set standard automotive low voltage threshold
            LowVoltageThreshold = 12.0;
        }

        /// <summary>
        /// Computes voltage metrics from all lines in this session.
        /// </summary>
        public override void ComputeMetrics()
        {
            // Compute base session metrics first
            base.ComputeMetrics();

            // Get all lines with voltage values
            var voltageLines = Lines.Where(l => l.VoltageValue.HasValue).ToList();

            if (voltageLines.Any())
            {
                // Extract all voltage values
                var voltageValues = voltageLines.Select(l => l.VoltageValue!.Value).ToList();

                // Calculate statistics
                MinVoltage = voltageValues.Min();
                MaxVoltage = voltageValues.Max();
                AverageVoltage = voltageValues.Average();
                SampleCount = voltageValues.Count;

                // Count low voltage events (below threshold)
                LowVoltageEventCount = voltageValues.Count(v => v < LowVoltageThreshold);
            }
            else
            {
                // No voltage data
                MinVoltage = null;
                MaxVoltage = null;
                AverageVoltage = null;
                SampleCount = 0;
                LowVoltageEventCount = 0;
            }

            // Session is successful if no low voltage events occurred
            IsSuccessful = LowVoltageEventCount == 0;
        }

        /// <summary>
        /// Gets a detailed summary of this voltage check session.
        /// </summary>
        public override string GetSummary()
        {
            // Start with base summary
            var summary = base.GetSummary();

            // Add voltage-specific details
            summary += "\n\n--- Voltage Monitoring Details ---\n";

            if (MinVoltage.HasValue && MaxVoltage.HasValue && AverageVoltage.HasValue)
            {
                summary += $"Voltage Statistics:\n";
                summary += $"  Min: {MinVoltage:F2}V\n";
                summary += $"  Max: {MaxVoltage:F2}V\n";
                summary += $"  Avg: {AverageVoltage:F2}V\n";
                summary += $"  Samples: {SampleCount}\n";
                summary += $"  Threshold: {LowVoltageThreshold:F1}V\n";

                if (LowVoltageEventCount > 0)
                {
                    summary += $"⚠ Low Voltage Events: {LowVoltageEventCount}";
                }
                else
                {
                    summary += "✓ Voltage remained stable above threshold";
                }
            }
            else
            {
                summary += "No voltage data available";
            }

            return summary;
        }
    }
}
