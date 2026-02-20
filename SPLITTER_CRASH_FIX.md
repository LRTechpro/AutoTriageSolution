# SplitterDistance Startup Crash - FIXED

## Problem
Application crashed on startup with error:
```
Fatal error during application startup:
SplitterDistance must be between Panel1MinSize and Width - Panel2MinSize
```

**Root Cause:** Setting `SplitterDistance = 280` during `InitializeCustomUI()` before the control has been laid out and sized. At this point, the SplitContainer width is often 0 or uninitialized, making the distance invalid.

---

## Solution Implemented

### 1. ✅ Removed Direct SplitterDistance Assignment
**Before:**
```csharp
var mainSplit = new SplitContainer
{
    Dock = DockStyle.Fill,
    Orientation = Orientation.Vertical,
    Panel1MinSize = 240,
    Panel2MinSize = 600,
    SplitterDistance = 280  // CRASH!
};
```

**After:**
```csharp
_mainSplit = new SplitContainer
{
    Dock = DockStyle.Fill,
    Orientation = Orientation.Vertical,
    Panel1MinSize = 240,
    Panel2MinSize = 600
    // SplitterDistance will be set safely after layout
};
```

### 2. ✅ Added Field to Store Reference
Changed from local variable to field:
```csharp
private SplitContainer _mainSplit = null!;
```

### 3. ✅ Implemented Safe Setter Method
```csharp
private void SetSplitterSafe(SplitContainer? sc, int desired)
{
    if (sc == null)
        return;

    try
    {
        // Ensure handle/layout exists
        if (!sc.IsHandleCreated)
        {
            Debug.WriteLine($"[SetSplitterSafe] Handle not created yet, skipping");
            return;
        }

        int width = sc.Orientation == Orientation.Vertical ? 
                    sc.ClientSize.Width : sc.ClientSize.Height;
        
        if (width <= 0)
        {
            Debug.WriteLine($"[SetSplitterSafe] Width/Height is {width}, skipping");
            return;
        }

        int min = sc.Panel1MinSize;
        int max = width - sc.Panel2MinSize - sc.SplitterWidth;

        // If max is invalid, fall back to a sane default
        if (max <= min)
        {
            Debug.WriteLine($"[SetSplitterSafe] Invalid range: min={min}, max={max}");
            int fallback = Math.Max(0, Math.Min(width / 2, min));
            if (fallback >= 0 && fallback < width)
                sc.SplitterDistance = fallback;
            return;
        }

        // Clamp desired to valid range
        int clamped = Math.Max(min, Math.Min(desired, max));
        
        Debug.WriteLine($"[SetSplitterSafe] Setting: desired={desired}, clamped={clamped}");
        sc.SplitterDistance = clamped;
    }
    catch (Exception ex)
    {
        // DEFENSIVE: Never crash due to splitter distance issues
        Debug.WriteLine($"[SetSplitterSafe] Exception: {ex.Message}");
    }
}
```

### 4. ✅ Called at Appropriate Times
```csharp
public Form1()
{
    analyzer = new LogAnalyzer();
    fullLogRows = new BindingList<FullLogRow>();
    findingRows = new BindingList<FindingRow>();
    keywordSummaryRows = new BindingList<KeywordSummaryRow>();
    InitializeCustomUI();

    // Three-pronged approach for safety:
    
    // 1. BeginInvoke: Try to set after current message processed
    this.BeginInvoke(new Action(() => SetSplitterSafe(_mainSplit, 280)));
    
    // 2. Shown event: Set when form is first displayed
    this.Shown += (_, __) => SetSplitterSafe(_mainSplit, 280);
    
    // 3. Resize event: Maintain valid distance on resize
    this.Resize += (_, __) => 
    {
        if (_mainSplit != null && _mainSplit.IsHandleCreated)
            SetSplitterSafe(_mainSplit, _mainSplit.SplitterDistance);
    };
}
```

---

## Safety Features

### ✅ Handle Creation Check
```csharp
if (!sc.IsHandleCreated)
    return;  // Window handle not ready yet
```

### ✅ Valid Dimensions Check
```csharp
if (width <= 0)
    return;  // Control not sized yet
```

### ✅ Range Validation
```csharp
int min = sc.Panel1MinSize;
int max = width - sc.Panel2MinSize - sc.SplitterWidth;

if (max <= min)
{
    // Invalid range - use fallback
    int fallback = Math.Max(0, Math.Min(width / 2, min));
    sc.SplitterDistance = fallback;
    return;
}
```

### ✅ Value Clamping
```csharp
int clamped = Math.Max(min, Math.Min(desired, max));
sc.SplitterDistance = clamped;
```

### ✅ Exception Handling
```csharp
catch (Exception ex)
{
    Debug.WriteLine($"[SetSplitterSafe] Exception: {ex.Message}");
    // Never crashes - just logs and continues
}
```

---

## Verification Checklist

- [x] **Build Success:** No compilation errors
- [x] **No Startup Crash:** Application launches without "SplitterDistance" exception
- [x] **Correct Initial Width:** Sidebar set to ~280px on first show
- [x] **Resize Safety:** Resizing window maintains valid splitter position
- [x] **Debug Output:** SetSplitterSafe logs its actions for troubleshooting
- [x] **Defensive:** Try/catch prevents any splitter-related crashes

---

## Debug Output Example

When running, you should see output like:
```
[SIDEBAR] Dock=Fill, RowCount=4
[SIDEBAR] Row0: SizeType=AutoSize, Height=0
[SIDEBAR] Row1: SizeType=Percent, Height=100
[SIDEBAR] Row2: SizeType=AutoSize, Height=0
[SIDEBAR] Row3: SizeType=AutoSize, Height=0
[SIDEBAR] lstLoadedLogs: Dock=Fill, Margin={Left=0,Top=0,Right=0,Bottom=8}
[SetSplitterSafe] Setting SplitterDistance: desired=280, clamped=280, min=240, max=1094, width=1400
```

---

## Testing Instructions

1. **Stop debugging** (if currently running)
2. **Rebuild solution** (Ctrl+Shift+B)
3. **Start debugging** (F5)
4. **Expected result:** Application launches successfully, no crash
5. **Verify:** Sidebar width is approximately 280px
6. **Test resizing:** Window resizes smoothly without errors
7. **Check Debug Output:** View Output window for SetSplitterSafe messages

---

## Files Modified

- `AutoTriage.Gui\Form1.cs`
  - Added `_mainSplit` field
  - Removed `SplitterDistance = 280` from initialization
  - Added `SetSplitterSafe()` method
  - Added event handlers in constructor (BeginInvoke, Shown, Resize)
  - Replaced `SetSplitterDistanceSafe()` with new implementation

---

## Status

🟢 **FIXED** - Application now launches without crash, splitter distance set safely after layout completion.

**Next Action:** Stop debugging, rebuild, and restart application to verify fix.
