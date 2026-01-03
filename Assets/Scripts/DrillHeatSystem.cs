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
    [Tooltip("Force feedback level for index finger lock (0 = no force, 100 = maximum force)")]
    [Range(0, 100)] public int indexFingerLockForce = 100;
    [Tooltip("Temperature at which finger lock releases (must be lower than lock temperature 100)")]
    [Range(0, 100)] public float fingerLockReleaseThreshold = 70f;

    [Header("Battery Integration")]
    public DrillBatterySystem batterySystem;

    private SG_TriggerLogic triggerLogic;
    private SG_HapticGlove hapticGlove;
    private SGCore.HapticGlove internalGlove;
    private bool isOverheating = false;
    private bool isBurning = false;
    private bool isFingerLocked = false;
    private bool isSafetyLocked = false;
    private bool lastFFBState = false; // Track last FFB state to avoid repeated sends
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
    }

    void Update()
    {
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
                
                if (adjustedPercent < 0.5f)
                    targetColor = Color.Lerp(warmColor, hotColor, adjustedPercent * 2f);
                else
                    targetColor = hotColor;
            }

            if (hasEmission)
            {
                float emissionIntensity = currentHeat < warmColorThreshold ? 0f : ((heatPercent - warmPercent) / (1f - warmPercent));
                drillBitMaterial.SetColor("_EmissionColor", targetColor * emissionIntensity * maxEmissionIntensity);
                drillBitMaterial.EnableKeyword("_EMISSION");
            }
            
            if (hasColorProperty)
            {
                drillBitMaterial.SetColor(colorPropertyName, targetColor);
                
                if (enableDebugLogs && Time.frameCount % 60 == 0)
                {
                    Debug.Log($"<color=orange>[Heat Visual] Setting {colorPropertyName} to {targetColor} (Heat: {currentHeat:F1}°C)</color>");
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
            internalGlove = (SGCore.HapticGlove)hapticGlove.InternalGlove;
            
            if (enableDebugLogs && internalGlove != null)
            {
                Debug.Log($"<color=cyan>[DrillHeatSystem] ✓ Internal glove acquired! Type: {internalGlove.GetType().Name}</color>");
            }
        }

        // Check if we have haptic glove
        if (hapticGlove == null)
        {
            if (enableDebugLogs && Time.frameCount % 120 == 0)
            {
                Debug.LogWarning($"[DrillHeatSystem] HapticGlove is null! TrackedHand exists: {trackedHand != null}");
            }
            return;
        }

        // Check connection
        if (internalGlove != null && !internalGlove.IsConnected())
        {
            if (enableDebugLogs && Time.frameCount % 120 == 0)
            {
                Debug.LogWarning($"[DrillHeatSystem] Glove not connected!");
            }
            return;
        }

        bool shouldLock = currentHeat >= maxHeat; // Lock when heat reaches 100 (max)
        
        // Track state changes
        if (shouldLock && !isFingerLocked)
        {
            isFingerLocked = true;
            lastFFBState = false; // Reset to trigger send on next frame
            
            if (enableDebugLogs)
            {
                Debug.Log($"<color=lime>[DrillHeatSystem] 🔒 INDEX FINGER LOCKED! Heat: {currentHeat:F1}°C >= {maxHeat}°C. Force: {indexFingerLockForce}%</color>");
            }
        }
        else if (currentHeat <= fingerLockReleaseThreshold && isFingerLocked)
        {
            isFingerLocked = false;
            lastFFBState = true; // Reset to trigger send on next frame
            
            if (enableDebugLogs)
            {
                Debug.Log($"<color=cyan>[DrillHeatSystem] 🔓 INDEX FINGER RELEASED! Heat: {currentHeat:F1}°C <= {fingerLockReleaseThreshold}°C</color>");
            }
        }

        // ========== CONTINUOUS Force Feedback: Send force every frame while locked ==========
        // Track state changes for logging
        if (isFingerLocked && !lastFFBState)
        {
            lastFFBState = true;
            if (enableDebugLogs)
            {
                Debug.Log($"<color=lime>[Finger Lock] 🔒 LOCK ENGAGED - Continuous force {indexFingerLockForce}% applied</color>");
            }
        }
        else if (!isFingerLocked && lastFFBState)
        {
            lastFFBState = false;
            if (enableDebugLogs)
            {
                Debug.Log($"<color=cyan>[Finger Lock] 🔓 LOCK RELEASED - Force removed</color>");
            }
        }
        
        // SEND FORCE EVERY FRAME while locked
        if (isFingerLocked)
        {
            float forceLevel = indexFingerLockForce / 100f;
            
            hapticGlove.QueueFFBCmd(SGCore.Finger.Index, forceLevel);
            
            if (internalGlove != null && internalGlove.IsConnected())
            {
                float[] ffb = new float[5];
                ffb[1] = forceLevel; // Index finger is at position 1
                internalGlove.QueueFFBLevels(ffb);
                internalGlove.SendHaptics();
            }
        }
        else
        {
            // RELEASE: Send zero force every frame to ensure complete release
            hapticGlove.QueueFFBCmd(SGCore.Finger.Index, 0f);
            
            if (internalGlove != null && internalGlove.IsConnected())
            {
                float[] ffb = new float[5];
                internalGlove.QueueFFBLevels(ffb);
                internalGlove.SendHaptics();
            }
        }
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
            float[] ffb = new float[5];
            internalGlove.QueueFFBLevels(ffb);
            internalGlove.SendHaptics();
            internalGlove.StopHaptics();
        }
    }
}