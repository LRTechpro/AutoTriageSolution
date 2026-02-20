# UI Layout Quick Reference

## Container Hierarchy

```
Form (1400x1000, MinSize: 1200x800)
│
├─ lblStatus (DockStyle.Bottom, Height=30)
│  └─ "Ready" / Status messages
│
└─ mainSplit (SplitContainer, Vertical, Dock=Fill)
   │
   ├─ Panel1: LEFT SIDEBAR (260px, resizable)
   │  │
   │  └─ sidebarLayout (TableLayoutPanel, 2 rows)
   │     │
   │     ├─ Row 0 (AutoSize): lblLoadedLogsInfo
   │     │  └─ "Loaded logs: X | Total lines: Y"
   │     │
   │     └─ Row 1 (100%): lstLoadedLogs (ListBox)
   │        └─ Log1, Log2, Log3...
   │
   └─ Panel2: MAIN CONTENT (fills remaining width)
      │
      └─ contentSplit (SplitContainer, Horizontal, Dock=Fill)
         │
         ├─ Panel1: FULL LOG VIEW (45% height)
         │  │
         │  └─ fullLogLayout (TableLayoutPanel, 2 rows)
         │     │
         │     ├─ Row 0 (AutoSize): topButtons (FlowLayoutPanel)
         │     │  ├─ btnLoadFile [Load Log File]
         │     │  ├─ btnPasteLog [Paste Log]
         │     │  └─ btnClearAll [Clear All]
         │     │
         │     └─ Row 1 (100%): dgvFullLog (DataGridView)
         │        └─ Line # | Source | Timestamp | Severity | Raw Text
         │
         └─ Panel2: FILTER + FINDINGS (55% height)
            │
            └─ lowerSplit (SplitContainer, Horizontal, Dock=Fill)
               │
               ├─ Panel1: FILTER BAR (280px height)
               │  │
               │  └─ filterLayout (TableLayoutPanel, 2 rows)
               │     │
               │     ├─ Row 0 (AutoSize): filtersRow (FlowLayoutPanel)
               │     │  ├─ lblKeyword "Keyword Filter:"
               │     │  ├─ txtKeywordFilter [____________]
               │     │  ├─ chkIncludeNonFindings ☑ Include non-finding
               │     │  ├─ lblSeverity "Severity:"
               │     │  ├─ chkCritical ☑ Critical
               │     │  ├─ chkError ☑ Error
               │     │  ├─ chkWarning ☑ Warning
               │     │  └─ chkSuccess ☑ Success
               │     │
               │     └─ Row 1 (100%): summaryPanel (Panel)
               │        └─ dgvKeywordSummary (DataGridView, 260px width)
               │           └─ Keyword | Matches
               │
               └─ Panel2: FINDINGS GRID (fills remaining)
                  │
                  └─ findingsContainer (Panel, Padding=8)
                     └─ dgvFindings (DataGridView, Dock=Fill)
                        └─ Line # | Code | Severity | Title | Line Text
```

## Layout Types Used

### SplitContainer (3 instances)
1. **mainSplit** (root): Vertical (left/right)
   - Panel1: Sidebar
   - Panel2: Main content
   - User-resizable

2. **contentSplit**: Horizontal (top/bottom)
   - Panel1: Full log view
   - Panel2: Filter + findings
   - User-resizable

3. **lowerSplit**: Horizontal (top/bottom)
   - Panel1: Filter bar
   - Panel2: Findings grid
   - User-resizable

### TableLayoutPanel (3 instances)
1. **sidebarLayout**: 2 rows x 1 column
   - Row 0: AutoSize (label)
   - Row 1: 100% (listbox)

2. **fullLogLayout**: 2 rows x 1 column
   - Row 0: AutoSize (buttons)
   - Row 1: 100% (grid)

3. **filterLayout**: 2 rows x 1 column
   - Row 0: AutoSize (filter controls)
   - Row 1: 100% (keyword summary grid)

### FlowLayoutPanel (2 instances)
1. **topButtons**: Horizontal flow
   - Load Log File
   - Paste Log
   - Clear All

2. **filtersRow**: Horizontal flow (wraps)
   - Keyword label + textbox
   - Include checkbox
   - Severity label + checkboxes

### DataGridView (3 instances)
1. **dgvFullLog**: All parsed log lines
2. **dgvKeywordSummary**: Keyword match counts
3. **dgvFindings**: Filtered findings

## Color Scheme
- **WhiteSmoke** (#F5F5F5): Sidebars, filter panel backgrounds
- **White** (#FFFFFF): Grid backgrounds, main content areas
- **LightGray** (#D3D3D3): Grid lines
- **DarkSlateGray** (#2F4F4F): Status text
- **Severity colors**: LightCoral, LightSalmon, LightYellow, LightGreen, LightCyan

## Font Scheme
- **Segoe UI 9F Bold**: Labels, headers
- **Segoe UI 9F**: Controls, checkboxes, buttons
- **Consolas 9F**: Grid content (monospace for logs)
- **Segoe UI 9F Bold**: Grid column headers

## Spacing Standards
- **Padding**: 8px on all TableLayoutPanels
- **Margins**: 6px between FlowLayoutPanel controls
- **SplitterWidth**: 6px on all SplitContainers
- **Button spacing**: 6px right margin
- **Control spacing**: 6px bottom margin for auto-sized rows

## Splitter Distances (Initial)
| Splitter | Initial Distance | Calculation |
|----------|------------------|-------------|
| mainSplit | 260px | Fixed width for sidebar |
| contentSplit | 45% of height | 45% for full log view |
| lowerSplit | 280px | Height for filter panel |

## Control Dimensions

### Fixed Sizes
- lblStatus: Height = 30px
- lblLoadedLogsInfo: Height = 30px (AutoSize=false)
- Buttons: 120x32 (Load), 100x32 (Paste, Clear)
- txtKeywordFilter: Width = 350px
- dgvKeywordSummary: Width = 260px (Dock=Left)

### Dynamic Sizes (Dock=Fill)
- lstLoadedLogs: Fills Row 1 of sidebarLayout
- dgvFullLog: Fills Row 1 of fullLogLayout
- dgvFindings: Fills findingsContainer

### Auto-Size
- All labels in FlowLayoutPanels
- All checkboxes
- FlowLayoutPanels themselves (GrowAndShrink)

## Minimum Sizes
| Component | Minimum Size |
|-----------|--------------|
| Form | 1200x800 |
| mainSplit.Panel1 | 200px width |
| mainSplit.Panel2 | 400px width |
| contentSplit.Panel1 | 150px height |
| contentSplit.Panel2 | 200px height |
| lowerSplit.Panel1 | 120px height |
| lowerSplit.Panel2 | 100px height |

## DataGridView Column Configuration

### dgvFullLog (5 columns)
| Column | Property | Width | AutoSizeMode | Resizable |
|--------|----------|-------|--------------|-----------|
| Line # | GlobalLineNumber | 70 | AllCells | True |
| Source | Source | 120 | AllCells | True |
| Timestamp | Timestamp | 150 | AllCells | True |
| Severity | Severity | 80 | AllCells | True |
| Raw Text | RawText | - | Fill | True |

### dgvKeywordSummary (2 columns)
| Column | Property | Width | AutoSizeMode | Resizable |
|--------|----------|-------|--------------|-----------|
| Keyword | Keyword | - | Fill | True |
| Matches | Matches | 80 | AllCells | True |

### dgvFindings (5 columns)
| Column | Property | Width | AutoSizeMode | Resizable |
|--------|----------|-------|--------------|-----------|
| Line # | GlobalLineNumber | 70 | AllCells | True |
| Code | Code | 100 | AllCells | True |
| Severity | Severity | 80 | AllCells | True |
| Title | Title | 250 | AllCells | True |
| Line Text | LineText | - | Fill | True |

## Resize Behavior Summary

### Width Changes
```
┌──────────┬────────────────────────────┐
│          │                            │
│  Sidebar │      Main Content          │ ← Expands/contracts
│  (260px) │      (fills remaining)     │
│          │                            │
│  Fixed*  │      Dynamic               │
└──────────┴────────────────────────────┘
*User can drag splitter to resize
```

### Height Changes
```
┌─────────────────────────────────────┐
│ Buttons (Auto)                      │ ← Minimal height
├─────────────────────────────────────┤
│                                     │
│ Full Log Grid (45%)                 │ ← Proportional
│                                     │
├─────────────────────────────────────┤
│ Filter Controls (Auto)              │ ← Minimal height
│ Keyword Summary (fills Row 1)       │ ← Expands
├─────────────────────────────────────┤
│                                     │
│ Findings Grid (55%)                 │ ← Proportional
│                                     │
└─────────────────────────────────────┘
```

## Navigation Path to Each Control

### To modify the sidebar:
```csharp
BuildLeftSidebar(mainSplit.Panel1)
  └─ sidebarLayout controls
```

### To modify full log view:
```csharp
BuildMainContent(mainSplit.Panel2)
  └─ BuildFullLogView(contentSplit.Panel1)
      └─ fullLogLayout controls
```

### To modify filter panel:
```csharp
BuildMainContent(mainSplit.Panel2)
  └─ BuildFilterAndFindings(contentSplit.Panel2)
      └─ BuildFilterPanel(lowerSplit.Panel1)
          └─ filterLayout controls
```

### To modify findings grid:
```csharp
BuildMainContent(mainSplit.Panel2)
  └─ BuildFilterAndFindings(contentSplit.Panel2)
      └─ BuildFindingsPanel(lowerSplit.Panel2)
          └─ dgvFindings configuration
```

## Common Customizations

### Change sidebar width:
```csharp
// In InitializeCustomUI, Shown event handler:
mainSplit.SplitterDistance = 300; // New width
```

### Change full log view height ratio:
```csharp
// In BuildMainContent, Shown event handler:
int distance = (int)(contentSplit.Height * 0.50); // 50% instead of 45%
```

### Add more buttons:
```csharp
// In BuildFullLogView, after btnClearAll:
var btnNewButton = new Button {
    Text = "New Action",
    Size = new Size(100, 32),
    Margin = new Padding(0, 0, 6, 0)
};
btnNewButton.Click += BtnNewButton_Click;
topButtons.Controls.Add(btnNewButton);
```

### Add sidebar buttons (Row 2):
```csharp
// In BuildLeftSidebar, add Row 2:
sidebarLayout.RowCount = 3;
sidebarLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
var buttonPanel = new FlowLayoutPanel { 
    Dock = DockStyle.Fill, 
    Padding = new Padding(0, 6, 0, 0) 
};
// Add buttons to buttonPanel
sidebarLayout.Controls.Add(buttonPanel, 0, 2);
```

## Troubleshooting

### Issue: Controls clipped on resize
**Solution**: Ensure parent uses `Dock = Fill` or `AutoSize = true`

### Issue: Empty space on right side
**Solution**: Check that mainSplit has `Dock = Fill` and Panel2 contains content with `Dock = Fill`

### Issue: Splitter won't move
**Solution**: Check `IsSplitterFixed = false` and min sizes don't exceed panel sizes

### Issue: FlowLayoutPanel controls overlap
**Solution**: Ensure `WrapContents = true` and controls have proper `Margin`

### Issue: TableLayoutPanel row not expanding
**Solution**: Check that row is `SizeType.Percent, 100F` and content has `Dock = Fill`
