using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoTriage.Core
{
    public class LogAnalyzer
    {
        public AnalysisResult Analyze(IReadOnlyList<string> lines)
        {
            var result = new AnalysisResult();

            // 1) Load the Critical Findings rules
            var rules = CriticalRuleSet.Build();

            // 2) Apply rules to lines and emit findings
            ApplyCriticalRules(lines, rules, result);

            return result;
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
