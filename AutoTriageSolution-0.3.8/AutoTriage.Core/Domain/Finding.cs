namespace AutoTriage.Core
{
    /// <summary>
    /// Represents a single log entry that was identified during analysis.
    /// 
    /// IMPORTANT DESIGN NOTES:
    /// 1) This class is a reference type (class) so it can be easily shared between layers
    ///    (Core DLL -> GUI) without copying large structs.
    /// 2) We keep the original properties (LineNumber, Severity, Code, Message) so existing
    ///    GUI bindings continue to work without changes.
    /// 3) We add additional metadata fields used by the new “Critical Findings” rule system,
    ///    including traceability (RuleId) and human explanation (Title / WhyItMatters).
    /// </summary>
    public class Finding
    {
        public string RuleId { get; set; }
        public string Code { get; set; }
        public FindingSeverity Severity { get; set; }
        public string Title { get; set; }
        public string WhyItMatters { get; set; }
        public int LineNumber { get; set; }
        public string Evidence { get; set; }
        // Add this property to fix CS0117
        public string LineText { get; set; }
    }
}
