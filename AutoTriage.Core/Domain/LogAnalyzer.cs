using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AutoTriage.Core
{
    public class LogAnalyzer
    {
        // ExplicitTagRegex is now initialized in the static constructor
        private readonly Regex ExplicitTagRegex;

        public LogAnalyzer()
        {
            ExplicitTagRegex = new Regex(
                @"\b(CRITICAL|ERROR|WARN(?:ING)?|SUCCESS|INFO)\b[\s:\-]",
                RegexOptions.IgnoreCase | RegexOptions.Compiled
            );
        }

        // Tag-first severity detection (authoritative) - Updated to handle [timestamp] prefixes

        // Keyword heuristics (only used if no explicit tag)
        private static readonly string[] CriticalKeywords = 
        {
            "critical", "fatal", "panic", "catastrophic", "system failure", "unrecoverable"
        };

        private static readonly string[] ErrorKeywords = 
        {
            "error", "fail", "failed", "failure", "denied", "abort", "aborted", 
            "exception", "crash", "timeout", "invalid", "corrupt", "refused"
        };

        private static readonly string[] WarningKeywords = 
        {
            "warn", "warning", "caution", "deprecated", "retry", "retrying", 
            "slow", "delay", "degraded", "limited"
        };

        private static readonly string[] SuccessKeywords = 
        {
            "success", "successful", "completed", "passed", "ok", "done", 
            "ready", "initialized", "started", "loaded"
        };

        public AnalysisResult Analyze(string[] lines, string[] customKeywords)
        {
            if (lines == null || lines.Length == 0)
            {
                return new AnalysisResult
                {
                    TotalLines = 0,
                    CriticalCount = 0,
                    ErrorCount = 0,
                    WarningCount = 0,
                    SuccessCount = 0,
                    Score = 0,
                    Findings = new List<Finding>()
                };
            }

            var findings = new List<Finding>();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // Determine severity using tag-first approach
                FindingSeverity severity;
                bool hasExplicitTag;
                DetermineSeverity(line, out severity, out hasExplicitTag);

                // Only create findings for non-info severities or important info
                if (severity != FindingSeverity.Info || ShouldIncludeInfoLine(line))
                {
                    findings.Add(new Finding
                    {
                        LineNumber = i + 1,
                        Severity = severity,
                        LineText = line.Trim(),
                        Title = ExtractTitle(line, severity),
                        Code = ExtractCode(severity, hasExplicitTag),
                        RuleId = string.Format("RULE_{0}_{1}", severity, i + 1),
                        Evidence = line.Trim(),
                        WhyItMatters = GetWhyItMatters(severity)
                    });
                }
            }

            return new AnalysisResult
            {
                TotalLines = lines.Length,
                CriticalCount = findings.Count(f => f.Severity == FindingSeverity.Critical),
                ErrorCount = findings.Count(f => f.Severity == FindingSeverity.Error),
                WarningCount = findings.Count(f => f.Severity == FindingSeverity.Warning),
                SuccessCount = findings.Count(f => f.Severity == FindingSeverity.Success),
                Score = CalculateScore(findings),
                Findings = findings
            };
        }

        /// <summary>
        /// Tag-first severity determination with strict precedence.
        /// 1. Check for explicit tags (AUTHORITATIVE - cannot be overridden)
        /// 2. If no tag, use keyword heuristics
        /// 3. Default to Info
        /// </summary>
        private void DetermineSeverity(string line, out FindingSeverity severity, out bool hasExplicitTag)
        {
            hasExplicitTag = false;
            
            if (string.IsNullOrWhiteSpace(line))
            {
                severity = FindingSeverity.Info;
                return;
            }

            // PHASE 1: Check for explicit tags (AUTHORITATIVE - highest priority)
            // This regex now uses \b word boundaries to find tags anywhere in the line
            var tagMatch = ExplicitTagRegex.Match(line);
            if (tagMatch.Success)
            {
                hasExplicitTag = true;
                string tag = tagMatch.Groups[1].Value.ToUpperInvariant();
                
                // Explicit tag is AUTHORITATIVE - return immediately without keyword checks
                switch (tag)
                {
                    case "CRITICAL":
                        severity = FindingSeverity.Critical;
                        return;
                    case "ERROR":
                        severity = FindingSeverity.Error;
                        return;
                    case "WARN":
                    case "WARNING":
                        severity = FindingSeverity.Warning;
                        return;
                    case "SUCCESS":
                        severity = FindingSeverity.Success;
                        return;
                    case "INFO":
                        severity = FindingSeverity.Info;
                        return;
                    default:
                        severity = FindingSeverity.Info;
                        return;
                }
            }

            // PHASE 2: No explicit tag found - use keyword heuristics ONLY
            string lineLower = line.ToLowerInvariant();
            
            // Check in order of severity (highest to lowest)
            
            // Critical keywords
            if (CriticalKeywords.Any(kw => lineLower.Contains(kw)))
            {
                severity = FindingSeverity.Critical;
                return;
            }

            // Error keywords
            if (ErrorKeywords.Any(kw => lineLower.Contains(kw)))
            {
                severity = FindingSeverity.Error;
                return;
            }

            // Warning keywords
            if (WarningKeywords.Any(kw => lineLower.Contains(kw)))
            {
                severity = FindingSeverity.Warning;
                return;
            }

            // Success keywords
            if (SuccessKeywords.Any(kw => lineLower.Contains(kw)))
            {
                severity = FindingSeverity.Success;
                return;
            }

            // Default to Info
            severity = FindingSeverity.Info;
        }

        private bool ShouldIncludeInfoLine(string line)
        {
            // Include info lines that have meaningful content
            string lineLower = line.ToLowerInvariant();
            
            // Include if it has explicit INFO tag
            if (lineLower.Contains("[info]") || lineLower.Contains("info:") || Regex.IsMatch(line, @"\bINFO\b", RegexOptions.IgnoreCase))
                return true;

            // Include if it has interesting keywords
            string[] interestingKeywords = { "starting", "loading", "initializing", "config", "version" };
            return interestingKeywords.Any(kw => lineLower.Contains(kw));
        }

        private string ExtractTitle(string line, FindingSeverity severity)
        {
            // Remove timestamp and extract meaningful title
            string cleaned = Regex.Replace(line, @"^\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2}", "").Trim();
            cleaned = Regex.Replace(cleaned, @"^\[\d+\]\s*", "").Trim(); // Remove [0008] style prefixes
            cleaned = Regex.Replace(cleaned, @"^\[\w+\]\s*", "").Trim();
            cleaned = Regex.Replace(cleaned, @"^\w+[\s:\-]", "", RegexOptions.IgnoreCase); // Remove severity tag
            
            // Truncate if too long
            if (cleaned.Length > 80)
                cleaned = cleaned.Substring(0, 77) + "...";
            
            return cleaned;
        }

        private string ExtractCode(FindingSeverity severity, bool hasExplicitTag)
        {
            // Code reflects the severity source
            switch (severity)
            {
                case FindingSeverity.Critical:
                    return hasExplicitTag ? "CRITICAL" : "CRIT";
                case FindingSeverity.Error:
                    return hasExplicitTag ? "ERROR" : "ERR";
                case FindingSeverity.Warning:
                    return hasExplicitTag ? "WARN" : "WARN";
                case FindingSeverity.Success:
                    return hasExplicitTag ? "SUCCESS" : "OK";
                case FindingSeverity.Info:
                    return "INFO";
                default:
                    return "UNKNOWN";
            }
        }

        private string GetWhyItMatters(FindingSeverity severity)
        {
            switch (severity)
            {
                case FindingSeverity.Critical:
                    return "Critical issue requiring immediate attention";
                case FindingSeverity.Error:
                    return "Error that may cause malfunction";
                case FindingSeverity.Warning:
                    return "Warning that should be investigated";
                case FindingSeverity.Success:
                    return "Successful operation";
                case FindingSeverity.Info:
                    return "Informational message";
                default:
                    return string.Empty;
            }
        }

        private int CalculateScore(List<Finding> findings)
        {
            int score = 100;
            
            score -= findings.Count(f => f.Severity == FindingSeverity.Critical) * 25;
            score -= findings.Count(f => f.Severity == FindingSeverity.Error) * 10;
            score -= findings.Count(f => f.Severity == FindingSeverity.Warning) * 2;
            score += findings.Count(f => f.Severity == FindingSeverity.Success) * 1;
            
            return Math.Max(0, Math.Min(100, score));
        }

        /// <summary>
        /// Internal validation to ensure tag-first precedence works correctly.
        /// Call this in debug mode or unit tests to verify behavior.
        /// </summary>
        public void ValidateSeverityPrecedence()
        {
            // Test cases to verify explicit tags are authoritative
            string[] testCases = new string[]
            {
                "[0008] INFO auth failed: bad credentials for user=test",
                "[0006] ERROR Failed to open diagnostic channel",
                "[0012] CRITICAL RESET REASON: WATCHDOG",
                "[0016] INFO Programming completed successfully",
                "erase_fail: flash erase failed"
            };

            FindingSeverity[] expectedSeverities = new FindingSeverity[]
            {
                FindingSeverity.Info,      // Explicit INFO tag overrides "failed" keyword
                FindingSeverity.Error,     // Explicit ERROR tag
                FindingSeverity.Critical,  // Explicit CRITICAL tag
                FindingSeverity.Info,      // Explicit INFO tag (not upgraded to Success)
                FindingSeverity.Error      // No explicit tag, uses "failed" keyword
            };

            for (int i = 0; i < testCases.Length; i++)
            {
                FindingSeverity actualSeverity;
                bool hasExplicitTag;
                DetermineSeverity(testCases[i], out actualSeverity, out hasExplicitTag);

                if (actualSeverity != expectedSeverities[i])
                {
                    throw new Exception(string.Format(
                        "Severity precedence validation FAILED!\nLine: {0}\nExpected: {1}\nActual: {2}",
                        testCases[i], expectedSeverities[i], actualSeverity));
                }
            }

            // All tests passed
            System.Diagnostics.Debug.WriteLine("✓ Severity precedence validation PASSED - All test cases correct");
        }
    }
}
