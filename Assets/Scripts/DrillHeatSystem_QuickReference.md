# Drill Heat System - Quick Reference

## 🔥 Heat Stages

```
0% ═══════════════════════════════ COOL
   └─ No effects, drill is cold
   └─ Color: Gray

25% ═══════════════════════════════ WARMING UP  
   └─ Slight glow begins
   └─ Color: Light Gray

50% ═══════════════════════════════ BURNING ⚠️
   └─ ✓ Burning smell particles START
   └─ Yellowish wisps appear
   └─ Color: Yellow-Orange

70% ═══════════════════════════════ OVERHEATING ⚠️⚠️
   └─ ✓ Smoke particles START
   └─ ✓ Burning smell continues
   └─ Gray smoke appears
   └─ Drill glows orange
   └─ Color: Orange

100% ══════════════════════════════ MAXIMUM HEAT 🔥
   └─ ✓ Heavy smoke
   └─ ✓ Heavy burning smell
   └─ Bright red glow
   └─ Material emission glowing red
   └─ Color: Bright Red
```

## 📊 Visual Effects by Heat Level

| Heat % | Glow Color | Point Light | Emission | Smoke | Burn Smell | Audio |
|--------|-----------|-------------|----------|-------|------------|-------|
| 0-25   | Gray      | Off         | Off      | ✗     | ✗          | ✗     |
| 25-50  | Gray-Yellow | Dim       | Faint    | ✗     | ✗          | ✗     |
| 50-70  | Orange    | Medium      | Medium   | ✗     | ✓          | Faint |
| 70-100 | Red       | Bright      | Strong   | ✓     | ✓          | ✓     |

## 🎮 Gameplay Impact

### Heat Increases When:
- ✓ Trigger is pressed (10-100% pressure)
- ✓ Drill is touching wood
- ✓ Higher pressure = faster heating

### Heat Decreases When:
- ✓ Trigger released
- ✓ Not touching wood
- ✓ Automatic cool-down

## 🎨 Component Setup

```
DummyDrill-Rigged/
├─ SG_TriggerLogic (existing)
├─ DrillHeatSystem (NEW)
└─ Model/
   └─ RotatingHead/
      └─ Drill holder/
         └─ Drillbit/
            ├─ WoodChipsParticle (existing)
            ├─ WoodDustSystem (existing)
            ├─ SmokeParticles (NEW) ← gray smoke
            ├─ BurningSmellParticles (NEW) ← yellow wisps
            └─ DrillBitGlow (auto-created) ← point light
```

## ⚙️ Essential Settings

### For Realistic Effect:
```
Heat Increase Rate: 8
Cool Down Rate: 3
Burning Threshold: 50
Overheating Threshold: 70
```

### For Faster/Dramatic Effect:
```
Heat Increase Rate: 12-15
Cool Down Rate: 2
Burning Threshold: 30
Overheating Threshold: 50
```

### For Slower/Subtle Effect:
```
Heat Increase Rate: 4-6
Cool Down Rate: 5
Burning Threshold: 60
Overheating Threshold: 80
```

## 🎯 Quick Setup Steps

1. **Select** `DummyDrill-Rigged` in Hierarchy
2. **Add Component** → `DrillParticleSetup`
3. **Assign** Drill Bit Tip → Drag `Drillbit` GameObject
4. **Click** three dots → `Setup Particle Systems`
5. **Add Component** → `DrillHeatSystem`
6. **Done!** Test in Play mode

## 🐛 Common Issues

### No particles showing?
- Check "Play On Awake" is OFF on particle systems
- Verify DrillHeatSystem has particle references assigned

### No glow effect?
- Enable "Use Emission Glow" in DrillHeatSystem
- Check drill bit material has emission support

### Particles in wrong location?
- Particles must be children of Drillbit
- Set local position to (0, 0, 0)

### Heat not changing?
- Verify SG_TriggerLogic exists on DummyDrill-Rigged
- Check drill is grabable and touching wood tagged surface

## 🔧 Tweaking Tips

**More smoke:** Increase particle emission rates (20 → 40+)  
**Brighter glow:** Increase Max Glow Intensity (3 → 6)  
**Longer burn:** Increase particle lifetime (3 → 6)  
**Faster heat:** Increase Heat Increase Rate (8 → 12)  
**Stay hot longer:** Decrease Cool Down Rate (3 → 1)

## 📝 Code Access

```csharp
// Get heat percentage from other scripts:
DrillHeatSystem heatSystem = GetComponent<DrillHeatSystem>();
float heatPercent = heatSystem.GetHeatPercentage(); // 0.0 to 1.0

// Check if overheating:
bool overheating = heatSystem.IsOverheating();

// Check if burning:
bool burning = heatSystem.IsBurning();
```

---

**Need help?** Check `DrillHeatSystem_SetupGuide.txt` for detailed instructions!
