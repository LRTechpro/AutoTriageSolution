using System.Linq;

namespace AutoTriage.Models
{
    /// <summary>
    /// Represents a programming session detected in the logs.
    /// Programming sessions involve flashing/updating ECU firmware or calibration.
    /// </summary>
    public class ProgrammingSession : Session
    {
        /// <summary>
        /// Returns the session type identifier.
        /// </summary>
        public override string SessionType => "Programming";

        /// <summary>
        /// Minimum voltage detected during this programming session.
        /// Low voltage can cause programming failures.
        /// </summary>
        public double? MinVoltage { get; set; }

        /// <summary>
        /// Maximum voltage detected during this programming session.
        /// </summary>
        public double? MaxVoltage { get; set; }

        /// <summary>
        /// Number of times voltage was checked during programming.
        /// </summary>
        public int TotalVoltageChecks { get; set; }

        /// <summary>
        /// Number of times voltage dropped below acceptable threshold (typically 12V).
        /// </summary>
        public int LowVoltageCount { get; set; }

        /// <summary>
        /// Number of Negative Response Codes (NRCs) encountered during programming.
        /// NRCs indicate communication or programming errors.
        /// </summary>
        public int NrcCount { get; set; }

        /// <summary>
        /// Module or ECU that was being programmed (e.g., "PCM", "TCM", "BCM").
        /// </summary>
        public string TargetModule { get; set; } = string.Empty;

        /// <summary>
        /// Programming result/outcome (e.g., "Success", "Failed", "Aborted").
        /// </summary>
        public string Result { get; set; } = string.Empty;

        /// <summary>
        /// Constructor initializes programming-specific fields.
        /// </summary>
        public ProgrammingSession()
        {
            // Initialize string fields to empty instead of null
            TargetModule = string.Empty;
            Result = string.Empty;
        }

        /// <summary>
        /// Computes all metrics specific to programming sessions.
        /// This includes voltage analysis and NRC counting.
        /// </summary>
        public override void ComputeMetrics()
        {
            // First compute base metrics (errors, warnings, timestamps)
            base.ComputeMetrics();

            // Find all lines that contain voltage values
            var voltageLines = Lines.Where(l => l.VoltageValue.HasValue).ToList();
            
            if (voltageLines.Any())
            {
                // Calculate min/max voltage from all voltage readings
                MinVoltage = voltageLines.Min(l => l.VoltageValue!.Value);
                MaxVoltage = voltageLines.Max(l => l.VoltageValue!.Value);
                
                // Count total voltage checks
                TotalVoltageChecks = voltageLines.Count;
                
                // Count low voltage occurrences (below 12V threshold)
                LowVoltageCount = voltageLines.Count(l => l.VoltageValue!.Value < 12.0);
            }
            else
            {
                // No voltage data found
                MinVoltage = null;
                MaxVoltage = null;
                TotalVoltageChecks = 0;
                LowVoltageCount = 0;
            }

            // Count NRC occurrences by searching for NRC patterns in line text
            NrcCount = Lines.Count(l => 
                l.RawText.Contains("NRC", System.StringComparison.OrdinalIgnoreCase) ||
                l.RawText.Contains("Negative Response", System.StringComparison.OrdinalIgnoreCase));

            // Determine success based on metrics
            // Programming is successful if:
            // 1. No critical errors
            // 2. No NRCs
            // 3. No low voltage events
            IsSuccessful = ErrorCount == 0 && NrcCount == 0 && LowVoltageCount == 0;
        }

        /// <summary>
        /// Gets a detailed summary specific to programming sessions.
        /// </summary>
        public override string GetSummary()
        {
            // Start with base session summary
            var summary = base.GetSummary();
            
            // Add programming-specific details
            summary += "\n\n--- Programming Details ---\n";
            
            if (!string.IsNullOrEmpty(TargetModule))
            {
                summary += $"Target Module: {TargetModule}\n";
            }
            
            // Add voltage information if available
            if (MinVoltage.HasValue && MaxVoltage.HasValue)
            {
                summary += $"Voltage Range: {MinVoltage:F2}V - {MaxVoltage:F2}V\n";
                summary += $"Voltage Checks: {TotalVoltageChecks}\n";
                
                if (LowVoltageCount > 0)
                {
                    summary += $"⚠ Low Voltage Events: {LowVoltageCount}\n";
                }
            }
            
            // Add NRC information
            if (NrcCount > 0)
            {
                summary += $"⚠ Negative Response Codes: {NrcCount}\n";
            }
            
            if (!string.IsNullOrEmpty(Result))
            {
                summary += $"Result: {Result}";
            }
            
            return summary;
        }
    }
}
