# SenseGlove Finger Lock Troubleshooting Guide

## Issue: Index Finger Not Locking at High Heat

### What SHOULD Happen
When heat reaches 90°C (default `hotColorThreshold`):
1. Index finger should feel strong resistance
2. Finger should "lock" in place, preventing further flexion
3. You should feel haptic force feedback pushing back against your finger
4. Debug console should show: `🔒 INDEX FINGER LOCKED!`

When heat drops below 90°C:
1. Index finger resistance should release
2. Finger can flex freely again
3. Console should show: `🔓 INDEX FINGER RELEASED!`

---

## Critical Setup Steps

### Step 1: Enable Debug Logs
1. Select `/DummyDrill-Rigged` in Hierarchy
2. In `DrillHeatSystem` component:
   - ✓ Check `Enable Debug Logs`
   - ✓ Check `Enable Finger Lock`
   - Verify `Index Finger Lock Force` is set to **100** (maximum)
   - Set `Hot Color Threshold` to **90** (default)

### Step 2: Setup Manual Heat Testing
1. In `DrillHeatSystem` component:
   - ✓ Check `Enable Manual Heat Control`
   - Set `Manual Heat Value` to **95** (above threshold)

### Step 3: Assign Tracked Hand Reference
1. **CRITICAL**: Verify `Tracked Hand` field is assigned:
   - It should reference `/[CameraRig]/[SG_User] - MODIFIED/SGHand Right`
   - If empty, drag the `SGHand Right` GameObject into this field

### Step 4: Verify Glove Connection
1. Enter Play Mode
2. Open Console window
3. Look for initialization message:
   ```
   [DrillHeatSystem] Initialized. TrackedHand: True, HapticGlove: True
   ```

**If you see `TrackedHand: False` or `HapticGlove: False`:**
- The hand reference is not assigned correctly
- Fix: Assign `SGHand Right` to `Tracked Hand` field in Inspector

---

## Diagnostic Console Output

### When Everything Works Correctly

#### On Play Mode Start:
```
[DrillHeatSystem] Initialized. TrackedHand: True, HapticGlove: True
```

#### When Heat Reaches 90°C (every second):
```
🔒 INDEX FINGER LOCKED! Heat: 95.0°C >= 90.0°C. Force: 100%
[Finger Lock] Sending FFB - Index: 1.00 (Force: 100%)
[Finger Lock] Sending FFB - Index: 1.00 (Force: 100%)
[Finger Lock] Sending FFB - Index: 1.00 (Force: 100%)
```

**Key Points:**
- `🔒 INDEX FINGER LOCKED!` appears ONCE when crossing threshold
- `[Finger Lock] Sending FFB` appears **EVERY SECOND** (60 frames)
- This confirms FFB is being sent continuously

#### When Heat Drops Below 90°C:
```
🔓 INDEX FINGER RELEASED! Heat: 85.0°C < 90.0°C
```

### When Things Go Wrong

#### Problem 1: No lock messages at all

**Console shows:**
```
⚠️ Internal glove is null! Cannot apply finger lock.
```

**Cause:** No glove reference assigned  
**Fix:**
1. Assign `SGHand Right` to `Tracked Hand` field
2. Restart Play Mode

---

#### Problem 2: Glove not connected

**Console shows:**
```
⚠️ Glove not connected! Cannot apply finger lock.
```

**Cause:** SenseGlove hardware not detected  
**Fix:**
1. Check glove is powered on
2. Check Bluetooth connection
3. Open SenseGlove Control Panel to verify connection
4. Ensure SenseGlove is paired in Windows Bluetooth settings

---

#### Problem 3: Lock message appears but no haptic feedback

**Console shows:**
```
🔒 INDEX FINGER LOCKED! Heat: 95.0°C >= 90.0°C. Force: 100%
[Finger Lock] Sending FFB - Index: 1.00 (Force: 100%)
```

**But you don't feel resistance.**

**Possible Causes:**

**A) Glove calibration issue**
- Try recalibrating the glove
- Check SenseGlove Control Panel calibration status

**B) Index finger motor not working**
- Test other fingers to verify FFB works
- Try setting all fingers to lock:
  ```csharp
  // Temporary test: modify UpdateFingerLock()
  float[] ffb = new float[5];
  ffb[0] = 1.0f; // Thumb
  ffb[1] = 1.0f; // Index
  ffb[2] = 1.0f; // Middle
  ffb[3] = 1.0f; // Ring
  ffb[4] = 0.0f; // Pinky (Nova 2 doesn't have FFB)
  ```
- If other fingers work but index doesn't, hardware issue

**C) Force level too low**
- Try increasing `Index Finger Lock Force` to **100**
- Verify console shows `Index: 1.00` not `Index: 0.00`

**D) Finger already fully extended**
- FFB only resists flexion (curling)
- Try curling finger while heat is high
- Lock should prevent further curling

**E) Bluetooth bandwidth issue**
- Too many haptic commands can cause drops
- Check for connection stability warnings
- Reduce other haptic effects running simultaneously

---

#### Problem 4: Lock works initially then stops

**Symptoms:**
- First few seconds: feels resistance
- Then: resistance disappears

**Cause:** FFB not being sent continuously  
**Status:** **FIXED** in latest code update
- Old code: sent FFB once on state change
- New code: sends FFB every frame while locked

**Verify fix:**
Look for `[Finger Lock] Sending FFB` appearing **every second** in console (60 frames interval)

If you don't see continuous messages:
- Code not updated correctly
- Verify `UpdateFingerLock()` contains the continuous send loop

---

## Testing Procedure

### Test 1: Basic Finger Lock

```
1. Enable Debug Logs
2. Set Manual Heat Control = ON
3. Set Manual Heat Value = 95
4. Grab drill with right hand
5. Enter Play Mode
6. Wait 2 seconds
7. Check console for:
   "🔒 INDEX FINGER LOCKED!"
   "[Finger Lock] Sending FFB" (repeating)
8. Try to curl index finger
9. Should feel strong resistance
```

**Expected:** Cannot fully curl index finger

### Test 2: Lock Release

```
1. With finger locked (heat at 95)
2. Set Manual Heat Value = 85
3. Check console for:
   "🔓 INDEX FINGER RELEASED!"
4. Try to curl index finger
5. Should move freely
```

**Expected:** Finger flexes normally

### Test 3: Threshold Boundary

```
1. Set Manual Heat Value = 89 (just below 90)
2. Finger should be free
3. Set Manual Heat Value = 90 (exactly at threshold)
4. Should lock immediately
5. Console: "🔒 INDEX FINGER LOCKED!"
```

**Expected:** Lock activates exactly at 90°C

### Test 4: Force Level Testing

```
1. Set Manual Heat Value = 95
2. Set Index Finger Lock Force = 25
3. Try to curl finger
4. Should feel slight resistance
5. Set Index Finger Lock Force = 100
6. Try to curl finger
7. Should feel much stronger resistance
```

**Expected:** Higher force = stronger resistance

---

## Advanced Debugging

### Enable Full Haptic Logging

Add this to `UpdateFingerLock()` for maximum detail:

```csharp
if (isFingerLocked)
{
    float[] ffb = new float[5];
    ffb[1] = indexFingerLockForce / 100f;
    internalGlove.QueueFFBLevels(ffb);
    internalGlove.SendHaptics();
    
    // FULL DEBUG (every frame)
    Debug.Log($"[FFB] Frame {Time.frameCount}: " +
              $"Heat={currentHeat:F1}, " +
              $"Threshold={hotColorThreshold}, " +
              $"Locked={isFingerLocked}, " +
              $"FFB={ffb[1]:F2}, " +
              $"Connected={internalGlove.IsConnected()}");
}
```

**Warning:** This will spam console with hundreds of messages per second. Use only for deep diagnosis.

---

### Check Glove Type and Firmware

Different SenseGlove models have different capabilities:

| Model | Index FFB | Firmware Check |
|-------|-----------|----------------|
| Nova 1 | ✓ Yes | Open SenseGlove Control Panel |
| Nova 2 | ✓ Yes | Check firmware version |
| DK1 | ✓ Yes | May need firmware update |

**To check:**
1. Open SenseGlove Control Panel application
2. Connect glove
3. View device info panel
4. Note model and firmware version
5. Check SenseGlove website for latest firmware

---

### Verify FFB Array Indices

The FFB array uses this mapping:
```
Index 0 = Thumb
Index 1 = Index finger ← YOUR LOCK
Index 2 = Middle finger
Index 3 = Ring finger
Index 4 = Pinky finger
```

**Verify your code uses `ffb[1]` for index finger.**

Current code:
```csharp
float[] ffb = new float[5];
ffb[1] = indexFingerLockForce / 100f;  // ✓ Correct
```

**WRONG examples to avoid:**
```csharp
ffb[0] = ...  // ✗ This is THUMB, not index!
ffb[2] = ...  // ✗ This is MIDDLE finger!
```

---

## Common Mistakes Checklist

- [ ] `Enable Finger Lock` is checked
- [ ] `Index Finger Lock Force` is set to 100 (not 0)
- [ ] `Tracked Hand` field is assigned to `SGHand Right`
- [ ] `Hot Color Threshold` is set to 90 or lower
- [ ] Heat is actually >= 90 (check `Manual Heat Value`)
- [ ] Glove is powered on and connected
- [ ] Glove is calibrated
- [ ] Trying to curl finger (not extend) - FFB only resists curling
- [ ] Index finger motor is working (test with SenseGlove Control Panel)
- [ ] Bluetooth connection is stable

---

## Still Not Working?

### Fallback Test: Lock All Fingers

Modify `UpdateFingerLock()` temporarily:

```csharp
if (isFingerLocked)
{
    float[] ffb = new float[5];
    ffb[0] = 1.0f; // Thumb
    ffb[1] = 1.0f; // Index
    ffb[2] = 1.0f; // Middle
    ffb[3] = 1.0f; // Ring
    ffb[4] = 0.0f; // Pinky (no FFB on Nova)
    
    internalGlove.QueueFFBLevels(ffb);
    internalGlove.SendHaptics();
    
    Debug.Log($"[FULL LOCK TEST] All fingers locked!");
}
```

**If this works:**
- FFB system is working
- Issue is with array index or force level
- Revert to `ffb[1]` only

**If this doesn't work:**
- Check glove connection
- Check glove calibration
- Check hardware (try SenseGlove examples)
- Contact SenseGlove support

---

## Technical Notes

### How Force Feedback Works

**Force Feedback (FFB)** on SenseGlove:
- Uses small motors in each finger
- Motors pull brake wires to resist flexion
- Value range: 0.0 (no force) to 1.0 (maximum force)
- Must be sent **continuously** every frame to maintain force
- SenseGlove SDK optimizes bandwidth automatically

### Why Continuous Sending is Required

From SenseGlove documentation:
> "For Force Feedback (FFB), commands are collected in a queue and sent out at the end of each frame. To maintain a continuous force, you would typically call the FFB function every frame with the desired force level."

**This means:**
- ✓ Call `QueueFFBLevels()` and `SendHaptics()` every frame
- ✗ Don't call just once on state change
- ✓ SDK handles optimization to avoid redundant sends
- ✓ Safe to call every frame without bandwidth issues

---

## What Changed in Latest Update

**OLD CODE (BROKEN):**
```csharp
void UpdateFingerLock()
{
    bool shouldLock = currentHeat >= hotColorThreshold;
    
    if (shouldLock && !isFingerLocked)
    {
        // Send FFB ONCE when locking
        isFingerLocked = true;
        float[] ffb = new float[5];
        ffb[1] = indexFingerLockForce / 100f;
        internalGlove.QueueFFBLevels(ffb);
        internalGlove.SendHaptics();
    }
    // ✗ No continuous sending!
}
```

**NEW CODE (FIXED):**
```csharp
void UpdateFingerLock()
{
    bool shouldLock = currentHeat >= hotColorThreshold;
    
    // Track state changes
    if (shouldLock && !isFingerLocked)
    {
        isFingerLocked = true;
        Debug.Log("🔒 INDEX FINGER LOCKED!");
    }
    
    // ✓ Send FFB EVERY FRAME while locked
    if (isFingerLocked)
    {
        float[] ffb = new float[5];
        ffb[1] = indexFingerLockForce / 100f;
        internalGlove.QueueFFBLevels(ffb);
        internalGlove.SendHaptics();
    }
    else
    {
        // Release when unlocked
        float[] ffb = new float[5];
        internalGlove.QueueFFBLevels(ffb);
        internalGlove.SendHaptics();
    }
}
```

**Key difference:** FFB is now sent **every frame** in Update(), not just once.

---

## Next Steps

1. **Enable debug logs** in DrillHeatSystem component
2. **Set manual heat to 95°C**
3. **Enter Play Mode**
4. **Check console output** - should see continuous FFB messages
5. **Test finger resistance** - try to curl index finger
6. **If still not working** - share console output for further diagnosis

The debug messages will tell us exactly what's happening:
- Is glove connected? ✓
- Is lock activating? ✓
- Is FFB being sent? ✓
- What force level is being sent? ✓

This will pinpoint the exact issue.
