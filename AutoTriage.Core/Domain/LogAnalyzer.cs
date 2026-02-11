using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AutoTriage.Core
{
    public class LogAnalyzer
    {
        private readonly Regex ExplicitTagRegex;

        public LogAnalyzer()
        {
            ExplicitTagRegex = new Regex(
                @"\b(CRITICAL|ERROR|WARN(?:ING)?|SUCCESS|INFO)\b[\s:\-]",
                RegexOptions.IgnoreCase | RegexOptions.Compiled
            );
        }

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
                    Findings = new List<Finding>(),
                    AllLines = new List<LogLine>()
                };
            }

            var findings = new List<Finding>();
            var allLines = new List<LogLine>();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // Determine severity using tag-first approach
                FindingSeverity severity;
                bool hasExplicitTag;
                DetermineSeverity(line, out severity, out hasExplicitTag);

                bool isFinding = false;

                // Only create findings for non-info severities or important info
                if (severity != FindingSeverity.Info || ShouldIncludeInfoLine(line))
                {
                    isFinding = true;
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

                // NEW: Track ALL lines for keyword searching
                allLines.Add(new LogLine
                {
                    LineNumber = i + 1,
                    RawText = line,
                    DetectedSeverity = severity,
                    IsFinding = isFinding
                });
            }

            return new AnalysisResult
            {
                TotalLines = lines.Length,
                CriticalCount = findings.Count(f => f.Severity == FindingSeverity.Critical),
                ErrorCount = findings.Count(f => f.Severity == FindingSeverity.Error),
                WarningCount = findings.Count(f => f.Severity == FindingSeverity.Warning),
                SuccessCount = findings.Count(f => f.Severity == FindingSeverity.Success),
                Score = CalculateScore(findings),
                Findings = findings,
                AllLines = allLines
            };
        }

        /// <summary>
        /// Searches for keyword matches across ALL lines (not just findings).
        /// Returns findings created from matching lines.
        /// </summary>
        public List<Finding> SearchKeywordsInAllLines(List<LogLine> allLines, string[] keywords, bool includeNonFindings)
        {
            if (allLines == null || allLines.Count == 0 || keywords == null || keywords.Length == 0)
            {
                return new List<Finding>();
            }

            var results = new List<Finding>();

            foreach (var logLine in allLines)
            {
                // Skip non-findings if the toggle is OFF
                if (!includeNonFindings && !logLine.IsFinding)
                    continue;

                // Case-insensitive substring matching
                string lineLower = logLine.RawText.ToLowerInvariant();
                bool matchesAnyKeyword = keywords.Any(kw => lineLower.Contains(kw.ToLowerInvariant()));

                if (matchesAnyKeyword)
                {
                    results.Add(new Finding
                    {
                        LineNumber = logLine.LineNumber,
                        Severity = logLine.DetectedSeverity,
                        LineText = logLine.RawText.Trim(),
                        Title = ExtractTitle(logLine.RawText, logLine.DetectedSeverity),
                        Code = logLine.IsFinding ? ExtractCode(logLine.DetectedSeverity, true) : "KEYWORD",
                        RuleId = string.Format("KEYWORD_{0}", logLine.LineNumber),
                        Evidence = logLine.RawText.Trim(),
                        WhyItMatters = logLine.IsFinding ? GetWhyItMatters(logLine.DetectedSeverity) : "Matched keyword filter"
                    });
                }
            }

            return results;
        }

        /// <summary>
        /// Parses keyword input with multiple separators and quoted phrases.
        /// </summary>
        public static string[] ParseKeywords(string keywordText)
        {
            if (string.IsNullOrWhiteSpace(keywordText))
                return new string[0];

            var keywords = new List<string>();
            
            // Handle quoted phrases first
            var quotedRegex = new Regex(@"""([^""]+)""", RegexOptions.Compiled);
            var quotedMatches = quotedRegex.Matches(keywordText);
            
            foreach (Match match in quotedMatches)
            {
                keywords.Add(match.Groups[1].Value.Trim());
            }

            // Remove quoted phrases from the text
            string remaining = quotedRegex.Replace(keywordText, " ");

            // Split by multiple separators: newline, comma, semicolon, space
            string[] separators = new string[] { "\r\n", "\n", ",", ";", " ", "\t" };
            var tokens = remaining.Split(separators, StringSplitOptions.RemoveEmptyEntries);

            foreach (var token in tokens)
            {
                string trimmed = token.Trim();
                if (!string.IsNullOrWhiteSpace(trimmed) && !keywords.Contains(trimmed))
                {
                    keywords.Add(trimmed);
                }
            }

            return keywords.ToArray();
        }

        /// <summary>
        /// Validation method to verify keyword matching works correctly.
        /// </summary>
        public void ValidateKeywordMatching()
        {
            string[] testLines = new string[]
            {
                "soc: 75%",
                "SOC: 69%",
                "Battery State of Charge (SOC)",
                "[INFO] Starting system",
                "ERROR: Connection failed"
            };

            var result = Analyze(testLines, null);
            var keywords = ParseKeywords("soc");
            var matches = SearchKeywordsInAllLines(result.AllLines, keywords, true);

            if (matches.Count != 3)
            {
                throw new Exception(string.Format(
                    "Keyword validation FAILED! Expected 3 matches for 'soc', got {0}",
                    matches.Count));
            }

            System.Diagnostics.Debug.WriteLine("✓ Keyword matching validation PASSED - 'soc' matched correctly");
        }

        private void DetermineSeverity(string line, out FindingSeverity severity, out bool hasExplicitTag)
        {
            hasExplicitTag = false;
            
            if (string.IsNullOrWhiteSpace(line))
            {
                severity = FindingSeverity.Info;
                return;
            }

            var tagMatch = ExplicitTagRegex.Match(line);
            if (tagMatch.Success)
            {
                hasExplicitTag = true;
                string tag = tagMatch.Groups[1].Value.ToUpperInvariant();
                
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

            string lineLower = line.ToLowerInvariant();
            
            if (CriticalKeywords.Any(kw => lineLower.Contains(kw)))
            {
                severity = FindingSeverity.Critical;
                return;
            }

            if (ErrorKeywords.Any(kw => lineLower.Contains(kw)))
            {
                severity = FindingSeverity.Error;
                return;
            }

            if (WarningKeywords.Any(kw => lineLower.Contains(kw)))
            {
                severity = FindingSeverity.Warning;
                return;
            }

            if (SuccessKeywords.Any(kw => lineLower.Contains(kw)))
            {
                severity = FindingSeverity.Success;
                return;
            }

            severity = FindingSeverity.Info;
        }

        private bool ShouldIncludeInfoLine(string line)
        {
            string lineLower = line.ToLowerInvariant();
            
            if (lineLower.Contains("[info]") || lineLower.Contains("info:") || Regex.IsMatch(line, @"\bINFO\b", RegexOptions.IgnoreCase))
                return true;

            string[] interestingKeywords = { "starting", "loading", "initializing", "config", "version" };
            return interestingKeywords.Any(kw => lineLower.Contains(kw));
        }

        private string ExtractTitle(string line, FindingSeverity severity)
        {
            string cleaned = Regex.Replace(line, @"^\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2}", "").Trim();
            cleaned = Regex.Replace(cleaned, @"^\[\d+\]\s*", "").Trim();
            cleaned = Regex.Replace(cleaned, @"^\[\w+\]\s*", "").Trim();
            cleaned = Regex.Replace(cleaned, @"^\w+[\s:\-]", "", RegexOptions.IgnoreCase);
            
            if (cleaned.Length > 80)
                cleaned = cleaned.Substring(0, 77) + "...";
            
            return cleaned;
        }

        private string ExtractCode(FindingSeverity severity, bool hasExplicitTag)
        {
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
