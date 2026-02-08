using System;

namespace AutoTriage.Core
{
    /// <summary>
    /// Provides functionality for analyzing raw log text and converting it
    /// into structured analysis results that can be consumed by a GUI.
    /// </summary>
    public class LogAnalyzer
    {
        /// <summary>
        /// Analyzes raw log text line-by-line and extracts structured findings.
        /// </summary>
        /// <param name="logText">
        /// The raw log text to analyze. Each line is evaluated independently.
        /// </param>
        /// <returns>
        /// An AnalysisResult object containing summary statistics and findings.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the provided log text is null or empty.
        /// </exception>
        public AnalysisResult Analyze(string logText)
        {
            // Guard clause to ensure valid input is provided to the analyzer.
            if (string.IsNullOrWhiteSpace(logText))
                throw new ArgumentException("Log text cannot be empty.");

            // Initialize the result object that will be populated during analysis.
            var result = new AnalysisResult();

            // Split the log into individual lines for processing.
            string[] lines = logText.Split(
                new[] { "\r\n", "\n" },
                StringSplitOptions.None
            );

            result.TotalLines = lines.Length;

            // Process each log line sequentially.
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string upper = line.ToUpperInvariant();

                // Determine severity based on keyword matching.
                if (upper.Contains("ERROR") || upper.Contains("FAIL"))
                {
                    result.ErrorCount++;

                    result.Findings.Add(new Finding
                    {
                        LineNumber = i + 1,
                        Severity = FindingSeverity.Error,
                        Code = "E-LOG",
                        Message = line
                    });
                }
                else if (upper.Contains("WARN"))
                {
                    result.WarningCount++;

                    result.Findings.Add(new Finding
                    {
                        LineNumber = i + 1,
                        Severity = FindingSeverity.Warning,
                        Code = "W-LOG",
                        Message = line
                    });
                }
                else if (upper.Contains("SUCCESS") || upper.Contains("COMPLETE"))
                {
                    result.SuccessCount++;

                    result.Findings.Add(new Finding
                    {
                        LineNumber = i + 1,
                        Severity = FindingSeverity.Success,
                        Code = "S-LOG",
                        Message = line
                    });
                }
            }

            // Calculate a simple health score based on detected issues.
            result.Score = Math.Max(
                0,
                100 - (result.ErrorCount * 15) - (result.WarningCount * 5)
            );

            return result;
        }
    }
}
