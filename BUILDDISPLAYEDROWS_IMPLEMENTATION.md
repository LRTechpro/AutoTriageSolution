# Complete BuildDisplayedRows() Implementation

## Replace the existing BuildDisplayedRows method with this version:

```csharp
/// <summary>
/// Builds the list of ResultRow objects to display in the grid.
/// Uses the new multi-log architecture with SourceFile and LogDate support.
/// </summary>
private List<ResultRow> BuildDisplayedRows()
{
    // Create empty result list
    var result = new List<ResultRow>();

    // Check if we have data from either old or new architecture
    bool hasNewData = (currentCase != null && currentCase.LogFiles.Any());
    bool hasOldData = (currentResult != null);
    
    if (!hasNewData && !hasOldData)
    {
        return result;  // No data available
    }

    // Parse keywords from filter textbox
    string[] keywords = ParseKeywords(txtKeywordFilter.Text);

    System.Diagnostics.Debug.WriteLine($"==== BuildDisplayedRows ====");
    System.Diagnostics.Debug.WriteLine($"Has new data: {hasNewData}, Has old data: {hasOldData}");
    System.Diagnostics.Debug.WriteLine($"Keywords: [{string.Join(", ", keywords)}] Count: {keywords.Length}");

    // ========== KEYWORD FILTER MODE ==========
    if (keywords.Length > 0)
    {
        List<AutoTriage.Models.LogLine> linesToSearch;
        
        // Use new architecture if available, otherwise fall back to old
        if (hasNewData)
        {
            // Get all lines from all log files in the vehicle case
            linesToSearch = currentCase.GetAllLines().ToList();
            System.Diagnostics.Debug.WriteLine($"Using NEW architecture: {linesToSearch.Count} lines");
        }
        else
        {
            // Fall back to old architecture - convert to new LogLine format
            linesToSearch = new List<AutoTriage.Models.LogLine>();
            foreach (var oldLine in currentResult.AllLines)
            {
                linesToSearch.Add(new AutoTriage.Models.LogLine
                {
                    LineNumber = oldLine.LineNumber,
                    RawText = oldLine.RawText ?? "",
                    DetectedSeverity = ConvertSeverity(oldLine.DetectedSeverity),
                    IsFinding = oldLine.IsFinding
                });
            }
            System.Diagnostics.Debug.WriteLine($"Using OLD architecture (converted): {linesToSearch.Count} lines");
        }
        
        // Use LogFilter to get matches with counts
        var matchedLines = logFilter.FilterByKeywords(linesToSearch, keywords);
        
        System.Diagnostics.Debug.WriteLine($"Matched lines: {matchedLines.Count}");
        
        // Convert each matched line to ResultRow
        foreach (var logLine in matchedLines)
        {
            // Determine row color based on severity
            Color rowColor = logLine.DetectedSeverity switch
            {
                AutoTriage.Models.FindingSeverity.Critical => Color.LightCoral,
                AutoTriage.Models.FindingSeverity.Error => Color.LightSalmon,
                AutoTriage.Models.FindingSeverity.Warning => Color.LightYellow,
                AutoTriage.Models.FindingSeverity.Success => Color.LightGreen,
                AutoTriage.Models.FindingSeverity.Info => Color.LightCyan,
                _ => Color.White
            };

            // Extract timestamp string
            string timestamp = logLine.Timestamp?.ToString("HH:mm:ss.fff") ?? ExtractTimestamp(logLine.RawText);
            
            // Extract source file and date (if available)
            string sourceFile = "";
            string logDate = "";
            if (logLine.SourceFile != null)
            {
                sourceFile = System.IO.Path.GetFileName(logLine.SourceFile.FilePath);
                logDate = logLine.SourceFile.LogDate.ToString("yyyy-MM-dd");
            }

            // Add the result row
            result.Add(new ResultRow
            {
                LineNumber = logLine.LineNumber,
                Timestamp = timestamp,
                Code = logLine.IsFinding ? "FINDING" : "KEYWORD",
                Severity = logLine.DetectedSeverity.ToString(),
                LineText = SanitizeForGrid(logLine.RawText),
                RowColor = rowColor,
                SourceFile = sourceFile,
                LogDate = logDate
            });
        }

        // Update status with keyword match statistics
        int totalMatches = matchedLines.Count;
        lblStatus.Text = string.Format("Keyword Search: {0} lines scanned | {1} matches found | Keywords: [{2}]",
            linesToSearch.Count,
            totalMatches,
            string.Join(", ", keywords));
    }
    // ========== SEVERITY FILTER MODE (NO KEYWORDS) ==========
    else
    {
        // Check which severity filters are selected
        bool anySeveritySelected = chkCritical.Checked || chkError.Checked || 
                                   chkWarning.Checked || chkSuccess.Checked;

        if (hasNewData)
        {
            // === USE NEW ARCHITECTURE ===
            
            // Get all lines from all log files
            var allLines = currentCase.GetAllLines().ToList();
            
            // Build list of selected severities
            var selectedSeverities = new List<AutoTriage.Models.FindingSeverity>();
            if (chkCritical.Checked) selectedSeverities.Add(AutoTriage.Models.FindingSeverity.Critical);
            if (chkError.Checked) selectedSeverities.Add(AutoTriage.Models.FindingSeverity.Error);
            if (chkWarning.Checked) selectedSeverities.Add(AutoTriage.Models.FindingSeverity.Warning);
            if (chkSuccess.Checked) selectedSeverities.Add(AutoTriage.Models.FindingSeverity.Success);
            
            List<AutoTriage.Models.LogLine> linesToShow;
            
            if (selectedSeverities.Any())
            {
                // Filter by selected severities
                linesToShow = logFilter.FilterBySeverity(allLines, selectedSeverities.ToArray());
            }
            else if (!chkNRC.Checked)
            {
                // No filters active at all - show nothing or only findings
                linesToShow = new List<AutoTriage.Models.LogLine>();
                lblStatus.Text = "No filters active. Select severity filters, NRC filter, or enter keywords.";
            }
            else
            {
                // No severity filters but NRC is checked - show all lines
                linesToShow = allLines.ToList();
            }
            
            // Apply NRC filter if unchecked (hide NRC lines)
            if (!chkNRC.Checked)
            {
                linesToShow = linesToShow.Where(l => !ContainsNrc(l.RawText)).ToList();
            }
            
            // Only show lines that are findings
            linesToShow = linesToShow.Where(l => l.IsFinding).ToList();
            
            // Convert to ResultRows
            foreach (var logLine in linesToShow)
            {
                Color rowColor = logLine.DetectedSeverity switch
                {
                    AutoTriage.Models.FindingSeverity.Critical => Color.LightCoral,
                    AutoTriage.Models.FindingSeverity.Error => Color.LightSalmon,
                    AutoTriage.Models.FindingSeverity.Warning => Color.LightYellow,
                    AutoTriage.Models.FindingSeverity.Success => Color.LightGreen,
                    AutoTriage.Models.FindingSeverity.Info => Color.LightCyan,
                    _ => Color.White
                };

                string timestamp = logLine.Timestamp?.ToString("HH:mm:ss.fff") ?? ExtractTimestamp(logLine.RawText);
                
                string sourceFile = "";
                string logDate = "";
                if (logLine.SourceFile != null)
                {
                    sourceFile = System.IO.Path.GetFileName(logLine.SourceFile.FilePath);
                    logDate = logLine.SourceFile.LogDate.ToString("yyyy-MM-dd");
                }

                result.Add(new ResultRow
                {
                    LineNumber = logLine.LineNumber,
                    Timestamp = timestamp,
                    Code = "FINDING",
                    Severity = logLine.DetectedSeverity.ToString(),
                    LineText = SanitizeForGrid(logLine.RawText),
                    RowColor = rowColor,
                    SourceFile = sourceFile,
                    LogDate = logDate
                });
            }
        }
        else if (hasOldData)
        {
            // === USE OLD ARCHITECTURE (BACKWARD COMPATIBILITY) ===
            
            List<AutoTriage.Core.Finding> findingsToShow = currentResult.Findings.ToList();

            if (anySeveritySelected)
            {
                findingsToShow = findingsToShow.Where(f =>
                    (chkCritical.Checked && f.Severity == AutoTriage.Core.FindingSeverity.Critical) ||
                    (chkError.Checked && f.Severity == AutoTriage.Core.FindingSeverity.Error) ||
                    (chkWarning.Checked && f.Severity == AutoTriage.Core.FindingSeverity.Warning) ||
                    (chkSuccess.Checked && f.Severity == AutoTriage.Core.FindingSeverity.Success)
                ).ToList();
            }
            else if (!chkNRC.Checked)
            {
                findingsToShow.Clear();
                lblStatus.Text = "No filters active. Select severity filters, NRC filter, or enter keywords.";
            }

            System.Diagnostics.Debug.WriteLine($"Findings mode (OLD): {findingsToShow.Count} findings");

            // Convert to ResultRow
            foreach (var finding in findingsToShow)
            {
                // Apply NRC code filter
                if (!ShouldShowBasedOnNrcFilter(finding.LineText ?? ""))
                {
                    continue;
                }

                Color rowColor = finding.Severity switch
                {
                    AutoTriage.Core.FindingSeverity.Critical => Color.LightCoral,
                    AutoTriage.Core.FindingSeverity.Error => Color.LightSalmon,
                    AutoTriage.Core.FindingSeverity.Warning => Color.LightYellow,
                    AutoTriage.Core.FindingSeverity.Success => Color.LightGreen,
                    AutoTriage.Core.FindingSeverity.Info => Color.LightCyan,
                    _ => Color.White
                };

                string lineText = SanitizeForGrid(finding.LineText ?? "");
                string timestamp = ExtractTimestamp(finding.LineText ?? "");

                result.Add(new ResultRow
                {
                    LineNumber = finding.LineNumber,
                    Timestamp = timestamp,
                    Code = SanitizeForGrid(finding.Code ?? "UNKNOWN"),
                    Severity = SanitizeForGrid(finding.Severity.ToString()),
                    LineText = lineText,
                    RowColor = rowColor,
                    SourceFile = "",  // Old architecture doesn't track source file
                    LogDate = ""
                });
            }
        }
    }

    System.Diagnostics.Debug.WriteLine($"Total rows to display: {result.Count}");
    return result;
}

/// <summary>
/// Helper method to convert old FindingSeverity to new FindingSeverity.
/// </summary>
private AutoTriage.Models.FindingSeverity ConvertSeverity(AutoTriage.Core.FindingSeverity oldSeverity)
{
    return oldSeverity switch
    {
        AutoTriage.Core.FindingSeverity.Critical => AutoTriage.Models.FindingSeverity.Critical,
        AutoTriage.Core.FindingSeverity.Error => AutoTriage.Models.FindingSeverity.Error,
        AutoTriage.Core.FindingSeverity.Warning => AutoTriage.Models.FindingSeverity.Warning,
        AutoTriage.Core.FindingSeverity.Success => AutoTriage.Models.FindingSeverity.Success,
        AutoTriage.Core.FindingSeverity.Info => AutoTriage.Models.FindingSeverity.Info,
        _ => AutoTriage.Models.FindingSeverity.Info
    };
}

/// <summary>
/// Helper method to check if a line contains NRC patterns.
/// </summary>
private bool ContainsNrc(string lineText)
{
    if (string.IsNullOrEmpty(lineText))
        return false;
        
    return lineText.Contains("NRC", StringComparison.OrdinalIgnoreCase) ||
           lineText.Contains("Negative Response", StringComparison.OrdinalIgnoreCase);
}
```

## Key Changes Made:

1. **Dual Architecture Support**: Works with both old (`currentResult`) and new (`currentCase`) data sources
2. **SourceFile and LogDate columns**: Populated when using new architecture
3. **LogFilter Integration**: Uses `logFilter.FilterByKeywords()` for keyword matching with counts
4. **Severity Conversion**: Helper method converts between old and new FindingSeverity enums
5. **NRC Helper**: Extracted NRC detection into reusable method
6. **Detailed Logging**: Debug output helps trace which code path is being used

## Additional Required Changes:

### 1. Remove duplicate `ShouldShowBasedOnNrcFilter` if it exists
The new `ContainsNrc()` method replaces it.

### 2. Ensure FindingSeverity enum exists in AutoTriage.Models
```csharp
namespace AutoTriage.Models
{
    public enum FindingSeverity
    {
        Info = 0,
        Success = 1,
        Warning = 2,
        Error = 3,
        Critical = 4
    }
}
```

### 3. Add severity detection when parsing files
In `LogParser.ParseFile()`, after creating each `LogLine`, detect its severity:

```csharp
// After: logLine.VoltageValue = ExtractVoltage(rawLines[i]);
logLine.DetectedSeverity = DetectSeverityFromLine(rawLines[i]);
logLine.IsFinding = IsFindingLine(rawLines[i]);
```

And add these helper methods to LogParser:

```csharp
private FindingSeverity DetectSeverityFromLine(string line)
{
    string lowerLine = line.ToLower();
    
    // Critical keywords
    if (lowerLine.Contains("critical") || lowerLine.Contains("fatal") || 
        lowerLine.Contains("crash") || lowerLine.Contains("abort"))
        return FindingSeverity.Critical;
    
    // Error keywords
    if (lowerLine.Contains("error") || lowerLine.Contains("fail") || 
        lowerLine.Contains("exception") || lowerLine.Contains("nrc"))
        return FindingSeverity.Error;
    
    // Warning keywords
    if (lowerLine.Contains("warning") || lowerLine.Contains("warn") || 
        lowerLine.Contains("caution") || lowerLine.Contains("low voltage"))
        return FindingSeverity.Warning;
    
    // Success keywords
    if (lowerLine.Contains("success") || lowerLine.Contains("complete") || 
        lowerLine.Contains("ok") || lowerLine.Contains("passed"))
        return FindingSeverity.Success;
    
    // Default
    return FindingSeverity.Info;
}

private bool IsFindingLine(string line)
{
    string lowerLine = line.ToLower();
    
    // Line is a finding if it contains any of these patterns
    return lowerLine.Contains("error") ||
           lowerLine.Contains("warning") ||
           lowerLine.Contains("critical") ||
           lowerLine.Contains("fail") ||
           lowerLine.Contains("nrc") ||
           lowerLine.Contains("exception") ||
           lowerLine.Contains("success") ||
           lowerLine.Contains("complete");
}
```

## Testing After Implementation:

1. Load multiple log files
2. Verify SourceFile and LogDate columns populate
3. Test keyword filtering - verify counts panel appears
4. Test severity filtering
5. Test session detection and comparison
6. Test switching between keyword and severity modes

This completes the integration!
