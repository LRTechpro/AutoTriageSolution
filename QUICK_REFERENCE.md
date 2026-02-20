# Quick Reference - AutoTriage Multi-Log Enhancement

## 🚀 Quick Start (To Finish Implementation)

### Step 1: Update BuildDisplayedRows() [15 min]
**File**: `AutoTriage.Gui/Form1.cs`
**Action**: Replace entire `BuildDisplayedRows()` method
**Source**: Copy from `BUILDDISPLAYEDROWS_IMPLEMENTATION.md`

### Step 2: Add Severity Detection [10 min]
**File**: `AutoTriage.Analysis/LogParser.cs`
**Add these methods**:

```csharp
private FindingSeverity DetectSeverityFromLine(string line)
{
    string lower = line.ToLower();
    if (lower.Contains("critical") || lower.Contains("fatal")) return FindingSeverity.Critical;
    if (lower.Contains("error") || lower.Contains("fail")) return FindingSeverity.Error;
    if (lower.Contains("warning") || lower.Contains("warn")) return FindingSeverity.Warning;
    if (lower.Contains("success") || lower.Contains("complete")) return FindingSeverity.Success;
    return FindingSeverity.Info;
}

private bool IsFindingLine(string line)
{
    string lower = line.ToLower();
    return lower.Contains("error") || lower.Contains("warning") || 
           lower.Contains("critical") || lower.Contains("fail") || 
           lower.Contains("nrc") || lower.Contains("success");
}
```

**In ParseFile() method, after line 120, add**:
```csharp
logLine.DetectedSeverity = DetectSeverityFromLine(rawLines[i]);
logLine.IsFinding = IsFindingLine(rawLines[i]);
```

### Step 3: Verify Project References [2 min]
**File**: `AutoTriage.Gui/AutoTriage.Gui.csproj`
**Check these exist**:
```xml
<ProjectReference Include="..\AutoTriage.Models\AutoTriage.Models.csproj" />
<ProjectReference Include="..\AutoTriage.Analysis\AutoTriage.Analysis.csproj" />
```

### Step 4: Build & Test [20 min]
1. Build solution (F6)
2. Run application (F5)
3. Click "Load Logs" → select 2-3 log files
4. Verify: SourceFile and LogDate columns populate
5. Click "Analyze Log"
6. Verify: Sessions appear in right panel
7. Type keywords in filter box
8. Verify: Keyword counts panel appears at bottom
9. Select 2 sessions → click "Compare"
10. Verify: Comparison dialog shows

## ✅ What's Already Done

| Component | Status | Location |
|-----------|--------|----------|
| Data Models | ✅ Complete | `AutoTriage.Models/*.cs` |
| Parsers | ✅ Complete | `AutoTriage.Analysis/LogParser.cs` |
| Session Detection | ✅ Complete | `AutoTriage.Analysis/SessionDetector.cs` |
| Filtering | ✅ Complete | `AutoTriage.Analysis/LogFilter.cs` |
| Comparison | ✅ Complete | `AutoTriage.Analysis/SessionComparator.cs` |
| UI Layout | ✅ Complete | `Form1.InitializeCustomUI()` |
| Multi-file Load | ✅ Complete | `Form1.BtnLoadFiles_Click()` |
| Session Display | ✅ Complete | `Form1.DetectAndDisplaySessions()` |
| Session Compare | ✅ Complete | `Form1.BtnCompareSessions_Click()` |
| Keyword Counts | ✅ Complete | `Form1.UpdateKeywordCounts()` |
| Line Numbering | ✅ Complete | Line number panel + throttling |

## ⚠️ What Needs Work

| Task | Priority | Time | File |
|------|----------|------|------|
| BuildDisplayedRows() | 🔴 HIGH | 15 min | Form1.cs |
| Severity Detection | 🔴 HIGH | 10 min | LogParser.cs |
| Testing | 🟡 MEDIUM | 20 min | N/A |

## 🎯 Testing Checklist

- [ ] Load single file → works
- [ ] Load multiple files → all appear in grid
- [ ] SourceFile column shows file names
- [ ] LogDate column shows dates
- [ ] Click Analyze → sessions detected
- [ ] Sessions grid shows programming sessions
- [ ] Enter keyword → counts panel appears
- [ ] Counts panel shows per-keyword totals
- [ ] Select 2 sessions → Compare button enables
- [ ] Click Compare → dialog shows comparison
- [ ] Severity filters work (Critical, Error, Warning, Success)
- [ ] NRC filter works
- [ ] Clear All resets everything
- [ ] Paste large log (1000+ lines) → line numbers sync correctly

## 📊 Architecture at a Glance

```
Form1 (UI)
   │
   ├─ Loads → VehicleCase (contains List<LogFile>)
   │              └─ Each LogFile has List<LogLine>
   │
   ├─ Uses → LogParser (parses files)
   ├─ Uses → SessionDetector (finds programming sessions)
   ├─ Uses → LogFilter (filters by keywords/severity)
   └─ Uses → SessionComparator (compares sessions)
```

## 🐛 Common Issues & Fixes

**Issue**: SourceFile/LogDate columns empty
**Fix**: Update BuildDisplayedRows() to populate these (Step 1)

**Issue**: All lines show as "Info" severity
**Fix**: Add severity detection to LogParser (Step 2)

**Issue**: "Type/namespace not found" error
**Fix**: Add project references (Step 3)

**Issue**: No sessions detected
**Fix**: Check that LogParser is calling severity detection

**Issue**: Keyword counts don't appear
**Fix**: Verify UpdateKeywordCounts() is called in TxtKeywordFilter_TextChanged

## 📞 Key Method Call Flow

### Loading Files:
```
BtnLoadFiles_Click()
  → logParser.ParseFiles(filePaths)
    → LogParser.ParseFile() for each
      → Creates LogFile with List<LogLine>
      → Normalizes line endings
      → Extracts timestamps, voltage
      → Detects severity ⚠️ STEP 2
  → Adds to currentCase.LogFiles
```

### Analyzing:
```
PerformAnalysis()
  → ApplyFiltersAndDisplay()
    → BuildDisplayedRows() ⚠️ STEP 1
      → Uses currentCase.GetAllLines()
      → Filters by keywords/severity
      → Populates SourceFile/LogDate
  → DetectAndDisplaySessions()
    → sessionDetector.DetectProgrammingSessions()
    → Displays in dgvSessions
```

### Filtering:
```
TxtKeywordFilter_TextChanged()
  → ParseKeywords()
  → UpdateKeywordCounts()
    → logFilter.FilterByKeywords()
    → Shows dgvKeywordCounts
  → ApplyFiltersAndDisplay()
    → BuildDisplayedRows()
```

## 📚 Documentation Files

- `INTEGRATION_SUMMARY.md` - Overall architecture and status
- `BUILDDISPLAYEDROWS_IMPLEMENTATION.md` - Complete code for BuildDisplayedRows()
- `IMPLEMENTATION_ROADMAP.md` - Detailed requirements checklist
- `QUICK_REFERENCE.md` - This file

## 🎓 Learning Resources

**Understanding the Models**:
- VehicleCase = Container for entire diagnostic case
- LogFile = One log file (e.g., "log_2024-01-15.txt")
- LogLine = One line from a log file
- Session = Grouping of related lines (programming, voltage check)

**Understanding the Flow**:
1. User loads files → LogParser creates VehicleCase
2. User clicks Analyze → SessionDetector finds sessions
3. User enters keywords → LogFilter filters + counts
4. User clicks results → Can compare sessions

---

**Estimated Total Time to Complete**: ~45 minutes
**Current Status**: 85% complete
**Blockers**: None - all dependencies resolved
