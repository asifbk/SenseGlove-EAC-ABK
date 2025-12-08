using UnityEngine;

public class DrillHeatSystem : MonoBehaviour
{
    [Header("Heat Settings")]
    [Range(0, 100)] public float currentHeat = 0f;
    public float maxHeat = 100f;
    public float heatIncreaseRate = 5f;
    public float coolDownRate = 3f;
    public float warmColorThreshold = 50f;
    public float hotColorThreshold = 90f;

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

    [Header("Material Emission")]
    public bool useEmissionGlow = true;
    public float maxEmissionIntensity = 2f;
    
    [Header("Audio")]
    public AudioSource heatAudioSource;
    public AudioClip overheatingSound;
    public AudioClip sizzleSound;

    [Header("Debug")]
    public bool enableDebugLogs = false;

    [Header("Haptic Feedback")]
    public bool enableHapticLock = true;
    public float normalEndFlexion = 0.5f;
    public float lockedEndFlexion = 0.3f;

    [Header("Battery Integration")]
    public DrillBatterySystem batterySystem;

    private SG_TriggerLogic triggerLogic;
    private bool isOverheating = false;
    private bool isBurning = false;
    private bool isFlexionLocked = false;
    private Material drillBitMaterial;
    private Color originalEmissionColor;
    private bool hasEmission = false;
    private Vector3 smokeParticlesOriginalOffset;
    private Vector3 burningSmellParticlesOriginalOffset;
    private Quaternion smokeParticlesOriginalRotation;
    private Quaternion burningSmellParticlesOriginalRotation;

    void Start()
    {
        triggerLogic = GetComponent<SG_TriggerLogic>();
        
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
            drillBitMaterial = drillBitRenderer.materials[materialIndex];
            
            if (drillBitMaterial.HasProperty("_EmissionColor"))
            {
                hasEmission = true;
                originalEmissionColor = drillBitMaterial.GetColor("_EmissionColor");
            }
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

        if (batterySystem == null)
        {
            batterySystem = GetComponent<DrillBatterySystem>();
        }
    }

    void Update()
    {
        if (triggerLogic != null)
        {
            if (drillBitTip == null || drillBitTip != triggerLogic.CurrentDrillTip)
            {
                drillBitTip = triggerLogic.CurrentDrillTip;
                UpdateDrillBitReferences();
            }

            bool isDrilling = triggerLogic.CurrentPressure > 0.1f;

            if (batterySystem != null && !batterySystem.CanDrill())
            {
                isDrilling = false;
            }

            if (enableDebugLogs && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[DrillHeatSystem] Pressure: {triggerLogic.CurrentPressure:F2}, isDrilling: {isDrilling}, Heat: {currentHeat:F1}");
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

        UpdateVisualEffects();
        UpdateParticleEffects();
        UpdateAudioEffects();
        UpdateHapticLock();
    }

    void UpdateDrillBitReferences()
    {
        if (drillBitTip != null)
        {
            ResetPreviousMaterial();

            drillBitRenderer = drillBitTip.GetComponent<Renderer>();
            
            if (drillBitRenderer != null && drillBitRenderer.materials.Length > materialIndex)
            {
                drillBitMaterial = drillBitRenderer.materials[materialIndex];
                
                if (drillBitMaterial.HasProperty("_EmissionColor"))
                {
                    hasEmission = true;
                    originalEmissionColor = drillBitMaterial.GetColor("_EmissionColor");
                }
                else
                {
                    hasEmission = false;
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

            if (enableDebugLogs)
            {
                Debug.Log($"[DrillHeatSystem] Switched to drill bit: {drillBitTip.name}");
            }
        }
    }

    void ResetPreviousMaterial()
    {
        if (drillBitMaterial != null && hasEmission)
        {
            drillBitMaterial.SetColor("_EmissionColor", originalEmissionColor);
        }
    }

    void UpdateVisualEffects()
    {
        float heatPercent = currentHeat / maxHeat;

        if (drillBitGlow != null)
        {
            drillBitGlow.intensity = Mathf.Lerp(0, maxGlowIntensity, heatPercent);
            
            if (heatPercent < 0.3f)
                drillBitGlow.color = coolColor;
            else if (heatPercent < 0.7f)
                drillBitGlow.color = Color.Lerp(coolColor, warmColor, (heatPercent - 0.3f) / 0.4f);
            else
                drillBitGlow.color = Color.Lerp(warmColor, hotColor, (heatPercent - 0.7f) / 0.3f);
        }

        if (useEmissionGlow && hasEmission && drillBitMaterial != null)
        {
            Color emissionColor;
            
            if (heatPercent < 0.3f)
                emissionColor = originalEmissionColor;
            else if (heatPercent < 0.7f)
                emissionColor = Color.Lerp(originalEmissionColor, warmColor, (heatPercent - 0.3f) / 0.4f);
            else
                emissionColor = Color.Lerp(warmColor, hotColor, (heatPercent - 0.7f) / 0.3f);

            float emissionIntensity = Mathf.Lerp(0, maxEmissionIntensity, heatPercent);
            drillBitMaterial.SetColor("_EmissionColor", emissionColor * emissionIntensity);
            drillBitMaterial.EnableKeyword("_EMISSION");
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

    void UpdateAudioEffects()
    {
        if (heatAudioSource == null) return;

        if (isOverheating && overheatingSound != null && !heatAudioSource.isPlaying)
        {
            heatAudioSource.clip = overheatingSound;
            heatAudioSource.loop = true;
            heatAudioSource.Play();
        }
        else if (!isOverheating && heatAudioSource.isPlaying && heatAudioSource.clip == overheatingSound)
        {
            heatAudioSource.Stop();
        }

        if (isBurning)
        {
            float volume = Mathf.Lerp(0.1f, 0.5f, (currentHeat - warmColorThreshold) / (maxHeat - warmColorThreshold));
            heatAudioSource.volume = volume;
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

    void UpdateHapticLock()
    {
        if (!enableHapticLock || triggerLogic == null) return;

        if (currentHeat >= hotColorThreshold && !isFlexionLocked)
        {
            isFlexionLocked = true;
            triggerLogic.endFlexion = lockedEndFlexion;
            
            if (enableDebugLogs)
            {
                Debug.Log($"[DrillHeatSystem] 🔥 OVERHEATED! Flexion locked at {lockedEndFlexion}");
            }
        }
        else if (currentHeat < warmColorThreshold && isFlexionLocked)
        {
            isFlexionLocked = false;
            triggerLogic.endFlexion = normalEndFlexion;
            
            if (enableDebugLogs)
            {
                Debug.Log($"[DrillHeatSystem] ❄️ Cooled down. Flexion restored to {normalEndFlexion}");
            }
        }
    }

    void OnDestroy()
    {
        if (drillBitMaterial != null && hasEmission)
        {
            drillBitMaterial.SetColor("_EmissionColor", originalEmissionColor);
        }
    }
}
