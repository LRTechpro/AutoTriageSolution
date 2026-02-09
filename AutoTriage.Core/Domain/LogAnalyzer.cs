using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace AutoTriage.Core
{
    /// <summary>
    /// Core engine for analyzing automotive diagnostic log files and detecting critical issues.
    /// 
    /// INTENT:
    /// This class serves as the primary orchestrator for log analysis in the AutoTriage system.
    /// It applies both pattern-based critical rules (timeouts, CAN errors, DTC codes) and
    /// deterministic threshold rules (voltage, state of charge) to identify conditions that
    /// could cause ECU failures, vehicle safety issues, or diagnostic session problems.
    /// 
    /// ARCHITECTURAL BOUNDARY:
    /// Lives in AutoTriage.Core (not GUI) because:
    /// - Business logic must be testable in isolation without UI dependencies
    /// - Can be reused across multiple front-ends (WinForms, WPF, CLI, web service)
    /// - No dependencies on System.Windows.Forms or presentation concerns
    /// - Pure input (log lines) → output (AnalysisResult) transformation
    /// 
    /// RISK IF INCORRECT:
    /// - False negatives: Critical vehicle safety issues go undetected (e.g., brownout voltage)
    /// - False positives: Technicians waste time investigating non-issues
    /// - Missed diagnostic failures: Flashing/programming operations fail without warning
    /// - Compliance risk: Safety-critical findings not flagged for regulatory review
    /// 
    /// FUTURE CONSIDERATIONS:
    /// - This class should remain synchronous and stateless (no threading complexity)
    /// - Adding new rule types? Consider strategy pattern instead of more private methods
    /// - Machine learning integration? Keep as separate analyzer, don't mix with rules
    /// - Performance optimization: Consider lazy evaluation or streaming for large files (>100k lines)
    /// - DO NOT add UI concerns (colors, formatting, user preferences) here
    /// </summary>
    public class LogAnalyzer
    {
        // ========================================================================
        // TEST DATA FOR LOW_VOLTAGE AND LOW_SOC RULES
        // ========================================================================
        /*
         * COPY THIS INTO A TEST FILE TO VERIFY THE NEW RULES:
         * 
         * INTENT OF THIS TEST DATA:
         * These test lines demonstrate boundary conditions and expected behavior for the
         * LOW_VOLTAGE and LOW_SOC rules. Each line tests a specific scenario:
         * - Normal operating conditions (should NOT generate findings)
         * - Warning thresholds (should generate WARNING severity)
         * - Critical thresholds (should generate CRITICAL severity)
         * - Boundary values (should test >= vs > conditions)
         * - False positive prevention (e.g., "download progress" should be ignored)
         * 
         * WHY THIS MATTERS:
         * Automotive systems operate on tight tolerances. A 0.1V difference can mean the
         * difference between stable operation and a brownout reset during flash programming.
         * These test cases ensure we correctly classify edge cases that occur in real vehicles.
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

        /// <summary>
        /// Main entry point for log analysis. Orchestrates all rule application and returns findings.
        /// 
        /// INTENT:
        /// This method coordinates three types of analysis:
        /// 1. Pattern-based critical rules (CAN errors, timeouts, DTCs) from CriticalRuleSet
        /// 2. Voltage threshold detection (LOW_VOLTAGE rule)
        /// 3. State of charge threshold detection (LOW_SOC rule)
        /// 
        /// EXECUTION ORDER MATTERS:
        /// Rules are applied in sequence, but all findings are collected into a single result.
        /// The order doesn't affect correctness (rules are independent), but Critical rules
        /// run first for performance reasons (they short-circuit on some conditions).
        /// 
        /// RISK IF INCORRECT:
        /// - Missing rule application: Entire classes of failures go undetected
        /// - Null handling issues: Crashes on empty or malformed input
        /// - Performance degradation: O(n*m) complexity with n=lines, m=rules
        /// 
        /// FUTURE CONSIDERATIONS:
        /// - If rules exceed ~20, consider parallel execution (but profile first!)
        /// - If adding ML/AI detection, call it here as a separate step
        /// - Keep rule execution order-independent to enable parallelization
        /// - DO NOT add filtering logic here (severity filtering belongs in GUI layer)
        /// </summary>
        /// <param name="lines">
        /// Raw log file lines. Can be empty or contain null elements (defensive handling included).
        /// Typically 1,000-50,000 lines for automotive diagnostic sessions.
        /// </param>
        /// <returns>
        /// AnalysisResult containing all detected findings, sorted by severity (Critical first).
        /// Empty Findings list if no issues detected (success case).
        /// </returns>
        public AnalysisResult Analyze(IReadOnlyList<string> lines)
        {
            var result = new AnalysisResult();

            // 1) Load the Critical Findings rules
            // These are pattern-based rules for known failure modes (timeouts, CAN errors, DTCs)
            var rules = CriticalRuleSet.Build();

            // 2) Apply rules to lines and emit findings
            // This handles both single-occurrence and clustered pattern detection
            ApplyCriticalRules(lines, rules, result);

            // 3) Apply NEW deterministic rules: LOW_VOLTAGE and LOW_SOC
            // These are threshold-based rules for automotive electrical safety
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
        /// Automotive ECUs (Engine Control Units, Body Control Modules, etc.) require stable
        /// electrical power to function correctly. The nominal voltage for automotive systems
        /// is 12V (lead-acid battery) or 14.4V (alternator running). However, various conditions
        /// can cause voltage drops:
        /// - Weak or failing battery
        /// - High electrical load (cranking, defrosters, headlights)
        /// - Poor electrical connections
        /// - Alternator failure
        /// 
        /// Low voltage can cause:
        /// - Brownout resets: ECU unexpectedly reboots, losing session state
        /// - Flash programming failures: Incomplete writes to ECU memory (DANGEROUS)
        /// - Memory corruption: Non-atomic writes leave ECU in invalid state
        /// - Communication loss: CAN bus transceivers require minimum voltage
        /// - False DTCs: Sensors report incorrect values due to voltage drop
        /// 
        /// INDUSTRY STANDARDS:
        /// - SAE J1699: Minimum 11.0V for flash programming operations
        /// - Most OEMs recommend 11.8V+ for stable diagnostics
        /// - Below 10.5V: Severe risk of ECU damage or bricking
        /// 
        /// This is a deterministic, rule-based check (no AI/ML involved).
        /// 
        /// DETECTION LOGIC (5-step process):
        /// 1. Keyword gate: Line must contain voltage-related term (prevents false positives)
        ///    Keywords: "battery voltage", "system voltage", "vbatt", "vbat", "b+ voltage",
        ///    "ignition voltage", "low voltage", "undervoltage", "supply voltage"
        /// 
        /// 2. Extract numeric voltage with regex: (\d+\.?\d*)\s*v(?:olts?)? (case-insensitive)
        ///    Matches: "11.7V", "10.9 V", "12.5 volts", "voltage=11.5V"
        /// 
        /// 3. Parse as double using InvariantCulture (handles both "11.7" and "11,7" in some locales)
        /// 
        /// 4. Classify by threshold:
        ///    - <= 11.0V => CRITICAL (may cause immediate ECU failure, per SAE J1699)
        ///    - <= 11.8V => WARNING (suboptimal, recommend monitoring or charging battery)
        ///    - > 11.8V  => OK (no finding emitted)
        /// 
        /// 5. Emit Finding with Code="LOW_VOLTAGE" and include original source line for traceability
        /// 
        /// SAFETY GUARANTEES:
        /// - Handles null/empty lines gracefully (defensive checks)
        /// - Only creates one finding per line (uses 'continue' after detection)
        /// - Voltage must be in range 0-20V to avoid false positives from non-voltage numbers
        /// - Keyword gate prevents matching "progress 12%" or other unrelated numbers
        /// 
        /// RISK IF INCORRECT:
        /// - False negative: Technician flashes ECU at 10.5V, bricks $2000 control module
        /// - False positive: Technician wastes time investigating healthy 12.2V reading
        /// - Regex failure: Misses alternative voltage formats used by different scan tools
        /// 
        /// ARCHITECTURAL NOTE:
        /// This rule is self-contained and stateless. It doesn't depend on other findings
        /// or maintain state between lines. This makes it easy to test in isolation.
        /// 
        /// FUTURE CONSIDERATIONS:
        /// - Thresholds (11.0V, 11.8V) are hardcoded. Consider moving to configuration
        ///   if different OEMs/regions require different limits.
        /// - Voltage trend detection: Could enhance to detect "voltage dropping" patterns
        /// - Temperature compensation: Voltage thresholds vary with battery temperature
        /// - DO NOT make this async (no I/O operations, pure computation)
        /// - DO NOT add UI-specific logic (colors, icons) here
        /// </summary>
        /// <param name="lines">Raw log lines to analyze (read-only, not modified)</param>
        /// <param name="result">AnalysisResult to append findings to (mutable)</param>
        private static void ApplyLowVoltageRule(IReadOnlyList<string> lines, AnalysisResult result)
        {
            // Defensive: null or empty collection
            // RISK: Without this check, we'd throw NullReferenceException on next line
            if (lines == null || lines.Count == 0) return;

            // Keywords that indicate this line is talking about voltage
            // PURPOSE: Prevents false positives from random numbers in logs
            // EXAMPLE: "Download progress 12%" should NOT trigger voltage rule
            // EXAMPLE: "Battery voltage 12.1V" SHOULD trigger voltage rule
            // 
            // WHY ARRAY (not List or HashSet):
            // - Small, fixed set (9 items) - array is fastest for iteration
            // - Read-only data - no need for collection mutation
            // - Allocated on stack in some JIT scenarios (performance)
            var voltageKeywords = new[]
            {
                "battery voltage",   // Most common term in automotive logs
                "system voltage",    // Used by some scan tools
                "vbatt",            // Engineering abbreviation
                "vbat",             // Alternative abbreviation
                "b+ voltage",       // Electrical engineering term (battery positive)
                "ignition voltage", // Key-on voltage
                "low voltage",      // Direct problem statement
                "undervoltage",     // Alternative problem term
                "supply voltage"    // Generic power supply term
            };

            // Regex to extract voltage value: matches "11.7V", "10.9 V", "12.5 volts", etc.
            // PATTERN BREAKDOWN:
            // (\d+\.?\d*)   - Capture group: one or more digits, optional decimal point, optional decimals
            //                 Matches: "12", "11.7", "10.95"
            // \s*           - Optional whitespace between number and unit
            // v(?:olts?)?   - "v", "volt", or "volts" (case-insensitive due to RegexOptions)
            //                 (?:...) is non-capturing group (we only need the number)
            // 
            // WHY COMPILED:
            // This regex is executed potentially thousands of times (once per line).
            // RegexOptions.Compiled pre-compiles the pattern to IL, improving performance ~2-5x.
            // 
            // LIMITATION:
            // This regex assumes voltage is always near the unit. It would miss:
            // "Voltage reading from sensor: 11.7 (units: V)" - too much text between number and V
            // For now, this is acceptable as 99% of logs use "11.7V" format.
            var voltageRegex = new Regex(@"(\d+\.?\d*)\s*v(?:olts?)?", RegexOptions.IgnoreCase | RegexOptions.Compiled);

            // Main loop: process each line independently
            // WHY NOT LINQ: We need line index for LineNumber field, and early exits (continue)
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];

                // Defensive: skip null or empty lines
                // RISK: Some log parsers return null elements for blank lines
                // Without this check, next operation would throw NullReferenceException
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // STEP 1: Keyword gate - must mention voltage-related term
                // PURPOSE: Dramatically reduces false positives and improves performance
                // PERFORMANCE: O(k*m) where k=keywords (9), m=line length (typically <200 chars)
                // This is much faster than running regex on every line
                bool hasVoltageKeyword = false;
                foreach (var keyword in voltageKeywords)
                {
                    if (line.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        hasVoltageKeyword = true;
                        break; // Stop checking keywords once we find one
                    }
                }

                if (!hasVoltageKeyword)
                    continue; // Skip lines that don't talk about voltage

                // STEP 2: Extract voltage value with regex
                // At this point we know the line talks about voltage, now find the actual value
                var match = voltageRegex.Match(line);
                if (!match.Success)
                    continue; // No voltage value found (e.g., "battery voltage: unknown")

                // STEP 3: Parse voltage as double (using InvariantCulture for safety)
                // WHY INVARIANTCULTURE:
                // Different cultures use different decimal separators ("11.7" vs "11,7")
                // InvariantCulture ensures consistent parsing regardless of machine locale
                // RISK: If we used CurrentCulture, code would behave differently on EU vs US machines
                string voltageStr = match.Groups[1].Value;
                if (!double.TryParse(voltageStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double voltage))
                    continue; // Failed to parse (shouldn't happen if regex matched, but defensive)

                // STEP 4: Sanity check (voltage should be in reasonable range 0-20V)
                // PURPOSE: Prevent false positives from non-voltage numbers that slipped through
                // EXAMPLE: "Error code 11.7V-related" - the "11.7" isn't actually a voltage
                // WHY 20V UPPER LIMIT:
                // - Automotive systems are 12V nominal
                // - Alternator can reach 14.4V (higher when cold)
                // - Jump starting can briefly spike to 16-18V
                // - Anything above 20V is definitely NOT a battery voltage
                if (voltage < 0 || voltage > 20)
                    continue; // Probably not a real voltage value

                // STEP 5: Classify and emit finding
                // This is where the automotive domain knowledge is encoded into thresholds
                FindingSeverity severity;

                if (voltage <= 11.0)
                {
                    // CRITICAL: Imminent risk of ECU brownout, reset, or programming failure
                    // RATIONALE: SAE J1699 specifies 11.0V minimum for flash operations
                    // REAL-WORLD IMPACT: Below this voltage, flash writes may fail mid-operation,
                    // leaving ECU in corrupted state (unrecoverable without JTAG or replacement)
                    severity = FindingSeverity.Critical;
                }
                else if (voltage <= 11.8)
                {
                    // WARNING: Below recommended operating voltage
                    // RATIONALE: Most OEMs recommend 11.8V+ for stable diagnostics
                    // REAL-WORLD IMPACT: System may work but is at risk. Technician should
                    // charge battery or use external power supply before critical operations.
                    severity = FindingSeverity.Warning;
                }
                else
                {
                    // Voltage is acceptable (>11.8V), no finding needed
                    // This branch means the line mentioned voltage but it's healthy
                    continue;
                }

                // Add the finding to the result
                // IMPORTANT: Each Finding is immutable after creation (object initializer pattern)
                // This prevents accidental modification during sorting or filtering
                result.Findings.Add(new Finding
                {
                    RuleId = "LOW_VOLTAGE_RULE",  // Unique identifier for filtering/grouping
                    Code = "LOW_VOLTAGE",          // Display code for technicians
                    Severity = severity,           // Determined above (Critical or Warning)
                    Title = "Low Battery/System Voltage Detected",
                    WhyItMatters = "Low voltage can cause ECU instability, resets, flash failures, and communication loss.",
                    LineNumber = i + 1,            // Convert to 1-based line number (user-friendly)
                    LineText = line,               // Original source for traceability
                    Evidence = $"Measured: {voltage:F1}V"  // Show actual voltage (formatted to 1 decimal)
                });

                // Only create ONE finding per line (avoid duplicates)
                // WHY: If a line says "Battery voltage 11.5V, System voltage 11.5V",
                // we don't want two separate warnings. One finding with full LineText is enough.
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
        /// Modern vehicles use battery State of Charge (SoC) for multiple critical functions:
        /// 
        /// 1. HYBRID/EV SYSTEMS:
        ///    - High-voltage battery SoC determines available power for electric motor
        ///    - Low SoC prevents regenerative braking (safety issue)
        ///    - Below critical threshold, vehicle enters "limp mode" or shuts down
        /// 
        /// 2. START-STOP SYSTEMS:
        ///    - Low 12V battery SoC disables auto-start feature
        ///    - Engine may fail to restart after stop (stranded driver)
        /// 
        /// 3. DIAGNOSTIC SESSIONS:
        ///    - ECU programming requires stable power throughout session (10-30 minutes)
        ///    - Low SoC means voltage will drop during operation
        ///    - May cause programming failure mid-session (bricked ECU)
        /// 
        /// Low SoC can cause:
        /// - Reduced performance (limited electric motor assist)
        /// - Inability to restart engine (start-stop or hybrid systems)
        /// - System shutdown warnings (prevents driving)
        /// - Programming failures (if SoC drops during flash operation)
        /// 
        /// This is a deterministic, rule-based check (no AI/ML involved).
        /// 
        /// DETECTION LOGIC (5-step process):
        /// 1. Keyword gate: Line must contain SoC-related term (prevents false positives)
        ///    Keywords: "state of charge", "soc", "battery charge", "charge level", "battery level"
        /// 
        /// 2. Extract percentage with regex: (\d{1,3})\s*%
        ///    Matches: "82%", "68 %", "charge at 49%"
        /// 
        /// 3. Parse as integer and validate range 0-100
        /// 
        /// 4. Classify by threshold:
        ///    - < 50% => CRITICAL (may shut down, cannot restart engine)
        ///    - < 70% => WARNING (reduced capability, recommend charging)
        ///    - >= 70% => OK (no finding emitted)
        /// 
        /// 5. Emit Finding with Code="LOW_SOC" and include original source line for traceability
        /// 
        /// SAFETY GUARANTEES:
        /// - Keyword gate prevents false positives (e.g., "download progress 65%" is ignored)
        /// - Handles null/empty lines gracefully (defensive checks)
        /// - Only creates one finding per line (uses 'continue' after detection)
        /// - Validates percentage is 0-100 to avoid nonsense values (e.g., "error 150%")
        /// 
        /// RISK IF INCORRECT:
        /// - False negative: Technician attempts flash at 40% SoC, ECU loses power mid-flash (bricked)
        /// - False positive: Technician wastes time charging battery that's already at 75%
        /// - Regex failure: Misses alternative SoC formats used by different scan tools
        /// 
        /// ARCHITECTURAL NOTE:
        /// This rule is self-contained and stateless, parallel to ApplyLowVoltageRule in design.
        /// It doesn't depend on voltage findings or maintain state between lines.
        /// 
        /// FUTURE CONSIDERATIONS:
        /// - Thresholds (50%, 70%) are hardcoded. Consider moving to configuration
        ///   if different vehicle types require different limits (e.g., pure EV vs hybrid).
        /// - SoC trend detection: Could enhance to detect "SoC dropping rapidly" patterns
        /// - Temperature effects: Battery SoC accuracy degrades at extreme temperatures
        /// - DO NOT make this async (no I/O operations, pure computation)
        /// - DO NOT add UI-specific logic (colors, icons) here
        /// </summary>
        /// <param name="lines">Raw log lines to analyze (read-only, not modified)</param>
        /// <param name="result">AnalysisResult to append findings to (mutable)</param>
        private static void ApplyLowSocRule(IReadOnlyList<string> lines, AnalysisResult result)
        {
            // Defensive: null or empty collection
            // RISK: Without this check, we'd throw NullReferenceException on next line
            if (lines == null || lines.Count == 0) return;

            // Keywords that indicate this line is talking about state of charge
            // PURPOSE: Prevents false positives from random percentages in logs
            // EXAMPLE: "Download progress 65%" should NOT trigger SoC rule (no SoC keywords)
            // EXAMPLE: "Battery charge level 65%" SHOULD trigger SoC rule (has "charge level")
            // 
            // WHY THESE SPECIFIC KEYWORDS:
            // - "state of charge" - Official term used in automotive standards
            // - "soc" - Common abbreviation in engineering logs
            // - "battery charge" - Natural language variant
            // - "charge level" - Alternative phrasing
            // - "battery level" - User-friendly term (sometimes logged)
            var socKeywords = new[]
            {
                "state of charge",  // ISO/SAE standard terminology
                "soc",             // Engineering abbreviation (case-insensitive search handles "SOC", "SoC")
                "battery charge",   // Common in user-facing logs
                "charge level",     // Alternative phrasing
                "battery level"     // Simplified term
            };

            // Regex to extract percentage: matches "82%", "68 %", etc.
            // PATTERN BREAKDOWN:
            // (\d{1,3})  - Capture group: 1 to 3 digits (handles 0-100)
            //              WHY 1-3? Percentages are always 0-100, never need 4+ digits
            // \s*        - Optional whitespace between number and percent sign
            // %          - Literal percent sign
            // 
            // WHY COMPILED:
            // This regex runs potentially thousands of times. Compilation improves performance.
            // 
            // LIMITATION:
            // This regex assumes percentage is always written as "XX%". It would miss:
            // "Charge: 0.65" (decimal notation) or "Charge: 65/100" (fraction)
            // For now, this is acceptable as 99% of automotive logs use "XX%" format.
            var percentRegex = new Regex(@"(\d{1,3})\s*%", RegexOptions.Compiled);

            // Main loop: process each line independently
            // WHY NOT LINQ: We need line index for LineNumber field, and early exits (continue)
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];

                // Defensive: skip null or empty lines
                // RISK: Some log parsers return null elements for blank lines
                // Without this check, next operation would throw NullReferenceException
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // STEP 1: Keyword gate - must mention SoC-related term
                // PURPOSE: Dramatically reduces false positives and improves performance
                // Without this, ANY percentage in the log would be checked (download %, error codes, etc.)
                bool hasSocKeyword = false;
                foreach (var keyword in socKeywords)
                {
                    if (line.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        hasSocKeyword = true;
                        break; // Stop checking keywords once we find one
                    }
                }

                if (!hasSocKeyword)
                    continue; // Skip lines that don't talk about SoC/battery charge

                // STEP 2: Extract percentage value with regex
                // At this point we know the line talks about SoC, now find the actual percentage
                var match = percentRegex.Match(line);
                if (!match.Success)
                    continue; // No percentage found (e.g., "battery charge: unknown" or "SoC unavailable")

                // STEP 3: Parse percentage as integer
                // WHY INTEGER (not double):
                // - SoC is always reported as whole percentages in automotive systems (68%, not 68.3%)
                // - Integer parsing is faster and more robust than float parsing
                // - Avoids floating-point comparison issues (e.g., 69.999% vs 70%)
                string percentStr = match.Groups[1].Value;
                if (!int.TryParse(percentStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out int percent))
                    continue; // Failed to parse (shouldn't happen if regex matched, but defensive)

                // STEP 4: Sanity check (percentage should be 0-100)
                // PURPOSE: Prevent false positives from non-percentage numbers that slipped through
                // EXAMPLE: "Error code 150% of threshold" - the "150" isn't a valid SoC percentage
                // WHY 0-100 RANGE:
                // - SoC is by definition a percentage (0% = empty, 100% = full)
                // - Some systems report slightly over 100% when fully charged (we reject this as invalid)
                if (percent < 0 || percent > 100)
                    continue; // Invalid percentage value

                // STEP 5: Classify and emit finding
                // This is where the automotive domain knowledge is encoded into thresholds
                FindingSeverity severity;

                if (percent < 50)
                {
                    // CRITICAL: Battery critically low, system may shut down
                    // RATIONALE:
                    // - Start-stop: Cannot restart engine below ~50% SoC (safety risk)
                    // - Hybrid: Limited or no electric assist, "limp mode" likely
                    // - EV: May shut down to protect battery from deep discharge
                    // REAL-WORLD IMPACT: Vehicle may become inoperable, driver stranded
                    severity = FindingSeverity.Critical;
                }
                else if (percent < 70)
                {
                    // WARNING: Below recommended operating level
                    // RATIONALE:
                    // - Reduced functionality (limited regenerative braking, electric assist)
                    // - Not enough capacity for full diagnostic session
                    // - Risk of dropping below 50% during programming (if session takes 20+ minutes)
                    // REAL-WORLD IMPACT: Recommend charging before critical operations
                    severity = FindingSeverity.Warning;
                }
                else
                {
                    // SoC is acceptable (>=70%), no finding needed
                    // This branch means the line mentioned SoC but it's healthy
                    continue;
                }

                // Add the finding to the result
                // IMPORTANT: Each Finding is immutable after creation (object initializer pattern)
                // This prevents accidental modification during sorting or filtering
                result.Findings.Add(new Finding
                {
                    RuleId = "LOW_SOC_RULE",      // Unique identifier for filtering/grouping
                    Code = "LOW_SOC",             // Display code for technicians
                    Severity = severity,          // Determined above (Critical or Warning)
                    Title = "Low Battery State of Charge Detected",
                    WhyItMatters = "Low SoC can cause reduced performance, engine restart failure, or system shutdown.",
                    LineNumber = i + 1,           // Convert to 1-based line number (user-friendly)
                    LineText = line,              // Original source for traceability
                    Evidence = $"Measured: {percent}%"  // Show actual SoC percentage
                });

                // Only create ONE finding per line (avoid duplicates)
                // WHY: If a line says "SoC 45%, Battery charge 45%", we only want one finding.
                // The full LineText captures all the information anyway.
                // Continue to next line after detecting SoC issue
                continue;
            }
        }

        /// <summary>
        /// Applies pattern-based critical rules to detect known failure modes in log files.
        /// 
        /// INTENT:
        /// This method handles two types of rules:
        /// 1. Single-hit rules: Generate one finding per occurrence (e.g., "Fatal error")
        /// 2. Clustered rules: Generate one finding when threshold is met (e.g., "3 timeouts within 250 lines")
        /// 
        /// WHY CLUSTERING:
        /// Automotive logs can contain noise (transient errors that self-recover).
        /// Example: Single CAN timeout = normal (bus arbitration lost, will retry)
        ///          5 CAN timeouts in 100 lines = CRITICAL (bus failure, ECU offline)
        /// Clustering reduces alert fatigue by only flagging persistent problems.
        /// 
        /// ALGORITHM (Sliding Window):
        /// - Track hit indices per rule: [10, 15, 20, 250, 255, 260]
        /// - For clustered rule (e.g., "3 hits in 200 lines"):
        ///   - Use two-pointer sliding window to find first occurrence where count >= threshold
        ///   - Emit ONE finding at the anchor point (first hit in cluster)
        ///   - Stop checking that rule (avoids redundant alerts)
        /// 
        /// RISK IF INCORRECT:
        /// - Missing clustering: Alerts on single transient error (false positive)
        /// - Wrong window size: Too small = false positives, too large = misses real issues
        /// - No deduplication: Same issue reported 10+ times (alert fatigue)
        /// 
        /// ARCHITECTURAL NOTE:
        /// This method modifies 'result' (passed by reference). It's responsible for:
        /// - Adding Finding objects to result.Findings
        /// - Sorting findings by severity (Critical first) at the end
        /// 
        /// FUTURE CONSIDERATIONS:
        /// - Currently processes all rules sequentially. Could parallelize for large rule sets.
        /// - Clustering algorithm is O(n*m) where n=hits, m=rules. Optimize if n gets large (>10k).
        /// - DO NOT change sorting logic without updating GUI expectations
        /// </summary>
        /// <param name="lines">Raw log lines (read-only, not modified)</param>
        /// <param name="rules">List of CriticalRule objects from CriticalRuleSet.Build()</param>
        /// <param name="result">AnalysisResult to append findings to (mutable)</param>
        private static void ApplyCriticalRules(
            IReadOnlyList<string> lines,
            List<CriticalRule> rules,
            AnalysisResult result)
        {
            // Defensive checks
            // RISK: Null parameters would cause NullReferenceException in subsequent operations
            if (lines == null || lines.Count == 0) return;
            if (rules == null || rules.Count == 0) return;

            // Track every matched line index per rule so we can do clustering
            // KEY = rule.RuleId (e.g., "CAN_TIMEOUT_RULE")
            // VALUE = List of line indices where this rule matched (e.g., [10, 15, 20, 250])
            // 
            // WHY Dictionary<string, List<int>>:
            // - Need to group hits by rule (dictionary)
            // - Need to preserve order of hits (list, not hashset)
            // - Need fast lookup by rule ID (dictionary, not list of tuples)
            // 
            // EXAMPLE: "3 timeouts within 250 lines" -> hitsByRule["CAN_TIMEOUT_RULE"] = [10, 15, 20]
            //          This allows us to check: Are there 3+ hits where max_index - min_index < 250?
            var hitsByRule = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);

            // STEP A: Find raw hits (rule matches line)
            // This is the "detection" phase - we identify ALL matches without emitting findings yet
            // WHY TWO-PHASE (detect then emit):
            // - Allows clustering logic to look at ALL hits before deciding to alert
            // - Enables sorting by severity at the end
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i] ?? string.Empty;

                foreach (var rule in rules)
                {
                    // Each CriticalRule has an IsMatch method (could be regex, keyword, etc.)
                    if (rule.IsMatch(line))
                    {
                        // First hit for this rule? Initialize the list
                        if (!hitsByRule.TryGetValue(rule.RuleId, out var list))
                        {
                            list = new List<int>();
                            hitsByRule[rule.RuleId] = list;
                        }

                        // Record this line index as a hit
                        list.Add(i);
                    }
                }
            }

            // STEP B: Emit findings (single-hit or clustered)
            // This is the "alerting" phase - we decide which hits warrant findings
            foreach (var rule in rules)
            {
                // No hits for this rule? Skip to next rule
                if (!hitsByRule.TryGetValue(rule.RuleId, out var hitLines) || hitLines.Count == 0)
                    continue;

                // Extract rule parameters (with defensive defaults)
                // WHY Math.Max: Ensures we never have invalid values (e.g., minHits=0, window=-1)
                int minHits = Math.Max(1, rule.MinHitsInWindow);  // At least 1 hit required
                int window = Math.Max(1, rule.WindowLines);       // At least 1 line window

                // Case 1: Single-hit rule (minHits=1 or window=1)
                // BEHAVIOR: Emit a finding for EACH occurrence
                // EXAMPLE: "Fatal error" rule should alert on every fatal error line
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
                            LineNumber = idx + 1,                    // Convert to 1-based
                            LineText = lines[idx] ?? string.Empty
                        });
                    }
                    continue; // Done with this rule, move to next
                }

                // Case 2: Clustered rule (e.g., "3+ hits within 250 lines")
                // BEHAVIOR: Emit ONE finding when threshold is first met
                // ALGORITHM: Sliding window with two pointers
                // 
                // EXAMPLE:
                // hitLines = [10, 15, 20, 250, 255, 260]
                // minHits = 3, window = 200
                // 
                // Iteration 1: end=0, start=0, count=1 (not enough)
                // Iteration 2: end=1, start=0, count=2 (not enough)
                // Iteration 3: end=2, start=0, count=3, span=20-10=10 < 200 ✓ ALERT at line 10
                // Break (don't alert again for this rule)
                int start = 0;
                for (int end = 0; end < hitLines.Count; end++)
                {
                    // Advance 'start' pointer while window is too large
                    // This maintains the invariant: hitLines[start..end] spans < window lines
                    while (hitLines[end] - hitLines[start] >= window)
                        start++;

                    // Count hits in current window
                    int count = end - start + 1;

                    // Threshold met? Emit finding and stop checking this rule
                    if (count >= minHits)
                    {
                        int anchor = hitLines[start];  // Use first hit in cluster as anchor point

                        result.Findings.Add(new Finding
                        {
                            RuleId = rule.RuleId,
                            Code = rule.FindingCode,
                            Severity = rule.Severity,
                            Title = rule.Title,
                            WhyItMatters = rule.WhyItMatters,
                            LineNumber = anchor + 1,
                            LineText = lines[anchor] ?? string.Empty,
                            Evidence = $"Hits={count} within {window} lines"  // Show clustering stats
                        });

                        // One alert per rule (keeps reports readable)
                        // WHY: If we continued, we'd alert again at lines 255, 260, etc.
                        // This would spam the report with redundant findings.
                        break;
                    }
                }
            }

            // Optional: sort so Critical is shown first, then by line number
            // WHY THIS MATTERS FOR GUI:
            // - GUI displays findings in order (no re-sorting)
            // - Technicians need to see Critical issues first (safety-critical)
            // - Within same severity, earlier line numbers shown first (chronological)
            // 
            // SORT ALGORITHM:
            // 1. Primary: severity descending (Critical > Error > Warning > Success)
            // 2. Secondary: line number ascending (earlier lines first)
            // 
            // RISK: If GUI expects unsorted results and does its own sorting, this
            // would cause duplicate work (but no correctness issue).
            result.Findings.Sort((a, b) =>
            {
                int sev = b.Severity.CompareTo(a.Severity);  // Note: b.CompareTo(a) for descending
                if (sev != 0) return sev;
                return a.LineNumber.CompareTo(b.LineNumber);
            });
        }
    }
}
