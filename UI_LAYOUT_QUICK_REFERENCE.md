# Quick Reference: UI Layout

## Layout Structure (Top to Bottom, Left to Right)

```
┌─────────────────────────────────────────────────────────────┐
│ [Load Log File] [Paste Log] [Clear All]      TOOLBAR       │
├──────────────┬──────────────────────────────────────────────┤
│ Loaded Logs  │                                              │
│ -----------  │  ┌────────────────────────────────────────┐  │
│ Log 1 (...)  │  │ dgvFullLog (Line #, Source, Timestamp, │  │
│ Log 2 (...)  │  │              Severity, Raw Text)       │  │
│              │  │ Shows ALL parsed log lines             │  │
│              │  │ Color-coded by severity                │  │
│              │  └────────────────────────────────────────┘  │
│              ├──────────────────────────────────────────────┤
│              │ Keyword Filter: [________________]           │
│              │ ☑ Include non-finding matches                │
│              │ Severity: ☑Critical ☑Error ☑Warning ☑Success│
│              │ ┌──────────────────────┐                     │
│              │ │ Keyword    │ Matches │                     │
│              │ │ -----------│------   │                     │
│              │ │ error      │ 15      │                     │
│              │ │ warning    │ 8       │                     │
│              │ └──────────────────────┘                     │
│              ├──────────────────────────────────────────────┤
│              │ dgvFindings (Line #, Code, Severity,        │
│              │               Title, Line Text)             │
│              │ Shows filtered findings                     │
│              │ Color-coded by severity                     │
├──────────────┴──────────────────────────────────────────────┤
│ STATUS: Showing 150 lines | Findings: 23                    │
└─────────────────────────────────────────────────────────────┘
```

## DataGridView Details

### dgvFullLog (Top Main View)
- **Purpose**: Primary view of ALL parsed log lines
- **Columns**:
  - Line # (GlobalLineNumber) - Right-aligned, resizable
  - Source (Which log file/paste) - Resizable
  - Timestamp (Extracted) - Resizable
  - Severity (Critical/Error/Warning/Success/Info) - Resizable
  - Raw Text (Full line content) - Fill, resizable
- **Filtering**: Shows keyword-matched lines when filter active
- **Row Colors**: Severity-based (Red/Orange/Yellow/Green/Cyan)

### dgvKeywordSummary (Filter Panel)
- **Purpose**: Shows match count per keyword
- **Columns**:
  - Keyword - Fill, resizable
  - Matches (Count) - Right-aligned, resizable
- **Anchoring**: Top-Left-Bottom within filter panel
- **Updates**: Whenever keyword filter changes

### dgvFindings (Bottom View)
- **Purpose**: Shows findings (with optional non-findings)
- **Columns**:
  - Line # (GlobalLineNumber) - Right-aligned, resizable
  - Code (Finding code) - Resizable
  - Severity - Resizable
  - Title (Short description) - Resizable
  - Line Text (Full text) - Fill, resizable
- **Filtering**: 
  - Severity checkboxes
  - Keyword filter (if active)
  - "Include non-finding matches" option
- **Row Colors**: Severity-based

## User Workflows

### Load a Log File
1. Click "Load Log File"
2. Select .log or .txt file
3. Automatically analyzed and displayed
4. Log added to "Loaded logs" panel
5. All three grids populate

### Paste Log Content
1. Click "Paste Log"
2. Modal dialog opens
3. Paste content into textbox
4. Click "Load"
5. Content added as "Pasted (n)"
6. Automatically analyzed and displayed

### Filter by Keywords
1. Type keywords in "Keyword Filter" textbox
2. Separate by comma, space, or newline
3. dgvFullLog filters to show only matching lines
4. dgvKeywordSummary shows match counts
5. dgvFindings filters to show only matching findings

### Filter by Severity
1. Check/uncheck severity checkboxes
2. dgvFindings updates to show only selected severities
3. Works in combination with keyword filter

### Clear Everything
1. Click "Clear All"
2. All logs removed
3. All grids cleared
4. Filters reset
5. Ready for new logs

## GlobalLineNumber Behavior
- **Line #1**: First line of first loaded log
- **Line #100**: If first log has 100 lines, second log starts at line 101
- **Preserved**: Sorting/filtering never changes GlobalLineNumber
- **Consistent**: Same line number in dgvFullLog and dgvFindings
- **Traceable**: Use "Source" column to see which log a line came from

## Color Coding
- **Critical**: Light Red (LightCoral)
- **Error**: Light Orange (LightSalmon)
- **Warning**: Light Yellow
- **Success**: Light Green
- **Info**: Light Cyan
- **Unknown**: White
