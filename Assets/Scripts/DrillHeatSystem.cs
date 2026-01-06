using UnityEngine;
using SG;

public class DrillHeatSystem : MonoBehaviour
{
    [Header("Heat Settings")]
    [Range(0, 100)] public float currentHeat = 0f;
    public float maxHeat = 100f;
    public float heatIncreaseRate = 5f;
    public float coolDownRate = 3f;
    public float warmColorThreshold = 50f;
    public float hotColorThreshold = 90f;

    [Header("Manual Temperature Control")]
    public bool enableManualHeatControl = false;
    [Range(0, 100)] public float manualHeatValue = 0f;
    [Tooltip("Time in seconds to reach max heat when drilling continuously")]
    public float timeToMaxHeat = 20f;

    [Header("Drill Bit References")]
    public Transform drillBitTip;
    public Renderer drillBitRenderer;
    public int materialIndex = 0;

    [Header("Particle Effects")]
    public ParticleSystem smokeParticles;
    public ParticleSystem burningSmellParticles;
    public bool moveParticlesWithDrillBit = true;
    
    [Header("Glow Effect")]
    public Light drillBitGlow;
    public Color coolColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    public Color warmColor = new Color(1f, 0.5f, 0f, 1f);
    public Color hotColor = new Color(1f, 0.1f, 0f, 1f);
    public float maxGlowIntensity = 3f;

    [Header("Material Color Override")]
    public bool useColorOverride = true;
    public string colorPropertyName = "_Color";
    public float maxEmissionIntensity = 2f;
    
    [Header("Debug")]
    public bool enableDebugLogs = false;

    [Header("Overheat Safety")]
    public bool enableOverheatSafety = true;
    public float safetyResetThreshold = 50f;

    [Header("SenseGlove Finger Lock")]
    public bool enableFingerLock = true;
    public SG_TrackedHand trackedHand;
    
    [Header("Lock Position Settings")]
    [Tooltip("Flexion position to lock the finger at when overheated (0 = straight, 1 = fully bent)")]
    [Range(0f, 1f)]
    public float targetLockFlexion = 0.5f;
    
    [Tooltip("Maximum force to resist movement from target position (0-100%)")]
    [Range(0, 100)]
    public int lockForce = 100;
    
    [Tooltip("Flexion tolerance - creates a 'dead zone' around target position")]
    [Range(0f, 0.2f)]
    public float flexionTolerance = 0.05f;
    
    [Tooltip("Spring strength - how aggressively to return to target (higher = stronger)")]
    [Range(1f, 5f)]
    public float springStrength = 3f;
    
    [Tooltip("Temperature at which finger lock releases (must be lower than lock temperature 100)")]
    [Range(0, 100)] 
    public float fingerLockReleaseThreshold = 70f;

    [Header("Battery Integration")]
    public DrillBatterySystem batterySystem;

    private SG_TriggerLogic triggerLogic;
    private SG_HapticGlove hapticGlove;
    private SGCore.HapticGlove internalGlove;
    private bool isOverheating = false;
    private bool isBurning = false;
    private bool isFingerLocked = false;
    private bool isSafetyLocked = false;
    private bool lastFFBState = false;
    private Material drillBitMaterial;
    private Material[] drillBitMaterials;
    private Color originalColor;
    private Color originalEmissionColor;
    private bool hasEmission = false;
    private bool hasColorProperty = false;
    private Vector3 smokeParticlesOriginalOffset;
    private Vector3 burningSmellParticlesOriginalOffset;
    private Quaternion smokeParticlesOriginalRotation;
    private Quaternion burningSmellParticlesOriginalRotation;
    private Transform lastDrillBitTip;

    void Start()
    {
        triggerLogic = GetComponent<SG_TriggerLogic>();
        
        if (trackedHand != null)
        {
            hapticGlove = trackedHand.GetComponent<SG_HapticGlove>();
            if (hapticGlove != null)
            {
                internalGlove = (SGCore.HapticGlove)hapticGlove.InternalGlove;
            }
        }

        if (triggerLogic != null)
        {
            if (triggerLogic.grabable != null && triggerLogic.grabable.ScriptsGrabbingMe().Count > 0)
            {
                trackedHand = triggerLogic.grabable.ScriptsGrabbingMe()[0].TrackedHand;
                hapticGlove = trackedHand.GetComponent<SG_HapticGlove>();
                if (hapticGlove != null)
                {
                    internalGlove = (SGCore.HapticGlove)hapticGlove.InternalGlove;
                }
            }
        }

        if (enableDebugLogs)
        {
            Debug.Log($"[DrillHeatSystem] Initialized. TrackedHand: {trackedHand != null}, HapticGlove: {hapticGlove != null}");
        }

        heatIncreaseRate = maxHeat / timeToMaxHeat;

        if (batterySystem == null)
        {
            batterySystem = GetComponent<DrillBatterySystem>();
        }

        if (drillBitTip == null && triggerLogic != null)
        {
            drillBitTip = triggerLogic.CurrentDrillTip;
        }

        if (drillBitRenderer == null && drillBitTip != null)
        {
            drillBitRenderer = drillBitTip.GetComponent<Renderer>();
        }

        if (drillBitRenderer != null && drillBitRenderer.materials.Length > materialIndex)
        {
            drillBitMaterials = drillBitRenderer.materials;
            drillBitMaterial = drillBitMaterials[materialIndex];
            
            if (drillBitMaterial.HasProperty("_EmissionColor"))
            {
                hasEmission = true;
                originalEmissionColor = drillBitMaterial.GetColor("_EmissionColor");
            }
            
            if (drillBitMaterial.HasProperty(colorPropertyName))
            {
                hasColorProperty = true;
                originalColor = drillBitMaterial.GetColor(colorPropertyName);
            }
            
            drillBitRenderer.materials = drillBitMaterials;
        }

        if (drillBitGlow == null && drillBitTip != null)
        {
            GameObject glowObj = new GameObject("DrillBitGlow");
            glowObj.transform.SetParent(drillBitTip);
            glowObj.transform.localPosition = Vector3.zero;
            drillBitGlow = glowObj.AddComponent<Light>();
            drillBitGlow.type = LightType.Point;
            drillBitGlow.range = 0.5f;
            drillBitGlow.intensity = 0f;
            drillBitGlow.color = coolColor;
        }

        if (smokeParticles != null)
        {
            smokeParticles.Stop();
            smokeParticlesOriginalOffset = smokeParticles.transform.localPosition;
            smokeParticlesOriginalRotation = smokeParticles.transform.localRotation;
        }
        
        if (burningSmellParticles != null)
        {
            burningSmellParticles.Stop();
            burningSmellParticlesOriginalOffset = burningSmellParticles.transform.localPosition;
            burningSmellParticlesOriginalRotation = burningSmellParticles.transform.localRotation;
        }

        // Diagnostic call for haptic setup
        if (enableDebugLogs)
        {
            Invoke("DiagnoseHapticSystem", 1f);
        }
    }

    void Update()
    {
        // DEBUG: Press 'F' to test force feedback
        if (Input.GetKeyDown(KeyCode.F))
        {
            TestForceFeeback();
        }

        if (enableManualHeatControl)
        {
            currentHeat = manualHeatValue;
        }
        else if (triggerLogic != null)
        {
            if (drillBitTip == null || drillBitTip != triggerLogic.CurrentDrillTip)
            {
                drillBitTip = triggerLogic.CurrentDrillTip;
                UpdateDrillBitReferences();
            }

            bool isDrilling = triggerLogic.CurrentPressure > 0.1f && !IsDrillLocked();

            if (batterySystem != null && !batterySystem.CanDrill())
            {
                isDrilling = false;
            }

            if (enableDebugLogs && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[DrillHeatSystem] Pressure: {triggerLogic.CurrentPressure:F2}, isDrilling: {isDrilling}, Heat: {currentHeat:F1}, SafetyLocked: {isSafetyLocked}, FingerLocked: {isFingerLocked}");
            }

            if (isDrilling)
            {
                currentHeat += heatIncreaseRate * Time.deltaTime;

                if (enableDebugLogs && Time.frameCount % 60 == 0)
                {
                    Debug.Log($"[DrillHeatSystem] Heat increasing by {heatIncreaseRate * Time.deltaTime:F3} per frame");
                }
            }
            else
            {
                currentHeat -= coolDownRate * Time.deltaTime;
            }
        }
        else
        {
            currentHeat -= coolDownRate * Time.deltaTime;
            
            if (enableDebugLogs && Time.frameCount % 120 == 0)
            {
                Debug.LogWarning("[DrillHeatSystem] SG_TriggerLogic is null! Cannot detect drilling.");
            }
        }

        currentHeat = Mathf.Clamp(currentHeat, 0, maxHeat);

        UpdateSafetyLock();
        UpdateFingerLock();
        UpdateVisualEffects();
        UpdateParticleEffects();
    }

    void UpdateDrillBitReferences()
    {
        if (drillBitTip != null && drillBitTip != lastDrillBitTip)
        {
            ResetPreviousMaterial();

            drillBitRenderer = drillBitTip.GetComponent<Renderer>();
            
            if (drillBitRenderer != null && drillBitRenderer.materials.Length > materialIndex)
            {
                drillBitMaterials = drillBitRenderer.materials;
                drillBitMaterial = drillBitMaterials[materialIndex];
                
                hasEmission = false;
                hasColorProperty = false;
                
                if (drillBitMaterial.HasProperty("_EmissionColor"))
                {
                    hasEmission = true;
                    originalEmissionColor = drillBitMaterial.GetColor("_EmissionColor");
                }
                
                if (drillBitMaterial.HasProperty(colorPropertyName))
                {
                    hasColorProperty = true;
                    originalColor = drillBitMaterial.GetColor(colorPropertyName);
                }
                
                drillBitRenderer.materials = drillBitMaterials;
                
                if (enableDebugLogs)
                {
                    Debug.Log($"<color=lime>[DrillHeatSystem] ✓ New drill bit detected: {drillBitTip.name}</color>\n" +
                              $"  Shader: {drillBitMaterial.shader.name}\n" +
                              $"  Has _EmissionColor: {hasEmission}\n" +
                              $"  Has {colorPropertyName}: {hasColorProperty}\n" +
                              $"  Original Color: {(hasColorProperty ? originalColor.ToString() : "N/A")}");
                }
            }

            if (drillBitGlow != null)
            {
                drillBitGlow.transform.SetParent(drillBitTip);
                drillBitGlow.transform.localPosition = Vector3.zero;
            }

            if (moveParticlesWithDrillBit)
            {
                if (smokeParticles != null)
                {
                    smokeParticles.transform.SetParent(drillBitTip);
                    smokeParticles.transform.localPosition = smokeParticlesOriginalOffset;
                    smokeParticles.transform.localRotation = smokeParticlesOriginalRotation;
                }

                if (burningSmellParticles != null)
                {
                    burningSmellParticles.transform.SetParent(drillBitTip);
                    burningSmellParticles.transform.localPosition = burningSmellParticlesOriginalOffset;
                    burningSmellParticles.transform.localRotation = burningSmellParticlesOriginalRotation;
                }
            }

            lastDrillBitTip = drillBitTip;
        }
    }

    void ResetPreviousMaterial()
    {
        if (drillBitMaterial != null && drillBitRenderer != null)
        {
            if (hasEmission)
            {
                drillBitMaterial.SetColor("_EmissionColor", originalEmissionColor);
            }
            
            if (hasColorProperty)
            {
                drillBitMaterial.SetColor(colorPropertyName, originalColor);
            }
            
            drillBitRenderer.materials = drillBitMaterials;
        }
    }

    void UpdateVisualEffects()
    {
        float heatPercent = currentHeat / maxHeat;
        float warmPercent = warmColorThreshold / maxHeat;

        if (drillBitGlow != null)
        {
            if (currentHeat < warmColorThreshold)
            {
                drillBitGlow.intensity = 0f;
                drillBitGlow.color = coolColor;
            }
            else
            {
                float adjustedPercent = (heatPercent - warmPercent) / (1f - warmPercent);
                drillBitGlow.intensity = Mathf.Lerp(0, maxGlowIntensity, adjustedPercent);
                
                if (adjustedPercent < 0.5f)
                    drillBitGlow.color = Color.Lerp(warmColor, hotColor, adjustedPercent * 2f);
                else
                    drillBitGlow.color = hotColor;
            }
        }

        if (useColorOverride && drillBitMaterial != null && drillBitRenderer != null)
        {
            Color targetColor;
            
            if (currentHeat < warmColorThreshold)
            {
                targetColor = originalColor;
            }
            else
            {
                float adjustedPercent = (heatPercent - warmPercent) / (1f - warmPercent);
                
                Color heatColor;
                if (adjustedPercent < 0.5f)
                    heatColor = Color.Lerp(warmColor, hotColor, adjustedPercent * 2f);
                else
                    heatColor = hotColor;
                
                targetColor = Color.Lerp(originalColor, heatColor, adjustedPercent);
            }

            if (hasEmission)
            {
                float emissionIntensity = currentHeat < warmColorThreshold ? 0f : ((heatPercent - warmPercent) / (1f - warmPercent));
                Color emissionColor = currentHeat < warmColorThreshold ? originalEmissionColor : (currentHeat < hotColorThreshold ? warmColor : hotColor);
                drillBitMaterial.SetColor("_EmissionColor", emissionColor * emissionIntensity * maxEmissionIntensity);
                drillBitMaterial.EnableKeyword("_EMISSION");
            }
            
            if (hasColorProperty)
            {
                drillBitMaterial.SetColor(colorPropertyName, targetColor);
                
                if (enableDebugLogs && Time.frameCount % 60 == 0)
                {
                    Debug.Log($"<color=orange>[Heat Visual] Setting {colorPropertyName} to {targetColor} (Heat: {currentHeat:F1}°C, Original: {originalColor})</color>");
                }
            }
            
            drillBitRenderer.materials = drillBitMaterials;
        }
    }

    void UpdateParticleEffects()
    {
        if (currentHeat >= warmColorThreshold && !isBurning)
        {
            isBurning = true;
            if (burningSmellParticles != null)
                burningSmellParticles.Play();
        }
        else if (currentHeat < warmColorThreshold && isBurning)
        {
            isBurning = false;
            if (burningSmellParticles != null)
                burningSmellParticles.Stop();
        }

        if (currentHeat >= hotColorThreshold && !isOverheating)
        {
            isOverheating = true;
            if (smokeParticles != null)
                smokeParticles.Play();
        }
        else if (currentHeat < hotColorThreshold && isOverheating)
        {
            isOverheating = false;
            if (smokeParticles != null)
                smokeParticles.Stop();
        }

        if (burningSmellParticles != null && isBurning)
        {
            var emission = burningSmellParticles.emission;
            float emissionRate = Mathf.Lerp(5, 20, (currentHeat - warmColorThreshold) / (maxHeat - warmColorThreshold));
            emission.rateOverTime = emissionRate;
        }

        if (smokeParticles != null && isOverheating)
        {
            var emission = smokeParticles.emission;
            float emissionRate = Mathf.Lerp(10, 40, (currentHeat - hotColorThreshold) / (maxHeat - hotColorThreshold));
            emission.rateOverTime = emissionRate;
        }
    }

    public float GetHeatPercentage()
    {
        return currentHeat / maxHeat;
    }

    public bool IsOverheating()
    {
        return isOverheating;
    }

    public bool IsBurning()
    {
        return isBurning;
    }

    public bool IsDrillLocked()
    {
        return enableOverheatSafety && isSafetyLocked;
    }

    public bool IsFingerLocked()
    {
        return enableFingerLock && isFingerLocked;
    }

    void UpdateSafetyLock()
    {
        if (!enableOverheatSafety) return;

        if (currentHeat >= maxHeat && !isSafetyLocked)
        {
            isSafetyLocked = true;
            
            if (enableDebugLogs)
            {
                Debug.Log($"[DrillHeatSystem] 🚨 SAFETY ENGAGED! Drill at 100% heat, locked until cooled to {safetyResetThreshold}°");
            }
        }
        else if (currentHeat <= safetyResetThreshold && isSafetyLocked)
        {
            isSafetyLocked = false;
            
            if (enableDebugLogs)
            {
                Debug.Log($"[DrillHeatSystem] ✅ SAFETY RELEASED! Drill operational again.");
            }
        }
    }

    void UpdateFingerLock()
    {
        if (!enableFingerLock)
        {
            if (enableDebugLogs && Time.frameCount % 120 == 0)
            {
                Debug.Log("[DrillHeatSystem] Finger lock disabled in settings.");
            }
            return;
        }

        // ===== INITIALIZATION PHASE =====
        // Try to get TrackedHand if not assigned
        if (trackedHand == null && triggerLogic != null && triggerLogic.grabable != null)
        {
            var grabbingScripts = triggerLogic.grabable.ScriptsGrabbingMe();
            if (grabbingScripts.Count > 0)
            {
                trackedHand = grabbingScripts[0].TrackedHand;
                if (enableDebugLogs && trackedHand != null)
                {
                    Debug.Log($"<color=cyan>[DrillHeatSystem] ✓ TrackedHand acquired from grabable!</color>");
                }
            }
        }

        // Try to get HapticGlove if not assigned
        if (hapticGlove == null && trackedHand != null)
        {
            hapticGlove = trackedHand.GetComponent<SG_HapticGlove>();
            
            if (enableDebugLogs && hapticGlove != null)
            {
                Debug.Log($"<color=cyan>[DrillHeatSystem] ✓ HapticGlove acquired from TrackedHand!</color>");
            }
        }

        // Get internal glove reference
        if (internalGlove == null && hapticGlove != null)
        {
            try
            {
                internalGlove = (SGCore.HapticGlove)hapticGlove.InternalGlove;
                
                if (enableDebugLogs && internalGlove != null)
                {
                    Debug.Log($"<color=cyan>[DrillHeatSystem] ✓ Internal glove acquired! Type: {internalGlove.GetType().Name}</color>");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[DrillHeatSystem] Failed to cast InternalGlove: {e.Message}");
                internalGlove = null;
            }
        }

        // ===== VALIDATION PHASE =====
        if (trackedHand == null)
        {
            if (enableDebugLogs && Time.frameCount % 120 == 0)
            {
                Debug.LogWarning($"[DrillHeatSystem] TrackedHand is null!");
            }
            return;
        }

        if (!trackedHand.IsConnected())
        {
            if (enableDebugLogs && Time.frameCount % 120 == 0)
            {
                Debug.LogWarning($"[DrillHeatSystem] TrackedHand not connected!");
            }
            return;
        }

        if (hapticGlove == null)
        {
            if (enableDebugLogs && Time.frameCount % 120 == 0)
            {
                Debug.LogWarning($"[DrillHeatSystem] HapticGlove is null!");
            }
            return;
        }

        if (internalGlove == null)
        {
            if (enableDebugLogs && Time.frameCount % 120 == 0)
            {
                Debug.LogWarning($"[DrillHeatSystem] Internal glove is null!");
            }
            return;
        }

        if (!internalGlove.IsConnected())
        {
            if (enableDebugLogs && Time.frameCount % 120 == 0)
            {
                Debug.LogWarning($"[DrillHeatSystem] Glove not connected!");
            }
            return;
        }

        // ===== LOCK/UNLOCK LOGIC =====
        bool shouldLock = currentHeat >= maxHeat;
        
        // DEBUG: Log state every frame to see what's happening
        if (enableDebugLogs && Time.frameCount % 10 == 0)
        {
            Debug.Log($"[Lock Logic] Heat: {currentHeat:F1}/{maxHeat} | shouldLock: {shouldLock} | isFingerLocked: {isFingerLocked} | ReleaseThreshold: {fingerLockReleaseThreshold}");
        }
        
        if (shouldLock && !isFingerLocked)
        {
            isFingerLocked = true;
            
            if (enableDebugLogs)
            {
                Debug.Log($"<color=lime>[DrillHeatSystem] 🔒 INDEX FINGER LOCKED at position {targetLockFlexion:F2}! Heat: {currentHeat:F1}°C >= {maxHeat}°C</color>");
            }
        }
        else if (currentHeat <= fingerLockReleaseThreshold && isFingerLocked)
        {
            isFingerLocked = false;
            
            if (enableDebugLogs)
            {
                Debug.Log($"<color=cyan>[DrillHeatSystem] 🔓 INDEX FINGER RELEASED! Heat: {currentHeat:F1}°C <= {fingerLockReleaseThreshold}°C</color>");
            }
        }

        // ===== POSITION-BASED FORCE FEEDBACK =====
        // Lock finger at a specific flexion position using proportional force
        try
        {
            float indexForce = 0f;
            
            if (isFingerLocked)
            {
                // Get current index finger flexion
                if (trackedHand.GetNormalizedFlexion(out float[] flexions) && flexions.Length > 1)
                {
                    float currentFlexion = flexions[1]; // Index finger
                    float flexionError = currentFlexion - targetLockFlexion;
                    
                    // Only apply force if outside tolerance zone
                    if (Mathf.Abs(flexionError) > flexionTolerance)
                    {
                        // Proportional force based on distance from target
                        // Creates a "spring" effect that pulls finger to target position
                        float proportionalForce = Mathf.Abs(flexionError) * springStrength;
                        
                        // Scale by max force setting and clamp
                        indexForce = Mathf.Clamp01(proportionalForce) * (lockForce / 100f);
                        
                        if (enableDebugLogs && Time.frameCount % 30 == 0)
                        {
                            Debug.Log($"<color=yellow>[Position Lock] Current: {currentFlexion:F2} | Target: {targetLockFlexion:F2} | " +
                                     $"Error: {flexionError:F3} | Force: {indexForce:P0}</color>");
                        }
                    }
                    else
                    {
                        // Within tolerance - no force needed
                        indexForce = 0f;
                        
                        if (enableDebugLogs && Time.frameCount % 90 == 0)
                        {
                            Debug.Log($"<color=lime>[Position Lock] ✓ At target ({currentFlexion:F2} ≈ {targetLockFlexion:F2})</color>");
                        }
                    }
                }
                else
                {
                    // Can't read flexion - use constant force as fallback
                    indexForce = lockForce / 100f;
                    
                    if (enableDebugLogs && Time.frameCount % 60 == 0)
                    {
                        Debug.LogWarning($"[Position Lock] Can't read flexion - using constant {lockForce}%");
                    }
                }
            }
            
            // Create force array
            float[] ffb = new float[5];
            ffb[0] = 0f;        // Thumb
            ffb[1] = indexForce; // Index - position-locked when overheated
            ffb[2] = 0f;        // Middle
            ffb[3] = 0f;        // Ring
            ffb[4] = 0f;        // Pinky
            
            // CRITICAL: Use ONLY the internal glove method, not the wrapper
            internalGlove.QueueFFBLevels(ffb);
            internalGlove.SendHaptics();
            
            // Send haptics
            internalGlove.QueueFFBLevels(ffb);
            internalGlove.SendHaptics();
            
            if (enableDebugLogs && Time.frameCount % 60 == 0)
            {
                string lockStatus = isFingerLocked ? "🔒 LOCKED" : "🔓 RELEASED";
                Debug.Log($"<color=lime>[Finger Lock] {lockStatus} - Heat: {currentHeat:F1}° | Force: {indexForce:P0}</color>");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[DrillHeatSystem] Haptic feedback error: {e.Message}\n{e.StackTrace}");
        }
    }

    void DiagnoseHapticSystem()
    {
        Debug.Log("\n========== HAPTIC SYSTEM FULL DIAGNOSIS ==========\n");
        
        // 1. Check TrackedHand
        if (trackedHand == null)
        {
            Debug.LogError("❌ TrackedHand is NULL! Cannot proceed.");
            return;
        }
        Debug.Log($"✓ TrackedHand found: {trackedHand.name}");
        Debug.Log($"  Hand type: {trackedHand.GetType().Name}");
        
        // 2. Check if hand is connected
        bool handConnected = trackedHand.IsConnected();
        Debug.Log(handConnected ? "✓ TrackedHand IS CONNECTED" : "❌ TrackedHand NOT CONNECTED");
        
        if (!handConnected)
        {
            Debug.LogError("The hand tracker itself is not connected. Check your hand tracking system.");
            return;
        }
        
        // 3. Check HapticGlove wrapper
        if (hapticGlove == null)
        {
            Debug.LogError("❌ HapticGlove component not found on TrackedHand!");
            Debug.Log("Available components on TrackedHand:");
            foreach (var comp in trackedHand.GetComponents<Component>())
            {
                Debug.Log($"  - {comp.GetType().Name}");
            }
            return;
        }
        Debug.Log($"✓ HapticGlove wrapper found: {hapticGlove.GetType().Name}");
        
        // 4. Check internal glove
        if (internalGlove == null)
        {
            Debug.LogError("❌ Internal glove is NULL!");
            return;
        }
        Debug.Log($"✓ Internal glove found: {internalGlove.GetType().Name}");
        
        // 5. Check glove connection
        bool gloveConnected = internalGlove.IsConnected();
        if (!gloveConnected)
        {
            Debug.LogError("❌ GLOVE IS NOT CONNECTED!");
            Debug.LogError("This is the root cause - the physical glove device is not communicating.");
            Debug.LogError("CHECK:");
            Debug.LogError("  1. Is the glove powered ON?");
            Debug.LogError("  2. Is the USB cable connected?");
            Debug.LogError("  3. Is the glove recognized in Device Manager?");
            Debug.LogError("  4. Try unplugging and reconnecting the glove.");
            return;
        }
        Debug.Log("✓ Glove IS CONNECTED");
        
        // 6. Try to get flexion data (important test)
        Debug.Log("\n--- Testing Hand Input ---");
        if (trackedHand.GetNormalizedFlexion(out float[] flexions))
        {
            Debug.Log("✓ Hand flexion data available:");
            for (int i = 0; i < flexions.Length && i < 5; i++)
            {
                string[] fingerNames = { "Thumb", "Index", "Middle", "Ring", "Pinky" };
                Debug.Log($"  {fingerNames[i]}: {flexions[i]:F2}");
            }
        }
        else
        {
            Debug.LogError("❌ Cannot read hand flexion data!");
        }
        
        // 7. Test force feedback
        Debug.Log("\n--- Testing Force Feedback ---");
        try
        {
            Debug.Log("Sending test forces to each finger...");
            
            string[] fingerNames = { "Thumb", "Index", "Middle", "Ring", "Pinky" };
            
            for (int i = 0; i < 5; i++)
            {
                float[] testFFB = new float[5];
                testFFB[i] = 1f;
                
                internalGlove.QueueFFBLevels(testFFB);
                internalGlove.SendHaptics();
                
                Debug.Log($"✓ Sending 100% force to {fingerNames[i]}...");
                Debug.Log($"  FFB array: [{testFFB[0]}, {testFFB[1]}, {testFFB[2]}, {testFFB[3]}, {testFFB[4]}]");
                
                System.Threading.Thread.Sleep(500); // Wait 500ms between tests
            }
            
            Debug.Log("\n✓ Force feedback test completed");
            Debug.Log("DID YOU FEEL FORCE ON YOUR FINGERS?");
            Debug.Log("  - If YES for all: System is fully functional!");
            Debug.Log("  - If YES for some: Only certain fingers work (hardware issue)");
            Debug.Log("  - If NO: Glove is connected but not receiving force (firmware issue)");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Force feedback test failed: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
        }
        
        Debug.Log("\n========== END DIAGNOSIS ==========\n");
    }

    void OnDisable()
    {
        ReleaseFingerLock();
    }

    void OnDestroy()
    {
        ReleaseFingerLock();
        
        if (drillBitMaterial != null && drillBitRenderer != null)
        {
            if (hasEmission)
            {
                drillBitMaterial.SetColor("_EmissionColor", originalEmissionColor);
            }
            
            if (hasColorProperty)
            {
                drillBitMaterial.SetColor(colorPropertyName, originalColor);
            }
            
            drillBitRenderer.materials = drillBitMaterials;
        }
    }

    void OnApplicationQuit()
    {
        ReleaseFingerLock();
    }

    void ReleaseFingerLock()
    {
        if (internalGlove != null && internalGlove.IsConnected())
        {
            try
            {
                float[] ffb = new float[5]; // All zeros
                internalGlove.QueueFFBLevels(ffb);
                internalGlove.SendHaptics();
                internalGlove.StopHaptics();
                
                if (enableDebugLogs)
                {
                    Debug.Log("[DrillHeatSystem] Finger lock released on shutdown.");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[DrillHeatSystem] Error releasing finger lock: {e.Message}");
            }
        }
    }

    void TestForceFeeback()
    {
        if (internalGlove == null || !internalGlove.IsConnected())
        {
            Debug.LogError("Cannot test - glove not connected!");
            return;
        }

        Debug.Log("\n========== TESTING FORCE FEEDBACK ==========");
        Debug.Log("Testing each finger individually...\n");

        string[] fingerNames = { "THUMB", "INDEX", "MIDDLE", "RING", "PINKY" };

        for (int i = 0; i < 5; i++)
        {
            try
            {
                float[] ffb = new float[5];
                ffb[i] = 1f; // 100% force

                internalGlove.QueueFFBLevels(ffb);
                internalGlove.SendHaptics();

                Debug.Log($"🔴 {fingerNames[i]} - Sending 100% force. Do you feel it?");
                
                // Wait 2 seconds
                for (int wait = 0; wait < 20; wait++)
                {
                    System.Threading.Thread.Sleep(100);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error testing {fingerNames[i]}: {e.Message}");
            }
        }

        // Release all
        float[] releaseFFB = new float[5];
        internalGlove.QueueFFBLevels(releaseFFB);
        internalGlove.SendHaptics();

        Debug.Log("\n========== TEST COMPLETE ==========");
        Debug.Log("Which fingers did you feel force on?");
    }
}