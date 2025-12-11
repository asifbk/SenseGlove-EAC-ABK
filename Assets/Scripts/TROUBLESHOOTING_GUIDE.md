# Drill System Troubleshooting Guide

## Quick Setup to Enable Debugging

### Step 1: Enable Debug Logs in Inspector

1. Select `/DummyDrill-Rigged` in the hierarchy
2. Find the `DrillHeatSystem` component
3. **Check the box**: `Enable Debug Logs`
4. This will show detailed information about:
   - Drill bit changes
   - Material property detection
   - Heat color updates
   - Finger lock state changes

### Step 2: Add Debug Helper (Optional)

1. Select `/DummyDrill-Rigged` in the hierarchy
2. Click `Add Component` → search for `DrillDebugHelper`
3. Drag the `DrillHeatSystem` component into the `Heat System` field
4. Expand `Drill Bits` array and set size to 4
5. Drag each drill bit into the array:
   - Element 0: `/3mm drill bit`
   - Element 1: `/5mm drill bit`
   - Element 2: `/8mm drill bit`
   - Element 3: `/10mm drill bit`

6. **During Play Mode**, press keyboard keys:
   - Press `D` = Debug all drill bit properties (colliders, materials, scales)
   - Press `M` = Debug current drill bit material state
   - Or enable `Apply Test Heat` and adjust `Test Heat` slider to manually test colors

---

## Issue 1: Heat Color Not Showing on Swapped Drill Bits

### What SHOULD Happen:
1. Start with default drill bit attached to drill
2. Enable manual heat control in Inspector
3. Set manual heat to 70°C (above warm threshold of 50°C)
4. Drill bit should turn **orange/red**
5. Swap to a different drill bit (e.g., remove current, attach 5mm bit)
6. New drill bit should **IMMEDIATELY** turn **orange/red** (same heat state)

### Diagnosis Steps:

#### A) Check if drill bit swap is detected:
**Look in Console for:**
```
✓ New drill bit detected: [BitName]
  Shader: [ShaderName]
  Has _EmissionColor: True/False
  Has _Color: True/False
  Original Color: [Color]
```

**Expected Results:**
- `Has _Color: True` (Steel 1.mat uses `_Color` property)
- `Has _EmissionColor: False` (WorldMatURPFree shader doesn't support emission)

**If you DON'T see this message:**
- The drill bit change was NOT detected by DrillHeatSystem
- Check that `SG_TriggerLogic.autoFindActiveDrillBit` is enabled
- Check that `DrillHeatSystem.drillBitTip` updates when you swap bits

#### B) Check if color is being applied:
**Look in Console for** (appears every 60 frames when heat > 50°C):
```
[Heat Visual] Setting _Color to [Color] (Heat: XX°C)
```

**If you DON'T see this message:**
- Either heat is below 50°C
- Or `useColorOverride` is disabled
- Or `hasColorProperty` is false

#### C) Verify material property:
**Press `M` key during play mode**

Expected console output:
```
=== CURRENT DRILL BIT MATERIAL ===
Drill Bit: [Name]
Heat: [Temperature]
Shader: Shader Graphs/WorldMatURPFree
Has _Color: True
_Color value: RGBA([values])
```

**If `Has _Color: False`:**
- The material doesn't have the `_Color` property
- Try changing `colorPropertyName` in Inspector to `_BaseColor` or another property
- Use the Shader Inspector to find available properties

#### D) Common Causes:

| Symptom | Cause | Solution |
|---------|-------|----------|
| No color change at all | `useColorOverride` is disabled | Enable it in Inspector |
| Color changes on original bit but not swapped bits | Material instance not updating | Already fixed in latest code |
| Drill bit turns black | Original color is black | Check material's `_Color` value |
| Changes visible in Scene view but not Game view | Rendering issue | Check camera settings, lighting |

---

## Issue 2: Carving Not Respecting Drill Bit Size

### What SHOULD Happen:
1. Attach 3mm drill bit to drill
2. Drill into wood
3. Hole should be approximately **3mm diameter**
4. Swap to 10mm drill bit
5. Drill into wood
6. Hole should be approximately **10mm diameter** (3.3x larger than 3mm)

### Diagnosis Steps:

#### A) Check if drill bit reference updates:
**Look in Console for:**
```
✓ Drill bit updated: [BitName]
  Collider radius: 0.1331m
  Scale: X=0.XXXX, Y=0.XXXX, Z=0.0312
  Effective radius: 0.00XXXX m (X.XXmm)
  Hole diameter will be: XX.XXmm
```

**Expected drill diameters:**
- 3mm bit: Diameter ≈ **9.3mm** (0.035 scale)
- 5mm bit: Diameter ≈ **13.3mm** (0.050 scale)
- 8mm bit: Diameter ≈ **21.3mm** (0.080 scale)
- 10mm bit: Diameter ≈ **26.6mm** (0.100 scale)

**Note:** The effective diameter is larger than the name suggests because:
- Collider radius (0.1331m) × scale gives the effective radius
- This is by design - the drill bits are scaled models

**If you DON'T see this message:**
- `SetDrillBit()` is not being called
- Check `SG_TriggerLogic.HandleCarving()` is calling `carvable.SetDrillBit(drillTip)`
- Verify `drillTip` is not null

#### B) Verify the calculation:
**Press `D` key during play mode** to see all drill bit properties

Expected output for each drill bit shows:
```
[BitName]:
  Scale: X=0.XXXX, ...
  Collider Radius: 0.1331m
  Effective Diameter: XX.XXmm
```

Verify that **different bits have different effective diameters**.

#### C) Test actual carving:
1. Enable debug logs
2. Attach 3mm bit
3. Drill into Wood object
4. Watch console - should see the drill bit update message
5. Observe hole size
6. Swap to 10mm bit
7. Drill into a different part of wood
8. Should see new drill bit update message
9. Hole should be visibly larger

#### D) Common Causes:

| Symptom | Cause | Solution |
|---------|-------|----------|
| All holes are same size | Drill bit reference not updating | Check SetDrillBit() is called |
| No holes at all | No raycast hit or carving disabled | Check SG_TriggerLogic raycast settings |
| Holes too large/small | Scale calculation wrong | Verify collider direction matches code |
| Silent failure | Missing warnings | Already added debug warnings in latest code |

---

## Expected Console Output (Normal Operation)

### When you swap from 3mm to 5mm drill bit:

```
✓ Switched to SCENE drill bit: 5mm drill bit
Detected drill size: 5mm drill bit | Radius: 0.0067m (13.3mm diameter)

✓ New drill bit detected: 5mm drill bit
  Shader: Shader Graphs/WorldMatURPFree
  Has _EmissionColor: False
  Has _Color: True
  Original Color: RGBA(0.544, 0.544, 0.544, 1.000)

✓ Drill bit updated: 5mm drill bit
  Collider radius: 0.1331m
  Scale: X=0.0500, Y=0.0500, Z=0.0312
  Effective radius: 0.006656m (6.66mm)
  Hole diameter will be: 13.31mm
```

### When drilling with heat at 75°C:

```
[Heat Visual] Setting _Color to RGBA(1.0, 0.3, 0.0, 1.0) (Heat: 75.0°C)
[DrillHeatSystem] Pressure: 0.85, isDrilling: True, Heat: 75.0, SafetyLocked: False, FingerLocked: False
```

---

## Quick Fixes

### Fix 1: If material color isn't changing
```
1. Select /DummyDrill-Rigged
2. In DrillHeatSystem component:
   - Enable "Use Color Override"
   - Set "Color Property Name" to "_Color"
   - Enable "Enable Debug Logs"
3. Set manual heat to 80°C
4. Watch console for color update messages
```

### Fix 2: If carving size isn't changing
```
1. Open SG_TriggerLogic.cs
2. Find HandleCarving() method around line 272
3. Verify this line exists:
   carvable.SetDrillBit(drillTip);
4. If missing, it should be called BEFORE CarveAtPosition()
```

### Fix 3: If nothing works
```
1. Enable debug logs on DrillHeatSystem
2. Enter Play mode
3. Swap drill bits manually
4. Take screenshot of console output
5. This will show exactly what's happening
```

---

## Known Limitations

1. **Drill bit sizes don't match names exactly** because they use a shared collider (radius 0.1331m) scaled by transform scale. The ratios between sizes are correct.

2. **Material changes require material instances** - the code creates instances automatically via `renderer.materials`, but shared materials won't show per-object colors.

3. **Color changes only visible when heat > 50°C** by default. Adjust `warmColorThreshold` to change this.

4. **Carving depth** is calculated from drill tip Y-position. If the drill bit's origin isn't at the tip, depth may be incorrect.

---

## Success Criteria

### Issue 1 Fixed ✓ When:
- [ ] Manual heat set to 75°C makes drill bit turn orange
- [ ] Swapping to new drill bit keeps it orange (doesn't reset to gray)
- [ ] Console shows "Setting _Color to [orange color]" messages
- [ ] Color visible in both Scene and Game view

### Issue 2 Fixed ✓ When:
- [ ] 3mm bit creates small holes
- [ ] 10mm bit creates holes ~2.8x larger diameter
- [ ] Console shows different "Hole diameter will be: XX.XXmm" for each bit
- [ ] Visual difference clearly visible when comparing holes

---

## Manual Testing Checklist

```
[ ] Enable Debug Logs on DrillHeatSystem
[ ] Enter Play Mode
[ ] Press D key - verify all 4 drill bits show different effective diameters
[ ] Enable Manual Heat Control
[ ] Set Manual Heat Value to 75
[ ] Grab drill
[ ] Verify current drill bit turns orange (check in Scene view)
[ ] Detach current drill bit
[ ] Attach different drill bit (e.g., 5mm)
[ ] Verify new drill bit IMMEDIATELY turns orange
[ ] Console shows: "✓ New drill bit detected: 5mm drill bit"
[ ] Console shows: "✓ Drill bit updated: 5mm drill bit"
[ ] Drill into wood with 3mm bit
[ ] Note hole size
[ ] Swap to 10mm bit
[ ] Drill into wood
[ ] Verify hole is significantly larger
[ ] Console shows different diameter values for each bit
```

If ALL checkboxes pass → Both issues are fixed!
If ANY checkbox fails → Check corresponding diagnosis section above.
