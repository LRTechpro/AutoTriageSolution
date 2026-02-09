using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AutoTriage.Core
{
    /// <summary>
    /// Defines one “rule” used to detect critical issues in automotive diagnostic logs.
    /// 
    /// WHY THIS EXISTS:
    /// - Keeps detection logic data-driven (keywords/regex + explanation)
    /// - Prevents huge if/else chains
    /// - Makes it easy to add/remove rules without rewriting analyzer logic
    /// </summary>
    public class CriticalRule
    {
        public string RuleId { get; set; } = string.Empty;
        public string FindingCode { get; set; } = string.Empty;
        public FindingSeverity Severity { get; set; } = FindingSeverity.Error;

        public string Title { get; set; } = string.Empty;
        public string WhyItMatters { get; set; } = string.Empty;

        // “Any keyword” matching (fast, simple)
        public string[] KeywordsAny { get; set; } = Array.Empty<string>();

        // Regex matching (powerful for patterns like long hex/base64 blobs)
        public Regex[] RegexAny { get; set; } = Array.Empty<Regex>();

        // Optional clustering detection (ex: “3 timeouts in 250 lines”)
        public int MinHitsInWindow { get; set; } = 1;
        public int WindowLines { get; set; } = 1;

        /// <summary>
        /// True if this rule matches the provided line.
        /// </summary>
        public bool IsMatch(string line)
        {
            if (line == null)
                line = string.Empty;

            foreach (var k in KeywordsAny)
            {
                if (!string.IsNullOrWhiteSpace(k) &&
                    line.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            foreach (var r in RegexAny)
            {
                if (r != null && r.IsMatch(line))
                    return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Central list of “Critical Findings” rules.
    /// This is where you paste/maintain the big list of automotive + cybersecurity indicators.
    /// </summary>
    public static class CriticalRuleSet
    {
        private static readonly RegexOptions RX =
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

        /// <summary>
        /// Builds the highest-signal rules first.
        /// Expand this list over time as you validate new log patterns.
        /// </summary>
        public static List<CriticalRule> Build()
        {
            var rules = new List<CriticalRule>
            {
                // ============================================================
                // 1) SYSTEM STABILITY AND CRASH INDICATORS
                // ============================================================

                new CriticalRule
                {
                    RuleId = "CRIT_STAB_WATCHDOG_RESET",
                    FindingCode = "WATCHDOG_RESET",
                    Severity = FindingSeverity.Critical,
                    Title = "Watchdog / hard reset indicator",
                    WhyItMatters = "ECU instability can cause reboot loops, loss of function, and safety impact.",
                    KeywordsAny = new[]
                    {
                        "WATCHDOG", "WDT", "WDT RESET", "RESET REASON: WATCHDOG",
                        "HARD FAULT", "BUS FAULT", "MEMMANAGE", "USAGE FAULT",
                        "panic", "kernel panic", "assert failed", "fatal exception"
                    }
                },
                new CriticalRule
                {
                    RuleId = "CRIT_STAB_MEMORY_CORRUPTION",
                    FindingCode = "MEMORY_CORRUPTION",
                    Severity = FindingSeverity.Critical,
                    Title = "Memory corruption / heap / stack failure",
                    WhyItMatters = "Memory safety defects are reliability failures and can be exploitable.",
                    KeywordsAny = new[]
                    {
                        "heap corruption", "malloc failed", "invalid pointer", "double free",
                        "stack overflow", "stack smashing", "guard page", "canary", "corrupt",
                        "segfault", "access violation", "SIGSEGV"
                    }
                },
                new CriticalRule
                {
                    RuleId = "CRIT_STAB_OOM_POOL",
                    FindingCode = "OUT_OF_MEMORY",
                    Severity = FindingSeverity.Critical,
                    Title = "Out-of-memory / pool exhausted",
                    WhyItMatters = "Predicts cascading failures (dropped messages, missed deadlines, feature outages).",
                    KeywordsAny = new[]
                    {
                        "OOM", "out of memory", "allocation failed", "no memory", "ENOMEM",
                        "fragmentation", "pool exhausted", "buffer pool depleted"
                    }
                },

                // ============================================================
                // 2) SECURITY / INTRUSION / TAMPER INDICATORS
                // ============================================================

                new CriticalRule
                {
                    RuleId = "CRIT_SEC_UNAUTHORIZED",
                    FindingCode = "AUTH_FAILURE",
                    Severity = FindingSeverity.Critical,
                    Title = "Unauthorized access / authentication failure",
                    WhyItMatters = "May indicate attempted compromise or broken/misconfigured security controls.",
                    KeywordsAny = new[]
                    {
                        "unauthorized", "access denied", "permission denied", "auth failed",
                        "invalid token", "token expired", "bad credentials", "login failed"
                    }
                },

                new CriticalRule
                {
                    RuleId = "CRIT_SEC_CRYPTO_CERT",
                    FindingCode = "CRYPTO_CERT_FAIL",
                    Severity = FindingSeverity.Critical,
                    Title = "Crypto / certificate verification failure",
                    WhyItMatters = "Can break OTA trust and secure comms; may indicate tampering.",
                    KeywordsAny = new[]
                    {
                        "cert verify failed", "chain invalid", "revoked", "OCSP", "CRL",
                        "HSM error", "key unwrap failed", "invalid key", "signature invalid",
                        "AES", "GCM", "tag mismatch", "MAC invalid", "decrypt failed",
                        "TLS handshake failed", "mTLS", "bad record mac"
                    }
                },

                new CriticalRule
                {
                    RuleId = "CRIT_SEC_SECURE_BOOT",
                    FindingCode = "SECURE_BOOT_FAIL",
                    Severity = FindingSeverity.Critical,
                    Title = "Secure boot / integrity check failure",
                    WhyItMatters = "Firmware integrity failures can indicate compromised software or broken update pipeline.",
                    KeywordsAny = new[]
                    {
                        "secure boot failed", "boot verification failed",
                        "hash mismatch", "integrity check failed", "image invalid",
                        "rollback detected", "anti-rollback", "downgrade attempt"
                    }
                },

                // ============================================================
                // 3) OTA / FLASH / PROGRAMMING FAILURES
                // ============================================================

                new CriticalRule
                {
                    RuleId = "CRIT_OTA_FLASH_FAIL",
                    FindingCode = "FLASH_PROGRAM_FAIL",
                    Severity = FindingSeverity.Critical,
                    Title = "Flash programming failure",
                    WhyItMatters = "ECU may be left unusable; vehicle function impacted.",
                    KeywordsAny = new[]
                    {
                        "programming failed", "flash write failed", "erase failed", "verify failed",
                        "ECU not responding during programming", "session dropped",
                        "block sequence error", "transfer data error"
                    }
                },

                new CriticalRule
                {
                    RuleId = "CRIT_OTA_PACKAGE_INVALID",
                    FindingCode = "OTA_PACKAGE_INVALID",
                    Severity = FindingSeverity.Critical,
                    Title = "Update package / manifest invalid",
                    WhyItMatters = "Can brick modules or indicate malicious/incorrect update artifacts.",
                    KeywordsAny = new[]
                    {
                        "package invalid", "manifest invalid", "metadata mismatch",
                        "signature verification failed", "update aborted", "rollback initiated", "update state corrupt"
                    }
                },

                // ============================================================
                // 4) UDS-SPECIFIC CRITICAL FINDINGS (ISO 14229)
                // ============================================================

              new CriticalRule
              {
                    RuleId = "CRIT_UDS_27_SECURITYACCESS_CLUSTER",
                    FindingCode = "UDS_27_SECURITYACCESS_ANOMALY",
                    Severity = FindingSeverity.Critical,
                    Title = "UDS SecurityAccess (0x27) anomaly pattern",
                    WhyItMatters = "Indicates brute-force attempts, mis-implemented security, or tool misuse.",
                    KeywordsAny = new[] { "0x27", "SecurityAccess", "seed/key", "invalid key", "exceeded attempts", "locked", "delay not expired" },
                    MinHitsInWindow = 3,
                    WindowLines = 200
              },

              new CriticalRule
              {
                    RuleId = "CRIT_UDS_10_SESSION_FAIL",
                    FindingCode = "UDS_10_SESSION_FAIL",
                    Severity = FindingSeverity.Critical,
                    Title = "UDS DiagnosticSessionControl (0x10) failure",
                    WhyItMatters = "Programming/security operations depend on session changes.",
                    KeywordsAny = new[] { "0x10", "DiagnosticSessionControl", "programming session", "extended session", "session change", "NRC" }
              },

               new CriticalRule
                {
                    RuleId = "CRIT_UDS_34_36_37_TRANSFER_FAIL",
                    FindingCode = "UDS_DOWNLOAD_TRANSFER_FAIL",
                    Severity = FindingSeverity.Critical,
                    Title = "UDS download/transfer pipeline failure (0x34/0x36/0x37)",
                    WhyItMatters = "Programming pipeline breakage; risk of partial flash.",
                    KeywordsAny = new[] { "0x34", "RequestDownload", "0x36", "TransferData", "0x37", "RequestTransferExit", "wrong block sequence", "transfer aborted", "download denied" }
                },

                new CriticalRule
                {
                    RuleId = "CRIT_UDS_7F_HIGH_IMPACT_NRC",
                    FindingCode = "UDS_7F_HIGH_IMPACT_NRC",
                    Severity = FindingSeverity.Critical,
                    Title = "UDS NegativeResponse (0x7F) with high-impact NRC",
                    WhyItMatters = "Indicates security lockouts, programming failures, or session/condition violations.",
                    // Regex catches NRC hex values. This supports multiple common log formats.
                    RegexAny = new[]
                    {
                        new Regex(@"\b0x7F\b", RX),
                        new Regex(@"\bNRC\b.*0x(35|36|37|72|73|78|7E|81)\b", RX),
                        new Regex(@"\b0x(35|36|37|72|73|78|7E|81)\b", RX)
                    }
                },

                // ============================================================
                // 5) TRANSPORT / NETWORK LAYER (CAN / ISO-TP / DOIP)
                // ============================================================

                new CriticalRule
                {
                    RuleId = "CRIT_NET_ISOTP_FAIL",
                    FindingCode = "ISOTP_SEGMENTATION_FAIL",
                    Severity = FindingSeverity.Critical,
                    Title = "ISO-TP segmentation / flow control failure",
                    WhyItMatters = "Transport instability breaks UDS operations and programming.",
                    KeywordsAny = new[] { "isotp", "first frame", "consecutive frame", "flow control", "sequence error", "missed CF", "FC timeout", "STmin violation", "rx overrun", "buffer overflow" }
                },

                new CriticalRule
                {
                    RuleId = "CRIT_NET_DOIP_FAIL",
                    FindingCode = "DOIP_FAIL",
                    Severity = FindingSeverity.Critical,
                    Title = "DoIP routing/connection failure",
                    WhyItMatters = "Blocks diagnostics/programming; may indicate network or gateway issues.",
                    KeywordsAny = new[] { "DoIP", "routing activation failed", "alive check failed", "TCP reset", "connection dropped", "socket error", "keepalive timeout", "invalid payload length", "payload type unsupported" }
                },

                new CriticalRule
                {
                    RuleId = "CRIT_NET_BUS_OFF",
                    FindingCode = "BUS_OFF_OR_NETWORK_DOWN",
                    Severity = FindingSeverity.Critical,
                    Title = "Bus-off / network down",
                    WhyItMatters = "Loss of communication; systemic fault affecting multiple ECUs.",
                    KeywordsAny = new[] { "BUS OFF", "busoff", "error passive", "tx error counter", "gateway reset", "network manager failure", "CAN shutdown" }
                },

                // ============================================================
                // 6) HEX / BINARY / ASCII-RELATED CRITICAL FINDINGS
                // ============================================================

                new CriticalRule
                {
                    RuleId = "CRIT_DATA_LONG_HEX_RUN",
                    FindingCode = "SUSPICIOUS_LONG_HEX_RUN",
                    Severity = FindingSeverity.Critical,
                    Title = "Suspicious long hex run (possible dump/raw frames)",
                    WhyItMatters = "May indicate memory leakage into logs (PII/keys), raw payload dumps, or crash dumps.",
                    RegexAny = new[]
                    {
                        new Regex(@"(?:\b[0-9A-F]{2}\b[\s:,-]*){64,}", RX),
                        new Regex(@"\b0x[0-9A-F]{128,}\b", RX)
                    }
                },

                new CriticalRule
                {
                    RuleId = "CRIT_DATA_SECRET_MATERIAL",
                    FindingCode = "SECRETS_IN_LOGS",
                    Severity = FindingSeverity.Critical,
                    Title = "Potential secret material in logs (cert/key/base64)",
                    WhyItMatters = "Secrets in logs are a serious compliance and security issue.",
                    RegexAny = new[]
                    {
                        new Regex(@"BEGIN\s+CERTIFICATE", RX),
                        new Regex(@"BEGIN\s+PRIVATE\s+KEY", RX),
                        new Regex(@"\b[A-Za-z0-9+/]{200,}={0,2}\b", RX)
                    }
                },

                // ============================================================
                // 7) TIMEOUTS AND “STUCK” CONDITIONS
                // ============================================================

                new CriticalRule
                {
                    RuleId = "CRIT_STUCK_TIMEOUT_CLUSTER",
                    FindingCode = "REPEATED_TIMEOUTS",
                    Severity = FindingSeverity.Critical,
                    Title = "Repeated timeouts / no response cluster",
                    WhyItMatters = "Indicates dead ECU path, blocked operation, or transport collapse.",
                    KeywordsAny = new[] { "timeout", "timed out", "no response", "response timeout" },
                    MinHitsInWindow = 3,
                    WindowLines = 250
                },

                // ============================================================
                // 8) DATA INTEGRITY AND PARSER-SAFETY FINDINGS
                // ============================================================

               new CriticalRule
               {
                    RuleId = "CRIT_DATA_TRUNC_OR_CRC",
                    FindingCode = "TRUNCATED_OR_INTEGRITY_FAIL",
                    Severity = FindingSeverity.Critical,
                    Title = "Truncated frames / invalid length / CRC failure",
                    WhyItMatters = "Data corruption or malformed payloads can break programming and diagnostics.",
                    KeywordsAny = new[] { "invalid length", "length mismatch", "short read", "truncated", "CRC error", "checksum failed", "parity error" }
               },

                new CriticalRule
                {
                    RuleId = "CRIT_DATA_OVERFLOW_OOB",
                    FindingCode = "OVERFLOW_OR_OOB",
                    Severity = FindingSeverity.Critical,
                    Title = "Overflow / out-of-bounds indicator",
                    WhyItMatters = "Often security-relevant, not just reliability.",
                    KeywordsAny = new[] { "overflow", "overrun", "out of bounds", "index out of range" }
                }
            };

            return rules;
        }
    }
}
