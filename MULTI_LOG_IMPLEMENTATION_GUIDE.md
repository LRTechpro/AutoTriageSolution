# Multi-Log Support Implementation Guide

## Overview
This document provides the complete implementation plan to add multi-log support with consistent line numbering to the AutoTriage WinForms application.

## 1. Data Model Changes

### Created Files:
- `AutoTriage.Models/LogEntry.cs` - Canonical data model for all log lines
- `AutoTriage.Models/LoadedLogInfo.cs` - Tracks metadata about each loaded log

### Key Properties of LogEntry:
```csharp
- GlobalLineNumber (int): 1..N across ALL logs, never changes
- SourceLogId (Guid): Unique ID for the source log
- SourceFileName (string): Display name
- SourceLocalLineNumber (int): Line number within that specific file
- Timestamp (DateTime?): Parsed timestamp  
- Severity (string): Critical/Error/Warning/Success/Info/Unknown
- Code (string): Finding code (NRC, ERROR, etc.)
- RawText (string): Complete raw line text
- IsFinding (bool): Whether this line is a finding
- RowColor (Color): Display color in grid
```

## 2. UI Changes Required

### A. Top Panel - Multi-Log Management
Replace single file load with:
- `lblLoadedLogsInfo`: Label showing "Loaded logs: X | Total lines: Y"
- `lstLoadedLogs`: ListBox showing all loaded logs with line counts
- `btnLoadLogs`: Button for multi-select OpenFileDialog
- `btnPasteLog`: Button to paste from clipboard (creates "Pasted (n)" log)
- `btnRemoveLog`: Remove selected log from list
- `btnClearAllLogs`: Clear all loaded logs
- `btnAnalyze`: Analyze all loaded logs
- `btnDecoder`: Existing decoder tools

### B. Full Log DataGridView (dgvAllLines)
Columns:
1. Line # (GlobalLineNumber) - Right-aligned, 60px min
2. Source (SourceFileName) - 100px min
3. Local Line # (SourceLocalLineNumber) - Right-aligned, 80px min  
4. Timestamp - 150px min
5. Severity - 80px min
6. Code - 70px min
7. Raw Text - Fill remaining space

ALL columns must have:
- `Resizable = DataGridViewTriState.True`
- `AutoSizeMode` set appropriately (DisplayedCells or Fill)

### C. Findings DataGridView (dgvFindings, renamed from dgvResults)
Same columns as Full Log grid
Shows only filtered findings from _filteredFindings binding list

### D. Keyword Filter Panel
Add inside filter panel:
- `dgvKeywordCounts`: Small DataGridView showing:
  - Column 1: Keyword
  - Column 2: Matches (count in FILTERED dataset)
- Position: Docked/Anchored properly, not floating
- Size: ~250x75px

## 3. Core Logic Changes

### A. Loading Logs (ParseAndLoadLogLines)
```csharp
private void ParseAndLoadLogLines(string logText, string sourceName)
{
    var logId = Guid.NewGuid();
    var lines = logText.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
    
    int startGlobalLine = _allLogEntries.Count + 1;
    
    for (int i = 0; i < lines.Length; i++)
    {
        var entry = new AutoTriage.Models.LogEntry
        {
            GlobalLineNumber = startGlobalLine + i,
            SourceLogId = logId,
            SourceFileName = sourceName,
            SourceLocalLineNumber = i + 1,
            Timestamp = ExtractTimestamp(lines[i]),
            Severity = GuessSeverity(lines[i]),
            Code = "",
            RawText = lines[i],
            IsFinding = false,
            RowColor = GetColorForSeverity(severity)
        };
        _allLogEntries.Add(entry);
        _fullLogBindingList.Add(entry);
    }
    
    var logInfo = new AutoTriage.Models.LoadedLogInfo
    {
        LogId = logId,
        Name = sourceName,
        LineCount = lines.Length,
        StartGlobalLineNumber = startGlobalLine,
        EndGlobalLineNumber = startGlobalLine + lines.Length - 1,
        LoadedAt = DateTime.Now
    };
    _loadedLogs.Add(logInfo);
    _loadedLogsBindingList.Add(logInfo);
    
    UpdateLoadedLogsLabel();
}
```

### B. Multi-Select Load
```csharp
private void BtnLoadLogs_Click(object sender, EventArgs e)
{
    using var openFileDialog = new OpenFileDialog
    {
        Filter = "Log Files (*.log;*.txt)|*.log;*.txt|All Files|*.*",
        Multiselect = true,
        Title = "Select Log Files"
    };
    
    if (openFileDialog.ShowDialog() == DialogResult.OK)
    {
        foreach (string filePath in openFileDialog.FileNames)
        {
            string content = File.ReadAllText(filePath);
            string fileName = Path.GetFileName(filePath);
            ParseAndLoadLogLines(content, fileName);
        }
    }
}
```

### C. Paste Log
```csharp
private void BtnPasteLog_Click(object sender, EventArgs e)
{
    if (Clipboard.ContainsText())
    {
        _pastedLogCounter++;
        string content = Clipboard.GetText();
        ParseAndLoadLogLines(content, $"Pasted ({_pastedLogCounter})");
    }
}
```

### D. Remove Selected Log
```csharp
private void BtnRemoveLog_Click(object sender, EventArgs e)
{
    if (lstLoadedLogs.SelectedItem is AutoTriage.Models.LoadedLogInfo selected)
    {
        // Remove entries for this log
        _allLogEntries.RemoveAll(e => e.SourceLogId == selected.LogId);
        _loadedLogs.Remove(selected);
        
        // Renumber all entries
        RenumberAllEntries();
        
        // Refresh bindings
        _fullLogBindingList.Clear();
        foreach (var entry in _allLogEntries)
            _fullLogBindingList.Add(entry);
        
        _loadedLogsBindingList.Remove(selected);
        UpdateLoadedLogsLabel();
    }
}
```

### E. Renumbering Logic
```csharp
private void RenumberAllEntries()
{
    int globalLine = 1;
    var groupedByLog = _allLogEntries.GroupBy(e => e.SourceLogId).OrderBy(g => g.First().GlobalLineNumber);
    
    _allLogEntries.Clear();
    foreach (var logGroup in groupedByLog)
    {
        var logInfo = _loadedLogs.First(l => l.LogId == logGroup.Key);
        logInfo.StartGlobalLineNumber = globalLine;
        
        foreach (var entry in logGroup.OrderBy(e => e.SourceLocalLineNumber))
        {
            entry.GlobalLineNumber = globalLine++;
            _allLogEntries.Add(entry);
        }
        
        logInfo.EndGlobalLineNumber = globalLine - 1;
    }
}
```

### F. Clear All Logs
```csharp
private void BtnClearAllLogs_Click(object sender, EventArgs e)
{
    _allLogEntries.Clear();
    _loadedLogs.Clear();
    _fullLogBindingList.Clear();
    _loadedLogsBindingList.Clear();
    _filteredFindings.Clear();
    keywordCounts.Clear();
    _pastedLogCounter = 0;
    currentResult = null;
    
    UpdateLoadedLogsLabel();
    lblStatus.Text = "Ready";
}
```

### G. Update Label
```csharp
private void UpdateLoadedLogsLabel()
{
    int totalLines = _allLogEntries.Count;
    int logCount = _loadedLogs.Count;
    lblLoadedLogsInfo.Text = $"Loaded logs: {logCount} | Total lines: {totalLines}";
}
```

### H. Keyword Counting on Filtered Data
```csharp
private void ComputeKeywordCounts()
{
    keywordCounts.Clear();
    string[] keywords = ParseKeywords(txtKeywordFilter.Text);
    if (keywords.Length == 0) return;
    
    // Count in FILTERED dataset, not master list
    var activeDataset = _filteredFindings.Count > 0 ? _filteredFindings.ToList() : _allLogEntries;
    
    foreach (string keyword in keywords)
    {
        int count = activeDataset.Count(e => 
            e.RawText.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);
        
        keywordCounts.Add(new KeywordCount 
        { 
            Keyword = keyword, 
            Count = count 
        });
    }
    
    keywordCountsBindingSource.ResetBindings(false);
}
```

## 4. Analysis Integration

When analyzing, pass _allLogEntries to the analyzer, then mark findings:

```csharp
private void PerformAnalysis()
{
    if (_allLogEntries.Count == 0)
    {
        MessageBox.Show("No logs loaded", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
    }
    
    string[] lines = _allLogEntries.Select(e => e.RawText).ToArray();
    currentResult = analyzer.Analyze(lines, null);
    
    // Mark findings in canonical model
    foreach (var finding in currentResult.Findings)
    {
        var entry = _allLogEntries.FirstOrDefault(e => e.GlobalLineNumber == finding.LineNumber);
        if (entry != null)
        {
            entry.IsFinding = true;
            entry.Code = finding.Code ?? "";
            entry.Severity = finding.Severity.ToString();
        }
    }
    
    ApplyFiltersAndDisplay();
}
```

## 5. Filtering

Filter creates a subset for _filteredFindings based on severity checkboxes and keywords:

```csharp
private void ApplyFiltersAndDisplay()
{
    _filteredFindings.Clear();
    
    var filtered = _allLogEntries.Where(e => e.IsFinding);
    
    // Apply severity filters
    if (chkCritical.Checked || chkError.Checked || chkWarning.Checked || chkSuccess.Checked)
    {
        filtered = filtered.Where(e =>
            (chkCritical.Checked && e.Severity == "Critical") ||
            (chkError.Checked && e.Severity == "Error") ||
            (chkWarning.Checked && e.Severity == "Warning") ||
            (chkSuccess.Checked && e.Severity == "Success"));
    }
    
    // Apply keyword filter
    string[] keywords = ParseKeywords(txtKeywordFilter.Text);
    if (keywords.Length > 0)
    {
        filtered = filtered.Where(e => keywords.Any(kw => 
            e.RawText.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0));
    }
    
    foreach (var entry in filtered)
        _filteredFindings.Add(entry);
    
    ComputeKeywordCounts();
    findingsBindingSource.ResetBindings(false);
}
```

## 6. Self-Check Validation

After implementation:
1. Build with 0 errors
2. Load 2 files + paste 1 log
3. Verify:
   - lstLoadedLogs shows 3 entries
   - lblLoadedLogsInfo shows "Loaded logs: 3 | Total lines: [sum]"
   - First entry GlobalLineNumber = 1
   - Last entry GlobalLineNumber = total lines
   - Both grids show same GlobalLineNumber for same line
   - Drag column separators to resize - works
   - Toggle severity filters - keyword counts update
   - Remove a log - lines renumber correctly

## 7. Remaining Work

You need to:
1. Replace all references to `dgvResults` with `dgvFindings`
2. Replace all references to `displayedRows` with `_filteredFindings`
3. Replace all references to `resultsBindingSource` with `findingsBindingSource`
4. Remove obsolete `DisplayLogLine` and `ResultRow` classes
5. Update all event handlers to use new button names
6. Add the new event handlers for multi-log buttons
7. Update dgvKeywordCounts positioning in filter panel
8. Test thoroughly

This is a significant refactoring that touches most of the Form1.cs file. Consider doing it in stages:
- Stage 1: Data model (done)
- Stage 2: UI layout
- Stage 3: Loading logic
- Stage 4: Filtering logic
- Stage 5: Analysis integration
