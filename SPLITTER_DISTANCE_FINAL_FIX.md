# SplitterDistance Startup Error - Final Fix

## Problem
Application crashes at startup with error:
```
SplitterDistance must be between Panel1MinSize and Width - Panel2MinSize
```

## Root Cause
The `Shown` event fires before all controls are fully sized/laid out, causing splitter distance calculations to use invalid dimensions and violate min/max constraints.

## Solution Implemented
Replaced immediate splitter initialization with a **100ms delayed Timer** approach:

### Changes Made

1. **Changed from `Shown` to `Load` event**
   - `Load` fires earlier, allowing us to schedule the timer
   - Timer delays execution until layout is complete

2. **Added `SetSplitterDistanceSafe()` helper method**
   - Validates splitter is not null and has valid dimensions
   - Calculates valid range based on Panel1MinSize, Panel2MinSize, and SplitterWidth
   - Clamps desired distance to valid range
   - Only sets distance if within valid range
   - Try-catch with debug logging for troubleshooting

3. **Timer-based deferred initialization**
   - 100ms delay ensures all controls are fully laid out
   - Timer auto-stops and disposes after first tick
   - Sets all three splitters sequentially with safe helper

### Code Flow

```csharp
Form.Load event
    ↓
Start Timer (100ms)
    ↓
Timer.Tick (after 100ms)
    ↓
Stop & Dispose Timer
    ↓
SetSplitterDistanceSafe(mainSplit, 260)
    ↓
SetSplitterDistanceSafe(contentSplit, 45% of height)
    ↓
SetSplitterDistanceSafe(lowerSplit, min(280, available))
```

### SetSplitterDistanceSafe Logic

```csharp
1. Check if splitter is null or has zero dimensions → return
2. Calculate minDistance = Panel1MinSize
3. Calculate maxDistance = (Width or Height) - Panel2MinSize - SplitterWidth
4. Clamp desiredDistance to [minDistance, maxDistance]
5. Set SplitterDistance if valid
6. Catch any exceptions and log
```

## Why This Works

### Problem with `Shown` Event
- `Shown` fires when form becomes visible
- Controls might not be fully sized yet
- Nested SplitContainers especially problematic
- `Application.DoEvents()` is not reliable

### Solution with Timer
- `Load` event fires during form initialization
- Timer delays execution by 100ms
- Gives WinForms time to complete layout
- All controls have final dimensions
- Safe helper validates all constraints

### Safety Features
1. **Null checks**: Won't crash if splitter not created
2. **Dimension checks**: Won't operate on zero-sized controls
3. **Range validation**: Ensures distance respects min/max
4. **Clamping**: Adjusts invalid distances to valid range
5. **Try-catch**: Graceful fallback on any error
6. **Debug logging**: Helps diagnose issues

## Testing Checklist

After rebuilding:
- [ ] Close any running instance of AutoTriage.Gui
- [ ] Rebuild solution (`dotnet build` or F6 in VS)
- [ ] Run application (F5)
- [ ] Verify application starts without error
- [ ] Verify mainSplit (sidebar) is at ~260px
- [ ] Verify contentSplit (full log vs filter+findings) is at ~45%
- [ ] Verify lowerSplit (filter vs findings) is at ~280px or less
- [ ] Drag each splitter to confirm they're movable
- [ ] Resize window to confirm splitters adjust properly

## Fallback Behavior

If setting any splitter fails:
- Debug message written to output
- Splitter remains at default position (usually 50%)
- Application continues to run normally
- User can manually adjust splitters

## Alternative Approaches Considered

1. **Increase `Shown` delay**: Not reliable, still race condition
2. **Multiple `Application.DoEvents()`**: Blocking, unpredictable
3. **`OnLayout` override**: Too early, similar issues
4. **`OnResize` event**: Fires too often, not appropriate
5. **`SizeChanged` event**: Same as Resize

## Recommended Approach (Implemented)
✅ **Timer with `Load` event**: Most reliable, non-blocking, gives adequate delay

## If Error Persists

If you still see the error after this fix:

1. **Check min sizes**:
   ```csharp
   mainSplit.Panel1MinSize = 200; // Should be 200
   mainSplit.Panel2MinSize = 400; // Should be 400
   ```

2. **Check form size**:
   ```csharp
   this.Size = new Size(1400, 1000);
   this.MinimumSize = new Size(1200, 800);
   ```
   - If form is too small, splitters can't fit

3. **Increase timer delay**:
   ```csharp
   var timer = new Timer { Interval = 200 }; // Try 200ms instead of 100ms
   ```

4. **Check debug output**:
   - Look for "SetSplitterDistanceSafe failed:" messages
   - Will show which splitter and what error

5. **Temporarily disable splitter initialization**:
   - Comment out timer code
   - Run application
   - Manually set splitters
   - Verify controls work otherwise

## Files Modified
- `AutoTriage.Gui/Form1.cs`:
  - Lines 119-163: Replaced `Shown` event handler with `Load` + Timer
  - Added `SetSplitterDistanceSafe()` helper method (after line 164)

## Build Status
✅ Code compiles successfully
⏳ Waiting for user to close running app and rebuild

## Next Steps
1. **Close AutoTriage.Gui** (stop debugging)
2. **Rebuild solution**: `dotnet build` or F6 in Visual Studio
3. **Run application**: F5 or Debug → Start Debugging
4. **Verify**: Application starts without SplitterDistance error
