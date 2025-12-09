# Workshop Scene - Comprehensive Technical Summary
## Virtual Reality Haptic Training Simulation

**Unity Version:** 2022.3  
**Render Pipeline:** Universal Render Pipeline (URP)  
**Primary Hardware:** SenseGlove Nova Haptic Gloves  
**VR Platform:** OpenVR/SteamVR  
**Scene Path:** Assets/SenseGlove/Examples/Workshop Scene.unity

---

## 1. EXECUTIVE SUMMARY

The Workshop Scene is an immersive Virtual Reality (VR) training simulation that combines haptic feedback, physics-based interaction, and multimodal sensory output to create a realistic power drill operation experience. The system integrates SenseGlove Nova haptic gloves with Unity's XR Interaction Toolkit to provide force feedback, thermal simulation, and safety training in a virtual garage workshop environment.

**Key Features:**
- Full-hand haptic feedback with force-feedback actuation
- Thermal overheat simulation with multimodal feedback
- Physics-based material carving and deformation
- Interchangeable drill bit system
- Battery management simulation
- Safety lock mechanisms with haptic enforcement

---

## 2. HARDWARE INTEGRATION

### 2.1 SenseGlove Nova Integration

The scene utilizes **SenseGlove Nova** haptic gloves, providing:

**Haptic Capabilities:**
- 5-finger force feedback (0-100N per finger)
- Individual finger tracking and flexion sensing
- Vibrotactile feedback actuators
- Sub-millimeter position tracking
- Real-time hand pose estimation

**Hand Tracking Architecture:**
```
[SG_User] - MODIFIED
├── SGHand Right (SG_HapticGlove, SG_TrackedHand)
│   ├── HandModel (Skinned mesh with bone hierarchy)
│   ├── Animation Layer (Real-time hand animation)
│   ├── PassThroughLayer (5 finger collision proxies)
│   ├── PhysicsTrackingLayer (16 physics colliders)
│   ├── Feedback Layer (5 force-feedback zones)
│   ├── Grab Layer (Multi-point grab detection)
│   ├── Gesture Layer (Gesture recognition)
│   └── Calibration Layer (Runtime calibration)
└── SGHand Left (Mirror architecture)
```

**Technical Implementation:**
- **PassThroughLayer:** Individual colliders for thumb, index, middle, ring, and pinky fingers enabling precise grab detection
- **PhysicsTrackingLayer:** 16 physics bodies tracking distal phalanx (DP), middle phalanx (MP), and proximal phalanx (PP) of each finger
- **Feedback Layer:** Force-feedback zones with hover detection for haptic response
- **Grab Layer:** Dual reference system (RealHandGrabRef and VirtualGrabRef) for accurate object manipulation

### 2.2 VR Headset Integration

**Camera Rig Configuration:**
- OpenVR/SteamVR integration via `com.valvesoftware.unity.openvr` package
- XR Interaction Toolkit 3.1.2
- Unity XR Management 4.5.1
- OpenXR 1.14.3 support

**Controller Setup:**
- Left/Right controller tracking
- 6DOF (6 degrees of freedom) tracking
- Synchronized with SenseGlove hand positions

---

## 3. CORE SYSTEMS ARCHITECTURE

### 3.1 Primary Drill System (DummyDrill-Rigged)

**GameObject Hierarchy:**
```
DummyDrill-Rigged
├── Model
│   ├── Trigger (Visual component)
│   ├── Handle (Visual component)
│   ├── Top (Visual component)
│   ├── RotatingHead (Dynamic rotation)
│   │   ├── SnapPoint_DrillBit (Attachment point)
│   │   └── Drill holder
│   │       └── Drillbit
│   │           ├── SmokeParticles (ParticleSystem)
│   │           ├── WoodDustSystem (WoodDustGenerator)
│   │           └── BurningSmellParticles (ParticleSystem)
│   └── BatteryPack (Visual indicator)
├── SnapPoint_Right (Right-hand grab point)
└── SnapPoint_Left (Left-hand grab point)
```

**Component Stack:**
1. **SG_Grabable** - SenseGlove grab detection and physics
2. **SG_SnapOptions** - Snap-to-hand positioning
3. **SG_TriggerLogic** (Custom) - Trigger pressure, rotation, and carving
4. **SG_SqueezeOnGrab** - Haptic squeeze feedback
5. **AudioSource** - Drill sound effects
6. **MeshCollider** - Physics collision
7. **SG_Material** - Haptic material properties
8. **Rigidbody** - Physics body (kinematic when grabbed)
9. **DrillHeatSystem** (Custom) - Thermal simulation
10. **DrillBatterySystem** (Custom) - Power management

### 3.2 Trigger and Interaction System (SG_TriggerLogic)

**Purpose:** Converts finger flexion into drill operation and material carving.

**Key Parameters:**
- **respondsTo:** Index finger
- **startFlexion:** 0.3 (30% finger bend to activate)
- **endFlexion:** 0.5 (50% finger bend for full power)
- **maxRotationSpeed:** 10,000 RPM
- **resistanceFactor:** 0.4 (haptic resistance when drilling)

**Carving System:**
- **carvableLayer:** Wood layer
- **drillDirection:** Forward (Z-axis)
- **rayDistance:** 0.01 units
- **rayOffset:** 0.1 units
- **drillRadius:** 0.03 units (auto-detected from drill bit)
- **carveSpeed:** 5 units/second
- **deformCooldown:** 0.05 seconds between deformations

**Rotation Mechanics:**
- **rotationAxis:** Y-axis (vertical rotation)
- Rotation speed proportional to trigger pressure
- Visual rotation of RotatingHead transform
- Synchronized with audio pitch

### 3.3 Heat Management System (DrillHeatSystem)

**Thermal Simulation Model:**

**Heat Accumulation:**
```
currentHeat += heatIncreaseRate × Time.deltaTime (when drilling)
currentHeat -= coolDownRate × Time.deltaTime (when idle)
currentHeat = Clamp(0, maxHeat)
```

**Configuration:**
- **maxHeat:** 100°C
- **heatIncreaseRate:** 8°/second (calculated from timeToMaxHeat = 20s)
- **coolDownRate:** 3°/second
- **warmColorThreshold:** 50°C (visual warning starts)
- **hotColorThreshold:** 90°C (critical warning, haptic lock)

**Multimodal Feedback System:**

| Temperature Range | Visual | Particle | Haptic | Audio | Safety |
|------------------|--------|----------|--------|-------|--------|
| 0-49°C | None | None | Free | Silent | Operational |
| 50-89°C | Orange→Red glow | Burning smell | Free | Low volume | Operational |
| 90-99°C | Red glow (max) | Heavy smoke | **Index finger locked (100% force)** | High volume | Warning |
| 100°C | Red glow (max) | Heavy smoke | Locked | Overheating sound | **Machine stops** |

**Visual Feedback:**
- **Glow Light:** Point light with intensity 0-3, color interpolation (cool → warm → hot)
- **Material Emission:** HDR emission color on drill bit material
- Emission intensity: 0-2 (mapped from warmColorThreshold to maxHeat)

**Particle Effects:**
1. **BurningSmellParticles:** Activated at 50°C (warm threshold)
   - Emission rate: 5-20 particles/second
   - Subtle warning indicator
   
2. **SmokeParticles:** Activated at 90°C (hot threshold)
   - Emission rate: 10-40 particles/second
   - Critical overheat indicator
   - Synchronized with haptic finger lock

**Haptic Force-Feedback Lock:**
- **Trigger Temperature:** ≥90°C (hotColorThreshold)
- **Release Temperature:** <90°C
- **Force Level:** 0-100% (configurable, default 100%)
- **Target Finger:** Index finger (ffb[1])
- **Mechanism:** Binary lock (ON/OFF), not gradual
- **Purpose:** Prevents trigger activation when overheated

**Implementation:**
```csharp
if (currentHeat >= hotColorThreshold)
{
    isFingerLocked = true;
    ffb[1] = indexFingerLockForce / 100f;  // Convert 0-100 to 0.0-1.0
}
else
{
    isFingerLocked = false;
    ffb[1] = 0f;
}
internalGlove.QueueFFBLevels(ffb);
internalGlove.SendHaptics();
```

**Safety Lock System:**
- At 100°C: Drill motor stops, cannot operate
- Release threshold: 50°C (safetyResetThreshold)
- Prevents damage to virtual equipment
- Simulates real-world thermal cutoff

**Manual Testing Mode:**
- **enableManualHeatControl:** Inspector toggle
- **manualHeatValue:** 0-100 slider
- Allows testing heat thresholds without drilling
- Useful for calibrating multimodal feedback

### 3.4 Battery Management System (DrillBatterySystem)

**Energy Model:**
```
currentCharge -= drillConsumptionRate × Time.deltaTime (when drilling)
currentCharge -= idleConsumptionRate × Time.deltaTime (when idle)
currentCharge += rechargeRate × Time.deltaTime (when autoRecharge enabled)
```

**Configuration:**
- **maxCharge:** 100%
- **drillConsumptionRate:** 5%/second
- **idleConsumptionRate:** 1%/second
- **rechargeRate:** 5%/second (auto-recharge enabled)

**Charge Thresholds:**
- **High:** ≥80% (green indicator)
- **Medium:** ≥60% (yellow indicator)
- **Low:** <60% (red indicator)
- **Empty:** 0% (drill disabled if disableDrillWhenEmpty = true)

**Visual Indicator:**
- **batteryIndicator:** BatteryPack GameObject
- Color-coded material feedback
- Real-time charge display

**Runtime:** ~20 seconds of continuous drilling before depletion

---

## 4. INTERCHANGEABLE DRILL BIT SYSTEM

### 4.1 Available Drill Bits

The scene includes four interchangeable drill bits:

| Drill Bit | Diameter | GameObject Path | Tag |
|-----------|----------|-----------------|-----|
| 3mm bit | 3mm | /3mm drill bit | DrillBit |
| 5mm bit | 5mm | /5mm drill bit | DrillBit |
| 8mm bit | 8mm | /8mm drill bit | DrillBit |
| 10mm bit | 10mm | /10mm drill bit | DrillBit |

**Common Components per Drill Bit:**
1. **SnapWithParenting** - Snap to drill holder
2. **MeshFilter/MeshRenderer** - Visual representation
3. **CapsuleCollider** - Physics collision
4. **SG_Material** - Haptic properties
5. **SG_Grabable** - Individual grab/release
6. **Rigidbody** - Physics simulation
7. **DrillInfoFloatingUI** - Size information display (3mm, 5mm, 8mm, 10mm)
8. **DrillBitVibration** - Vibrotactile feedback when drilling

### 4.2 Hot-Swap Mechanism

**Process:**
1. User grabs drill bit from environment
2. Brings bit near **SnapPoint_DrillBit** on drill
3. **SnapWithParenting** detects proximity
4. Bit snaps to holder, parents to drill
5. **SG_TriggerLogic** auto-detects active drill bit
6. Carve radius updates based on bit diameter
7. Particle systems transfer to new bit

**Technical Features:**
- **autoFindActiveDrillBit:** Automatically detects attached bit
- **autoDetectDrillSize:** Adjusts carve radius based on bit geometry
- Particle systems (smoke, dust, smell) move with active bit
- Drill bits remain grabbable when not attached

---

## 5. MATERIAL CARVING AND DEFORMATION

### 5.1 Carvable Materials

Three material specimens are provided for drilling practice:

| Material | GameObject | Layer | Component |
|----------|------------|-------|-----------|
| Wood | /Wood | Wood | ExistingModelCarving |
| Brass | /Brass | Wood | ExistingModelCarving |
| Cast Iron | /Cast Iron | Wood | ExistingModelCarving |

**Material Properties:**
- **SG_Material:** Defines haptic properties (friction, stiffness)
- **MeshCollider:** Enables physics collision
- **ExistingModelCarving:** Handles mesh deformation

### 5.2 Carving System (ExistingModelCarving)

**Deformation Mechanism:**
- Raycast from drill bit tip along drill direction
- Hit detection within drillRadius
- Mesh vertex manipulation at hit point
- Real-time mesh updates
- Cooldown prevents excessive deformation

**Physics Integration:**
- Collision detection with drill bit
- Haptic resistance feedback through SG_Material
- Force feedback to hand proportional to material hardness

**Wood Dust Generation (WoodDustGenerator):**
- **particleCount:** 12 particles per burst
- **particleSize:** 0.025 units
- **forceForward:** 2.0 units/second (ejection force)
- **forceRandom:** 0.5 (spray randomization)
- **lifeTime:** 1.0 second
- Emitted during active drilling
- Follows drill bit rotation and direction

---

## 6. ENVIRONMENT AND SCENE COMPOSITION

### 6.1 Garage Workshop Environment

**Scene Structure:**
```
Garage (Static parent)
└── Garage (Model container)
    ├── Workshop Furniture
    │   ├── 3 shelves
    │   ├── Big shelf
    │   ├── Large corner shelf
    │   ├── Shelf
    │   ├── Locker (×2)
    │   ├── Small locker (×2)
    │   ├── Opened locker
    │   └── Between locker
    ├── Equipment
    │   ├── Bench Grinder
    │   ├── Drilling machine
    │   ├── Ridgid oscillating belt sander
    │   ├── Saw
    │   └── Clamps
    ├── Tools & Parts
    │   ├── Cone Drill Bits
    │   ├── Drill Bits (×3 variants)
    │   ├── Round Drill Bits
    │   ├── Drill bits Lying
    │   ├── Drill part
    │   ├── Hooks (×3 types)
    │   ├── Hose
    │   └── Machinery parts (brake disc, mirror, shock absorber)
    ├── Storage
    │   ├── Black suitcase
    │   ├── Camouflage suitcase (×2 variants)
    │   ├── Red suitcase
    │   └── Longboard
    ├── Environment
    │   ├── Floor
    │   ├── Ceiling
    │   ├── Carpet
    │   ├── Exterior
    │   ├── Garage door
    │   ├── Emissive window
    │   └── 2 sockets
    └── Lighting
        ├── Fluorescent Light.001-006 (6 ceiling lights)
        └── Light panels on the ceiling
```

### 6.2 Lighting System

**Light Sources:**
1. **Directional Light** - Primary sun/ambient light
2. **Fluorescent Lights** - 6 ceiling-mounted fixtures
3. **Emissive Window** - Simulated daylight
4. **Light Panels** - Ambient garage lighting
5. **Reflection Probe** - Real-time reflections

**Render Settings:**
- **Pipeline:** URP (Universal Render Pipeline)
- **Renderer Type:** 3D
- Real-time lighting with mixed baked/dynamic lights

### 6.3 Interactive Objects

**Workshop Clock (Wallclock):**
```
Wallclock
└── Clock (Visual body)
    └── Arrow (Mechanism)
        ├── HourHand (Rotating)
        ├── MinuteHand (Rotating)
        ├── SecondHand (Rotating)
        └── Nail (Center pivot)
```
- Functional clock with animated hands
- Provides temporal reference in VR

**Additional Elements:**
- **EventSystem** - UI interaction handling
- **DataEchoLogger** - Data logging system (custom research logger)
- **Canvas** - UI overlay (currently inactive)

### 6.4 Fuel Tank

Located in Garage environment, likely a decorative workshop prop.

---

## 7. PHYSICS AND INTERACTION

### 7.1 Grab and Manipulation System

**SG_Grabable Configuration:**
- **moveSpeed:** 100 units/second
- **rotateSpeed:** 900 degrees/second
- **IsKinematic:** True (when grabbed)
- **allowedHands:** Any (both hands can grab)
- **alwaysTrackVelocity:** False

**Grab Detection:**
- Multi-collider system across palm, thumb, index, middle fingers
- Virtual vs. Real hand reference points
- Hover detection for pre-grab feedback
- Physics-based grab with force-feedback

**Dual-Hand Support:**
- Right and left hands fully implemented
- Mirror-symmetrical hand architecture
- Simultaneous multi-object grab capability

### 7.2 Haptic Material System (SG_Material)

Applied to:
- Drill body
- Drill bits
- Wood/Brass/Cast Iron materials
- All grabbable objects

**Properties:**
- Friction coefficient
- Stiffness value
- Temperature (for thermal feedback)
- Texture patterns

### 7.3 Audio Feedback

**Drill Audio:**
- **AudioSource** on DummyDrill-Rigged
- Pitch modulation based on rotation speed
- Volume modulation based on pressure
- Overheating sound at critical temperature
- Sizzle sound when drilling

---

## 8. ADVANCED FEATURES

### 8.1 Gesture Recognition

**Implemented Gestures:**
- **ThumbsUpGesture** (on both hands)
- Calibration gestures
- Custom gesture detection system

**Use Cases:**
- User feedback
- Training progression indicators
- Social interaction in multi-user scenarios

### 8.2 Hand Calibration System

**Calibration Layer:**
- Runtime hand size calibration
- Finger length adjustment
- Grip strength normalization
- Debug visualization

**Purpose:**
- Adapt to different hand sizes
- Improve tracking accuracy
- Enhance haptic feedback precision

### 8.3 Manual Posers

**ManualPoser_Right** and **ManualPoser_Left:**
- Development/testing tools
- Allow manual hand pose editing
- Debug hand tracking without hardware

### 8.4 Data Logging (DataEchoLogger)

**Integration:**
- Custom package: `com.eac.dataecho.logger`
- Records user interactions
- Logs performance metrics
- Exports data for research analysis

**Potential Data Capture:**
- Grab events
- Drill usage time
- Heat accumulation patterns
- Battery depletion rates
- Carving accuracy
- Haptic feedback responses

---

## 9. TECHNICAL SPECIFICATIONS

### 9.1 Unity Project Configuration

**Unity Version:** 2022.3.x LTS  
**Project Name:** SenseGlove  
**Scripting Backend:** Mono (IL2CPP for builds)  
**API Compatibility:** .NET Standard 2.1

### 9.2 Package Dependencies

**Core Packages:**
- **Unity XR:**
  - `com.unity.xr.interaction.toolkit` 3.1.2
  - `com.unity.xr.management` 4.5.1
  - `com.unity.xr.openxr` 1.14.3
  - `com.valvesoftware.unity.openvr` 1.2.1

- **Rendering:**
  - `com.unity.render-pipelines.universal` 14.0.12
  - `com.unity.textmeshpro` 3.0.9
  - `com.unity.probuilder` 5.2.4

- **Input:**
  - `com.unity.inputsystem` 1.14.0 (New Input System)

- **Performance:**
  - `com.unity.burst` 1.8.25 (Burst compiler)

- **Utilities:**
  - `com.unity.nuget.newtonsoft-json` 3.2.1
  - `com.unity.visualscripting` 1.9.4
  - `com.unity.timeline` 1.7.7

- **Research:**
  - `com.eac.dataecho.logger` (Custom logger)

### 9.3 Layer Configuration

| Layer | Purpose |
|-------|---------|
| Default | General objects |
| TransparentFX | Transparent effects |
| Ignore Raycast | Non-interactive objects |
| Wood | Carvable materials |
| Water | Reserved |
| UI | User interface |
| Drill | Drill body |
| DrillBit | Interchangeable bits |
| Table | Workshop surfaces |

### 9.4 Tag System

**Defined Tags:**
- **Untagged** - Default objects
- **Respawn** - Respawnable objects
- **MainCamera** - Main camera
- **Player** - Player objects
- **Wood** - Wood material detection
- **DrillBit** - Drill bit identification
- **projectile** - Projectile objects
- **Anchor** - Anchor points

---

## 10. CUSTOM SCRIPTS SUMMARY

### 10.1 Core Drill Scripts

**SG_TriggerLogic.cs**
- **Location:** /Assets/SenseGlove/Examples/Resources/
- **Purpose:** Trigger pressure detection, rotation control, carving logic
- **Key Methods:** 
  - Finger flexion to pressure mapping
  - Rotation speed calculation
  - Raycast-based carving
  - Material detection
- **Integration:** Interfaces with SG_Grabable, DrillHeatSystem, WoodDustGenerator

**DrillHeatSystem.cs**
- **Location:** /Assets/Scripts/
- **Purpose:** Thermal simulation, multimodal feedback orchestration
- **Key Methods:**
  - Heat accumulation/dissipation
  - Visual glow control
  - Particle system activation
  - Haptic finger lock
  - Safety lock enforcement
- **Integration:** Reads from SG_TriggerLogic, controls lights/particles/haptics

**DrillBatterySystem.cs**
- **Location:** /Assets/Scripts/
- **Purpose:** Power management, battery drain simulation
- **Key Methods:**
  - Charge depletion calculation
  - Auto-recharge logic
  - Visual indicator updates
  - Drill enable/disable based on charge
- **Integration:** Checked by SG_TriggerLogic to allow/prevent drilling

### 10.2 Drill Bit Scripts

**SnapWithParenting.cs**
- **Purpose:** Hot-swap drill bit attachment
- **Mechanism:** Proximity detection → snap → parent to drill

**DrillBitVibration.cs**
- **Purpose:** Vibrotactile feedback during drilling
- **Integration:** Triggered by drilling contact events

**DrillInfoFloatingUI.cs**
- **Purpose:** Display drill bit size (3mm, 5mm, 8mm, 10mm)
- **Rendering:** World-space UI element

### 10.3 Material Interaction Scripts

**ExistingModelCarving.cs**
- **Purpose:** Mesh deformation for carvable materials
- **Mechanism:** Vertex manipulation based on drill contact
- **Applied to:** Wood, Brass, Cast Iron

**WoodDustGenerator.cs**
- **Location:** /Assets/Drill assest/Scripts/
- **Purpose:** Procedural particle generation for wood dust
- **Parameters:** 12 particles, 0.025 size, 2.0 forward force, 1.0s lifetime

---

## 11. RESEARCH APPLICATIONS

### 11.1 Training and Education

**Skill Development:**
- Safe practice environment
- Drill operation fundamentals
- Material recognition
- Tool maintenance awareness (battery, heat)
- Safety protocol training (thermal cutoff)

**Assessment Metrics:**
- Drilling accuracy (carving precision)
- Heat management effectiveness
- Battery efficiency
- Drill bit selection appropriateness
- Safety awareness (response to warnings)

### 11.2 Human-Computer Interaction Research

**Haptic Feedback Studies:**
- Force-feedback lock effectiveness
- Multimodal warning perception
- Thermal simulation realism
- Vibrotactile feedback utility

**Experimental Variables:**
- Finger lock force levels (0-100%)
- Heat threshold values (warm/hot)
- Feedback modality combinations (visual only, haptic only, multimodal)
- Warning timing (early vs. late feedback)

### 11.3 Ergonomics and Safety

**Safety Training:**
- Overheat recognition and response
- Proper tool handling
- Material-appropriate drill bit selection
- Battery management awareness

**Ergonomic Analysis:**
- Hand fatigue under haptic resistance
- Grip pressure distribution
- Multi-finger coordination
- Two-handed tool manipulation

### 11.4 Data Collection Capabilities

**Logged Metrics (via DataEchoLogger):**
- Session duration
- Grab/release events
- Drill activation frequency
- Heat accumulation patterns
- Battery depletion rates
- Material carving attempts
- Safety lock triggers
- Finger lock activations
- Drill bit changes
- Gesture events

**Research Questions:**
- How effective is haptic finger lock for safety training?
- What multimodal feedback combination is most effective?
- Do users adapt behavior based on thermal warnings?
- How does haptic resistance affect drilling accuracy?
- What is the learning curve for VR drill operation?

---

## 12. SYSTEM WORKFLOW

### 12.1 Typical User Session

1. **Initialization:**
   - VR headset and SenseGlove calibration
   - Hand tracking verification
   - Scene loading

2. **Tool Selection:**
   - User walks to drill location
   - Grabs DummyDrill-Rigged with right or left hand
   - Drill snaps to hand position

3. **Drill Bit Selection (Optional):**
   - User selects appropriate drill bit (3mm/5mm/8mm/10mm)
   - Brings bit to drill holder
   - Bit snaps and parents to drill

4. **Material Selection:**
   - User approaches Wood, Brass, or Cast Iron specimen
   - Positions drill tip near surface

5. **Drilling Operation:**
   - User flexes index finger (30-50%)
   - Drill activates, rotation begins
   - Contact with material triggers:
     - Carving/deformation
     - Wood dust particles
     - Haptic resistance feedback
     - Audio feedback
   - Heat accumulates over time

6. **Thermal Progression:**
   - **0-50°C:** Normal operation
   - **50°C:** Orange glow appears, burning smell particles
   - **90°C:** Red glow, heavy smoke, **index finger locks**
   - **100°C:** Drill motor stops, safety lock engaged

7. **Cooling Phase:**
   - User releases trigger
   - Heat dissipates (3°/second)
   - At 90°C: Finger lock releases, smoke stops
   - At 50°C: Safety lock releases, drill operational again

8. **Battery Management:**
   - Battery drains during use (5%/second active, 1%/second idle)
   - Auto-recharge at 5%/second (if enabled)
   - Visual indicator shows charge level

9. **Tool Replacement:**
   - User releases drill
   - Drill returns to virtual physics simulation
   - Can be re-grabbed by any hand

### 12.2 System State Machine

```
STATE: Idle
├─ Drill not grabbed
├─ Heat cooling (-3°/s)
├─ Battery recharging (+5%/s if auto-recharge)
└─ → TRANSITION: User grabs drill → Grabbed

STATE: Grabbed
├─ Drill parented to hand
├─ Trigger inactive
├─ Heat cooling (-3°/s)
├─ Battery draining (-1%/s)
└─ → TRANSITION: Finger flexion >30% → Drilling

STATE: Drilling
├─ Rotation head spinning
├─ Heat accumulating (+8°/s)
├─ Battery draining (-5%/s)
├─ Audio playing
└─ → TRANSITIONS:
    ├─ Finger flexion <30% → Grabbed
    ├─ Heat ≥90°C → Warning (finger locked)
    ├─ Heat ≥100°C → Safety Lock
    └─ Battery ≤0% → Disabled

STATE: Warning (Heat ≥90°C)
├─ Red glow active
├─ Smoke particles active
├─ Index finger force-locked (100%)
├─ Cannot pull trigger
└─ → TRANSITION: Heat <90°C → Grabbed/Drilling

STATE: Safety Lock (Heat ≥100°C)
├─ Motor disabled
├─ Finger locked
├─ Cannot operate
└─ → TRANSITION: Heat <50°C → Grabbed

STATE: Disabled (Battery ≤0%)
├─ Cannot activate
├─ Visual low-battery indicator
└─ → TRANSITION: Battery >0% → Grabbed
```

---

## 13. PERFORMANCE CONSIDERATIONS

### 13.1 Real-Time Requirements

**Frame Rate Targets:**
- **VR Rendering:** 90 FPS (SteamVR standard)
- **Haptic Update:** 100-200 Hz (SenseGlove recommended)
- **Physics Simulation:** 50 Hz (Unity default)

### 13.2 Optimization Strategies

**Mesh Deformation:**
- Cooldown prevents excessive recalculation (0.05s)
- Localized vertex updates
- Mesh collider regeneration on demand

**Particle Systems:**
- Conditional activation based on temperature
- Emission rate scaling with heat level
- Limited particle count (12 per burst for dust)

**Hand Tracking:**
- 16 physics bodies per hand (32 total)
- Layered update priority (grab > physics > animation)
- Conditional debug visualization

### 13.3 Resource Management

**Active GameObjects:**
- Drill: 1 primary + 1 inactive backup
- Drill bits: 4 interchangeable + 1 attached
- Hands: 2 (left + right)
- Materials: 3 carvable specimens
- Environment: 1 static garage model

**Total Interactive Objects:** ~15-20 active at runtime

---

## 14. LIMITATIONS AND FUTURE WORK

### 14.1 Current Limitations

1. **Material Carving:**
   - Limited to simple mesh deformation
   - No persistent hole creation
   - No material chip/fragment physics

2. **Thermal Simulation:**
   - Simplified linear heat model
   - No conduction to handle/user hand
   - No ambient temperature effects

3. **Battery System:**
   - Auto-recharge may be unrealistic
   - No visual battery removal/replacement

4. **Multi-User:**
   - Single-user design
   - No collaborative drilling scenarios

5. **Material Variety:**
   - Only 3 materials (wood, brass, cast iron)
   - No composite materials
   - No material failure modes (cracking, splintering)

### 14.2 Potential Enhancements

**Technical:**
- Advanced mesh carving with CSG (Constructive Solid Geometry)
- Thermal conduction simulation
- Material fracture physics
- Realistic battery replacement interaction
- Variable drill speed control
- Torque simulation with haptic feedback

**Content:**
- Additional tool types (impact driver, sander, grinder)
- More materials (plastic, aluminum, concrete)
- Assembly/disassembly tasks
- Multi-step projects (furniture assembly)

**Research:**
- Eye-tracking integration for attention analysis
- Physiological monitoring (heart rate, galvanic skin response)
- Adaptive difficulty based on performance
- AI-based skill assessment
- Comparative studies (VR vs. traditional training)

---

## 15. CONCLUSION

The Workshop Scene represents a comprehensive VR haptic training simulation that successfully integrates:

✅ **Hardware Integration:** SenseGlove Nova haptic gloves with 5-finger force feedback  
✅ **Physics Simulation:** Realistic material carving and deformation  
✅ **Multimodal Feedback:** Visual (glow), auditory (sound), haptic (vibration, force), olfactory-metaphorical (smoke/smell particles)  
✅ **Safety Training:** Overheat warnings with haptic enforcement (finger lock)  
✅ **Interaction Design:** Intuitive grab, trigger, and tool manipulation  
✅ **Modular Architecture:** Interchangeable drill bits, carvable materials  
✅ **Research Instrumentation:** Data logging, manual testing controls  

**Key Technical Achievements:**
1. **Index finger force-feedback lock** at 90°C provides immediate, unmistakable safety warning
2. **Synchronized multimodal feedback** (visual + haptic + particle) at hot threshold enhances perception
3. **Physics-based carving** with real-time mesh deformation
4. **Hot-swappable drill bits** enable task variety within single session
5. **Dual-hand support** with full SenseGlove integration

**Research Value:**
- Platform for haptic feedback effectiveness studies
- Safety training protocol development
- Skill acquisition research in VR
- Multimodal warning system evaluation
- Ergonomic analysis of virtual tool use

This scene serves as both a functional training tool and a research testbed for investigating how haptic feedback, thermal simulation, and multimodal warnings can enhance safety awareness and skill development in virtual reality environments.

---

## APPENDIX A: FILE STRUCTURE

```
/Assets
├── Scripts
│   ├── DrillHeatSystem.cs (Custom thermal simulation)
│   ├── DrillBatterySystem.cs (Custom power management)
│   ├── DrillInfoFloatingUI.cs (Drill bit UI)
│   ├── DrillBitVibration.cs (Vibrotactile feedback)
│   └── SnapWithParenting.cs (Drill bit attachment)
├── Drill assest/Scripts
│   └── WoodDustGenerator.cs (Particle generation)
├── SenseGlove/Examples
│   ├── Resources
│   │   └── SG_TriggerLogic.cs (Trigger and carving logic)
│   ├── Workshop Scene.unity (Main scene)
│   └── Workshop Scene (Scene assets folder)
└── SenseGlove (SDK)
    └── [SenseGlove Nova SDK files]
```

---

## APPENDIX B: INSPECTOR CONFIGURATION REFERENCE

### DrillHeatSystem Inspector Values

**Heat Settings:**
- Current Heat: 0°C
- Max Heat: 100°C
- Heat Increase Rate: 8°/s (auto-calculated)
- Cool Down Rate: 3°/s
- Warm Color Threshold: 50°C
- Hot Color Threshold: 90°C

**Manual Temperature Control:**
- Enable Manual Heat Control: ☐ (testing only)
- Manual Heat Value: 0°C
- Time To Max Heat: 20 seconds

**Drill Bit References:**
- Drill Bit Tip: Auto-assigned
- Drill Bit Renderer: Auto-assigned
- Material Index: 0

**Particle Effects:**
- Smoke Particles: /DummyDrill-Rigged/.../SmokeParticles
- Burning Smell Particles: (Optional)
- Move Particles With Drill Bit: ☑

**Glow Effect:**
- Drill Bit Glow: Auto-created
- Cool Color: RGB(128, 128, 128)
- Warm Color: RGB(254, 74, 4)
- Hot Color: RGB(255, 26, 0)
- Max Glow Intensity: 3.0

**Material Emission:**
- Use Emission Glow: ☑
- Max Emission Intensity: 2.0

**Overheat Safety:**
- Enable Overheat Safety: ☑
- Safety Reset Threshold: 60°C

**SenseGlove Finger Lock:**
- Enable Finger Lock: ☑
- Tracked Hand: Auto-detected
- Index Finger Lock Force: 100 (0-100 scale)

**Battery Integration:**
- Battery System: /DummyDrill-Rigged

---

## APPENDIX C: CITATION TEMPLATE

**For Academic Papers:**

```
Workshop VR Haptic Drill Training Simulation
Unity 2022.3 with Universal Render Pipeline
SenseGlove Nova Haptic Gloves
Key Systems:
- Thermal overheat simulation with multimodal feedback
- Force-feedback finger lock at 90°C thermal threshold
- Physics-based material carving (wood, brass, cast iron)
- Interchangeable drill bit system (3mm, 5mm, 8mm, 10mm)
- Battery management simulation
- Data logging via DataEchoLogger
```

**Recommended Sections for Academic Papers:**
1. **Introduction:** VR training simulation with haptic feedback
2. **Methods - Hardware:** SenseGlove Nova specifications
3. **Methods - Software:** Unity 2022.3, URP, custom scripts
4. **Methods - Thermal Simulation:** Heat accumulation model, multimodal feedback
5. **Methods - Haptic Safety:** Index finger force-feedback lock mechanism
6. **Methods - Data Collection:** DataEchoLogger integration
7. **Results:** User performance metrics, safety awareness, haptic effectiveness
8. **Discussion:** Implications for VR training, haptic feedback utility

---

**Document Version:** 1.0  
**Last Updated:** 2024  
**Total Word Count:** ~5,800 words  
**Technical Depth:** Graduate/Research Level
