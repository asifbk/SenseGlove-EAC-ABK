# 🔥 Drill Heat System - Complete Implementation

## Overview

This system adds three realistic visual effects to your VR drill:
1. **Smoke particles** when drilling for extended periods
2. **Burning wood smell effect** (visual wisps)
3. **Drill bit overheating** with red glow

## 📦 What's Included

### Core Scripts
- `DrillHeatSystem.cs` - Main heat management and effects controller
- `DrillParticleSetup.cs` - Automatic particle system generator
- `DrillHeatVisualizer.cs` - Optional UI visualization (for testing/debugging)

### Editor Tools
- `DrillHeatSystemEditor.cs` - Custom inspector with auto-setup buttons

### Documentation
- `DrillHeatSystem_SetupGuide.txt` - Detailed step-by-step setup guide
- `DrillHeatSystem_QuickReference.md` - Quick reference card
- `README_DrillHeatSystem.md` - This file

## 🚀 Quick Start (5 Minutes)

### Option 1: Automatic Setup (Recommended)

1. **Select** `DummyDrill-Rigged` in your Hierarchy
2. **Add Component** → Search for `DrillParticleSetup`
3. **Drag** the `Drillbit` GameObject to "Drill Bit Tip" field
4. **Right-click** the component → Select "Setup Particle Systems"
5. **Add Component** → Search for `DrillHeatSystem`
6. **Remove** the `DrillParticleSetup` component (no longer needed)
7. **Done!** Press Play to test

### Option 2: Using Editor Buttons

1. **Select** `DummyDrill-Rigged` in your Hierarchy
2. **Add Component** → `DrillHeatSystem`
3. In the Inspector, click **"Auto-Find Drill Bit"** button
4. Add `DrillParticleSetup` component temporarily
5. Click the component menu (⋮) → **"Setup Particle Systems"**
6. Back in `DrillHeatSystem`, click **"Auto-Find Particle Systems"**
7. **Remove** `DrillParticleSetup` component
8. **Done!** Press Play to test

## 🎮 How It Works

### Heat Mechanics

```
Not Drilling → Heat decreases at 3°/second
    ↓
Drilling (Light Pressure) → Heat increases at 4-8°/second
    ↓
Drilling (Full Pressure) → Heat increases at 8-12°/second
    ↓
Stopped Drilling → Automatic cool-down begins
```

### Visual Progression

```
0-50% Heat: Cool/Warming
├─ No particles
├─ Minimal glow
└─ Gray color

50-70% Heat: Burning ⚠️
├─ Burning smell particles (yellow wisps)
├─ Orange glow
└─ Yellow-orange color

70-100% Heat: Overheating 🔥
├─ Burning smell particles (intense)
├─ Smoke particles (gray smoke)
├─ Bright red glow
├─ Material emission glow
└─ Bright red color
```

## 🎨 Visual Effects Details

### 1. Smoke Particles (70%+ heat)
- **Appearance**: Gray smoke rising from drill bit
- **Behavior**: Rises slowly, dissipates over time
- **Intensity**: Increases with heat (10-40 particles/sec)
- **Color**: Dark gray to light gray gradient
- **Purpose**: Shows drill bit is dangerously hot

### 2. Burning Smell Particles (50%+ heat)
- **Appearance**: Yellowish-brown wisps
- **Behavior**: Rises with slight turbulence/noise
- **Intensity**: Increases with heat (5-20 particles/sec)
- **Color**: Orange-yellow to gray gradient
- **Purpose**: Shows wood is burning from friction

### 3. Drill Bit Glow (All heat levels)
- **Point Light**: Illuminates nearby surfaces
  - Cool: Gray, dim
  - Warm: Orange, medium
  - Hot: Red, bright
- **Material Emission**: Drill bit itself glows
  - Uses material's `_EmissionColor` property
  - Intensity scales with heat
- **Purpose**: Shows drill bit temperature visually

## ⚙️ Configuration

### Heat Settings

| Parameter | Default | Range | Description |
|-----------|---------|-------|-------------|
| Max Heat | 100 | 50-200 | Maximum temperature |
| Heat Increase Rate | 8 | 1-20 | °/sec when drilling |
| Cool Down Rate | 3 | 1-10 | °/sec when idle |
| Burning Threshold | 50 | 20-80 | When smell starts |
| Overheating Threshold | 70 | 50-95 | When smoke starts |

### Glow Settings

| Parameter | Default | Description |
|-----------|---------|-------------|
| Cool Color | Gray | 0-25% heat |
| Warm Color | Orange | 25-70% heat |
| Hot Color | Red | 70-100% heat |
| Max Glow Intensity | 3 | Point light brightness |
| Max Emission Intensity | 2 | Material glow strength |

## 🔧 Advanced Customization

### Make It Heat Up Faster
```csharp
heatIncreaseRate = 12-15;
coolDownRate = 2;
burningThreshold = 30;
overheatingThreshold = 50;
```

### Make It More Realistic (Slower)
```csharp
heatIncreaseRate = 4-6;
coolDownRate = 5;
burningThreshold = 60;
overheatingThreshold = 80;
```

### Extreme Effects
```csharp
heatIncreaseRate = 20;
maxGlowIntensity = 8;
maxEmissionIntensity = 5;
// In particle systems: increase emission rates to 60-100
```

## 🎯 Scene Setup

### Hierarchy Structure
```
DummyDrill-Rigged/
├─ Transform
├─ SG_Grabable (existing)
├─ SG_TriggerLogic (existing)
├─ DrillHeatSystem (NEW) ← Main component
├─ AudioSource (optional for heat sounds)
└─ Model/
   └─ RotatingHead/
      └─ Drill holder/
         └─ Drillbit/
            ├─ Transform
            ├─ MeshRenderer ← Referenced by heat system
            ├─ Collider
            ├─ WoodChipsParticle (existing)
            ├─ WoodDustSystem (existing)
            ├─ SmokeParticles (NEW) ← Auto-created
            ├─ BurningSmellParticles (NEW) ← Auto-created
            └─ DrillBitGlow (child) ← Auto-created at runtime
```

## 📊 Inspector Reference

### DrillHeatSystem Component

**Heat Settings:**
- Current Heat: Runtime value (read-only in play mode)
- Max Heat: 100
- Heat Increase Rate: 8
- Cool Down Rate: 3
- Overheating Threshold: 70
- Burning Threshold: 50

**Drill Bit References:**
- Drill Bit Tip: Drillbit GameObject
- Drill Bit Renderer: Drillbit MeshRenderer
- Material Index: 0 (usually)

**Particle Effects:**
- Smoke Particles: SmokeParticles GameObject
- Burning Smell Particles: BurningSmellParticles GameObject

**Glow Effect:**
- Drill Bit Glow: Auto-created (leave empty)
- Cool Color: RGB(0.5, 0.5, 0.5)
- Warm Color: RGB(1, 0.5, 0)
- Hot Color: RGB(1, 0.1, 0)
- Max Glow Intensity: 3

**Material Emission:**
- Use Emission Glow: ✓
- Max Emission Intensity: 2

**Audio:**
- Heat Audio Source: (optional)
- Overheating Sound: (optional)
- Sizzle Sound: (optional)

## 🐛 Troubleshooting

### Problem: No effects appearing
**Solutions:**
- Check that `SG_TriggerLogic` exists on `DummyDrill-Rigged`
- Verify drill is grabable (test grabbing in VR)
- Confirm you're drilling a GameObject tagged "Wood"
- Check heat is actually increasing (view in Inspector during play)

### Problem: Particles not visible
**Solutions:**
- Particle GameObjects must be children of Drillbit
- "Play On Awake" must be unchecked
- Check particle material is assigned (URP/Particles/Unlit)
- Verify Simulation Space is "World"

### Problem: No glow effect
**Solutions:**
- Enable "Use Emission Glow" in DrillHeatSystem
- Verify drill bit material has emission support
- Check Material Index matches drill bit material
- Try increasing Max Glow Intensity and Max Emission Intensity

### Problem: Wrong particle location
**Solutions:**
- Particles must be children of Drillbit
- Set particle local position to (0, 0, 0)
- Check "Drill Bit Tip" reference points to correct GameObject

### Problem: Heat increases too fast/slow
**Solutions:**
- Adjust Heat Increase Rate (lower = slower)
- Adjust Cool Down Rate (higher = faster cooling)
- Check trigger pressure values in SG_TriggerLogic

## 🎓 Code Examples

### Access Heat from Other Scripts
```csharp
DrillHeatSystem heatSystem = GetComponent<DrillHeatSystem>();

// Get current heat percentage (0.0 to 1.0)
float heatPercent = heatSystem.GetHeatPercentage();

// Check states
bool isOverheating = heatSystem.IsOverheating();
bool isBurning = heatSystem.IsBurning();

// Use in your logic
if (heatPercent > 0.8f)
{
    Debug.Log("Drill is very hot!");
}
```

### Create Custom Heat-Based Effects
```csharp
void Update()
{
    DrillHeatSystem heat = drill.GetComponent<DrillHeatSystem>();
    
    // Reduce drill efficiency when hot
    float efficiency = Mathf.Lerp(1f, 0.5f, heat.GetHeatPercentage());
    drillSpeed *= efficiency;
    
    // Trigger haptic feedback when overheating
    if (heat.IsOverheating())
    {
        TriggerHapticPulse(0.8f);
    }
}
```

## 📝 Dependencies

**Required:**
- SenseGlove SDK (SG_TriggerLogic component)
- Unity Particle System module
- Universal Render Pipeline (URP)

**Optional:**
- TextMeshPro (for DrillHeatVisualizer UI)
- Audio clips (for heat sounds)

## 🎉 Features Summary

✅ Automatic heat accumulation when drilling  
✅ Realistic cool-down when idle  
✅ Two-stage particle effects (burning smell + smoke)  
✅ Dynamic glow with color transition (gray → orange → red)  
✅ Material emission for hot metal effect  
✅ Pressure-based heat increase rate  
✅ Easy automatic setup with one click  
✅ Custom inspector with helper buttons  
✅ Fully configurable thresholds and rates  
✅ Optional audio support  
✅ Debug visualization available  

## 🚀 Next Steps

1. ✅ Complete basic setup
2. Test in VR and adjust heat rates to your preference
3. Add audio clips for overheating sounds (optional)
4. Create custom particle materials/textures (optional)
5. Add gameplay mechanics based on heat (e.g., reduced efficiency)
6. Implement battery system that drains faster when hot
7. Add cooling station or water bucket to cool drill manually

## 📞 Support

- Check `DrillHeatSystem_SetupGuide.txt` for detailed instructions
- Check `DrillHeatSystem_QuickReference.md` for quick reference
- Use the custom inspector buttons for auto-setup
- Enable Gizmos in Scene view to see particle bounds

---

**Enjoy your enhanced VR drilling experience with realistic heat effects!** 🔥
