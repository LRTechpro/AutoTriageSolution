using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace AutoTriage.Core
{
    public class LogAnalyzer
    {
        // ========================================================================
        // TEST DATA FOR LOW_VOLTAGE AND LOW_SOC RULES
        // ========================================================================
        /*
         * COPY THIS INTO A TEST FILE TO VERIFY THE NEW RULES:
         * 
         * Line 1: System voltage 12.5V                     -> NO FINDING (voltage OK)
         * Line 2: Battery Voltage 11.7V                    -> WARNING (below 11.8V threshold)
         * Line 3: VBAT 10.9V                               -> CRITICAL (below 11.0V threshold)
         * Line 4: SOC 82%                                  -> NO FINDING (above 70%)
         * Line 5: SOC 68%                                  -> WARNING (below 70% threshold)
         * Line 6: Battery charge level 49%                 -> CRITICAL (below 50% threshold)
         * Line 7: Download progress 65%                    -> NO FINDING (no battery/SoC keywords)
         * Line 8: Battery Voltage: 11.8V exactly           -> NO FINDING (boundary case: > 11.8V is OK)
         * Line 9: State of charge 50%                      -> NO FINDING (boundary case: >= 50% is OK)
         * Line 10: Ignition voltage low at 10.5 volts     -> CRITICAL
         * Line 11: Supply voltage = 11.5 V                -> WARNING
         */

        public AnalysisResult Analyze(IReadOnlyList<string> lines)
        {
            var result = new AnalysisResult();

            // 1) Load the Critical Findings rules
            var rules = CriticalRuleSet.Build();

            // 2) Apply rules to lines and emit findings
            ApplyCriticalRules(lines, rules, result);

            // 3) Apply NEW deterministic rules: LOW_VOLTAGE and LOW_SOC
            ApplyLowVoltageRule(lines, result);
            ApplyLowSocRule(lines, result);

            return result;
        }

        // ========================================================================
        // LOW_VOLTAGE RULE
        // ========================================================================
        /// <summary>
        /// Detects low battery/system voltage conditions that may cause ECU instability.
        /// 
        /// WHY THIS RULE EXISTS:
        /// - Automotive ECUs require stable voltage (typically 12V nominal, 11.8V+ recommended).
        /// - Low voltage can cause:
        ///   * Brownout resets
        ///   * Flash programming failures
        ///   * Memory corruption
        ///   * Communication loss
        /// - This is a deterministic, rule-based check (no AI/ML).
        /// 
        /// DETECTION LOGIC:
        /// 1. Keyword gate: line must contain one of:
        ///    "battery voltage", "system voltage", "vbatt", "vbat", "b+ voltage",
        ///    "ignition voltage", "low voltage", "undervoltage", "supply voltage"
        /// 2. Extract numeric voltage with regex: (\d+\.?\d*)\s*V (case-insensitive)
        /// 3. Parse as double using InvariantCulture
        /// 4. Classify:
        ///    - <= 11.0V => CRITICAL (may cause immediate ECU failure)
        ///    - <= 11.8V => WARNING (suboptimal, monitor)
        /// 5. Emit Finding with Code="LOW_VOLTAGE" and include original source line
        /// 
        /// SAFETY:
        /// - Handles null/empty lines gracefully
        /// - Only creates one finding per line (uses 'continue' after detection)
        /// - Voltage must be in range 0-20V to avoid false positives from non-voltage numbers
        /// </summary>
        private static void ApplyLowVoltageRule(IReadOnlyList<string> lines, AnalysisResult result)
        {
            // Defensive: null or empty collection
            if (lines == null || lines.Count == 0) return;

            // Keywords that indicate this line is talking about voltage
            // (prevents false positives from random numbers in logs)
            var voltageKeywords = new[]
            {
                "battery voltage",
                "system voltage",
                "vbatt",
                "vbat",
                "b+ voltage",
                "ignition voltage",
                "low voltage",
                "undervoltage",
                "supply voltage"
            };

            // Regex to extract voltage value: matches "11.7V", "10.9 V", "12.5 volts", etc.
            // Captures the numeric part before V/volt/volts
            var voltageRegex = new Regex(@"(\d+\.?\d*)\s*v(?:olts?)?", RegexOptions.IgnoreCase | RegexOptions.Compiled);

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];

                // Defensive: skip null or empty lines
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // STEP 1: Keyword gate - must mention voltage-related term
                bool hasVoltageKeyword = false;
                foreach (var keyword in voltageKeywords)
                {
                    if (line.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        hasVoltageKeyword = true;
                        break;
                    }
                }

                if (!hasVoltageKeyword)
                    continue; // Skip lines that don't talk about voltage

                // STEP 2: Extract voltage value with regex
                var match = voltageRegex.Match(line);
                if (!match.Success)
                    continue; // No voltage value found

                // STEP 3: Parse voltage as double (using InvariantCulture for safety)
                string voltageStr = match.Groups[1].Value;
                if (!double.TryParse(voltageStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double voltage))
                    continue; // Failed to parse

                // STEP 4: Sanity check (voltage should be in reasonable range 0-20V)
                if (voltage < 0 || voltage > 20)
                    continue; // Probably not a real voltage value

                // STEP 5: Classify and emit finding
                FindingSeverity severity;
                string message;

                if (voltage <= 11.0)
                {
                    // CRITICAL: Imminent risk of ECU brownout, reset, or programming failure
                    severity = FindingSeverity.Critical;
                    message = $"CRITICAL: Battery/system voltage is {voltage:F1}V (threshold: ≤11.0V). " +
                              $"Risk of ECU brownout, watchdog reset, or flash programming failure. " +
                              $"Immediate action required. | Source: {line}";
                }
                else if (voltage <= 11.8)
                {
                    // WARNING: Below recommended operating voltage
                    severity = FindingSeverity.Warning;
                    message = $"WARNING: Battery/system voltage is {voltage:F1}V (threshold: ≤11.8V). " +
                              $"Below recommended operating range. May cause instability or programming issues. " +
                              $"Monitor and charge battery if needed. | Source: {line}";
                }
                else
                {
                    // Voltage is acceptable (>11.8V), no finding needed
                    continue;
                }

                // Add the finding to the result
                result.Findings.Add(new Finding
                {
                    RuleId = "LOW_VOLTAGE_RULE",
                    Code = "LOW_VOLTAGE",
                    Severity = severity,
                    Title = "Low Battery/System Voltage Detected",
                    WhyItMatters = "Low voltage can cause ECU instability, resets, flash failures, and communication loss.",
                    LineNumber = i + 1, // Convert to 1-based line number
                    LineText = line,
                    Evidence = $"Measured: {voltage:F1}V"
                });

                // Only create ONE finding per line (avoid duplicates)
                // Continue to next line after detecting voltage issue
                continue;
            }
        }

        // ========================================================================
        // LOW_SOC RULE
        // ========================================================================
        /// <summary>
        /// Detects low State of Charge (SoC) conditions for battery systems.
        /// 
        /// WHY THIS RULE EXISTS:
        /// - Hybrid/EV and start-stop systems depend on battery SoC for:
        ///   * Engine restart capability
        ///   * Regenerative braking
        ///   * Electric motor assist
        /// - Low SoC can cause:
        ///   * Reduced performance
        ///   * Inability to restart engine
        ///   * System shutdown warnings
        /// - This is a deterministic, rule-based check (no AI/ML).
        /// 
        /// DETECTION LOGIC:
        /// 1. Keyword gate: line must contain one of:
        ///    "state of charge", "soc", "battery charge", "charge level", "battery level"
        /// 2. Extract percentage with regex: (\d{1,3})\s*%
        /// 3. Parse as integer and validate range 0-100
        /// 4. Classify:
        ///    - < 50% => CRITICAL (may shut down, cannot restart)
        ///    - < 70% => WARNING (reduced capability)
        /// 5. Emit Finding with Code="LOW_SOC" and include original source line
        /// 
        /// SAFETY:
        /// - Keyword gate prevents false positives (e.g., "download progress 65%")
        /// - Handles null/empty lines gracefully
        /// - Only creates one finding per line (uses 'continue' after detection)
        /// - Validates percentage is 0-100 to avoid nonsense values
        /// </summary>
        private static void ApplyLowSocRule(IReadOnlyList<string> lines, AnalysisResult result)
        {
            // Defensive: null or empty collection
            if (lines == null || lines.Count == 0) return;

            // Keywords that indicate this line is talking about state of charge
            // (prevents false positives from random percentages like "download progress 65%")
            var socKeywords = new[]
            {
                "state of charge",
                "soc",
                "battery charge",
                "charge level",
                "battery level"
            };

            // Regex to extract percentage: matches "82%", "68 %", etc.
            var percentRegex = new Regex(@"(\d{1,3})\s*%", RegexOptions.Compiled);

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];

                // Defensive: skip null or empty lines
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // STEP 1: Keyword gate - must mention SoC-related term
                bool hasSocKeyword = false;
                foreach (var keyword in socKeywords)
                {
                    if (line.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        hasSocKeyword = true;
                        break;
                    }
                }

                if (!hasSocKeyword)
                    continue; // Skip lines that don't talk about SoC/battery charge

                // STEP 2: Extract percentage value with regex
                var match = percentRegex.Match(line);
                if (!match.Success)
                    continue; // No percentage found

                // STEP 3: Parse percentage as integer
                string percentStr = match.Groups[1].Value;
                if (!int.TryParse(percentStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out int percent))
                    continue; // Failed to parse

                // STEP 4: Sanity check (percentage should be 0-100)
                if (percent < 0 || percent > 100)
                    continue; // Invalid percentage value

                // STEP 5: Classify and emit finding
                FindingSeverity severity;
                string message;

                if (percent < 50)
                {
                    // CRITICAL: Battery critically low, system may shut down
                    severity = FindingSeverity.Critical;
                    message = $"CRITICAL: Battery State of Charge is {percent}% (threshold: <50%). " +
                              $"System may shut down, engine restart may fail, or electric assist unavailable. " +
                              $"Immediate charging required. | Source: {line}";
                }
                else if (percent < 70)
                {
                    // WARNING: Below recommended operating level
                    severity = FindingSeverity.Warning;
                    message = $"WARNING: Battery State of Charge is {percent}% (threshold: <70%). " +
                              $"Below recommended operating range. Performance may be reduced. " +
                              $"Consider charging battery. | Source: {line}";
                }
                else
                {
                    // SoC is acceptable (>=70%), no finding needed
                    continue;
                }

                // Add the finding to the result
                result.Findings.Add(new Finding
                {
                    RuleId = "LOW_SOC_RULE",
                    Code = "LOW_SOC",
                    Severity = severity,
                    Title = "Low Battery State of Charge Detected",
                    WhyItMatters = "Low SoC can cause reduced performance, engine restart failure, or system shutdown.",
                    LineNumber = i + 1, // Convert to 1-based line number
                    LineText = line,
                    Evidence = $"Measured: {percent}%"
                });

                // Only create ONE finding per line (avoid duplicates)
                // Continue to next line after detecting SoC issue
                continue;
            }
        }

        private static void ApplyCriticalRules(
            IReadOnlyList<string> lines,
            List<CriticalRule> rules,
            AnalysisResult result)
        {
            // Defensive checks
            if (lines == null || lines.Count == 0) return;
            if (rules == null || rules.Count == 0) return;

            // Track every matched line index per rule so we can do clustering
            // Example: "3 timeouts within 250 lines" -> one Critical finding
            var hitsByRule = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);

            // STEP A: Find raw hits (rule matches line)
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i] ?? string.Empty;

                foreach (var rule in rules)
                {
                    if (rule.IsMatch(line))
                    {
                        if (!hitsByRule.TryGetValue(rule.RuleId, out var list))
                        {
                            list = new List<int>();
                            hitsByRule[rule.RuleId] = list;
                        }

                        list.Add(i);
                    }
                }
            }

            // STEP B: Emit findings (single-hit or clustered)
            foreach (var rule in rules)
            {
                if (!hitsByRule.TryGetValue(rule.RuleId, out var hitLines) || hitLines.Count == 0)
                    continue;

                int minHits = Math.Max(1, rule.MinHitsInWindow);
                int window = Math.Max(1, rule.WindowLines);

                // Case 1: single-hit rule -> emit a finding for each match
                if (minHits <= 1 || window <= 1)
                {
                    foreach (var idx in hitLines)
                    {
                        result.Findings.Add(new Finding
                        {
                            RuleId = rule.RuleId,
                            Code = rule.FindingCode,
                            Severity = rule.Severity,
                            Title = rule.Title,
                            WhyItMatters = rule.WhyItMatters,
                            LineNumber = idx + 1, // convert to 1-based
                            LineText = lines[idx] ?? string.Empty
                        });
                    }
                    continue;
                }

                // Case 2: clustered rule -> emit ONE finding once threshold is met
                int start = 0;
                for (int end = 0; end < hitLines.Count; end++)
                {
                    while (hitLines[end] - hitLines[start] >= window)
                        start++;

                    int count = end - start + 1;
                    if (count >= minHits)
                    {
                        int anchor = hitLines[start];

                        result.Findings.Add(new Finding
                        {
                            RuleId = rule.RuleId,
                            Code = rule.FindingCode,
                            Severity = rule.Severity,
                            Title = rule.Title,
                            WhyItMatters = rule.WhyItMatters,
                            LineNumber = anchor + 1,
                            LineText = lines[anchor] ?? string.Empty,
                            Evidence = $"Hits={count} within {window} lines"
                        });

                        // One alert per rule (keeps reports readable)
                        break;
                    }
                }
            }

            // Optional: sort so Critical is shown first, then by line number
            result.Findings.Sort((a, b) =>
            {
                int sev = b.Severity.CompareTo(a.Severity);
                if (sev != 0) return sev;
                return a.LineNumber.CompareTo(b.LineNumber);
            });
        }
    }
}
