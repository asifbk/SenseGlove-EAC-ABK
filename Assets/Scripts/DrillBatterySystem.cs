using UnityEngine;

public class DrillBatterySystem : MonoBehaviour
{
    [Header("Battery Settings")]
    [Range(0, 100)] public float currentCharge = 100f;
    public float maxCharge = 100f;
    public float drillConsumptionRate = 2f;
    public float idleConsumptionRate = 0.1f;
    
    [Header("Battery Thresholds")]
    [Range(0, 100)] public float highChargeThreshold = 60f;
    [Range(0, 100)] public float mediumChargeThreshold = 30f;
    
    [Header("Behavior")]
    public bool disableDrillWhenEmpty = true;
    public bool autoRecharge = false;
    public float rechargeRate = 5f;
    
    [Header("References")]
    public BatteryLevelIndicator batteryIndicator;
    
    [Header("Debug")]
    public bool enableDebugLogs = false;
    
    private SG_TriggerLogic triggerLogic;
    private bool isDrilling = false;
    private bool isEmpty = false;

    void Start()
    {
        triggerLogic = GetComponent<SG_TriggerLogic>();
        
        if (batteryIndicator == null)
        {
            batteryIndicator = GetComponentInChildren<BatteryLevelIndicator>();
        }
        
        if (batteryIndicator != null)
        {
            batteryIndicator.UpdateBatteryLevel(GetChargePercentage());
        }
    }

    void Update()
    {
        CheckDrillingState();
        UpdateBatteryCharge();
        UpdateIndicator();
    }

    void CheckDrillingState()
    {
        if (triggerLogic != null)
        {
            isDrilling = triggerLogic.CurrentPressure > 0.1f && !isEmpty;
        }
    }

    void UpdateBatteryCharge()
    {
        if (isEmpty && !autoRecharge)
        {
            return;
        }

        if (autoRecharge && currentCharge < maxCharge)
        {
            currentCharge += rechargeRate * Time.deltaTime;
            
            if (currentCharge >= 1f && isEmpty)
            {
                isEmpty = false;
                
                if (enableDebugLogs)
                {
                    Debug.Log("[DrillBattery] 🔋 Battery recharged! Ready to use.");
                }
            }
        }
        else if (isDrilling)
        {
            currentCharge -= drillConsumptionRate * Time.deltaTime;
            
            if (enableDebugLogs && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[DrillBattery] Drilling... Charge: {currentCharge:F1}%");
            }
        }
        else if (currentCharge > 0)
        {
            currentCharge -= idleConsumptionRate * Time.deltaTime;
        }

        currentCharge = Mathf.Clamp(currentCharge, 0, maxCharge);

        if (currentCharge <= 0 && !isEmpty)
        {
            isEmpty = true;
            
            if (enableDebugLogs)
            {
                Debug.Log("[DrillBattery] 🪫 Battery empty! Drill disabled.");
            }
        }
    }

    void UpdateIndicator()
    {
        if (batteryIndicator != null)
        {
            batteryIndicator.UpdateBatteryLevel(GetChargePercentage());
        }
    }

    public float GetChargePercentage()
    {
        return currentCharge / maxCharge;
    }

    public bool IsEmpty()
    {
        return isEmpty;
    }

    public bool IsLowCharge()
    {
        return currentCharge < mediumChargeThreshold;
    }

    public bool IsMediumCharge()
    {
        return currentCharge >= mediumChargeThreshold && currentCharge < highChargeThreshold;
    }

    public bool IsHighCharge()
    {
        return currentCharge >= highChargeThreshold;
    }

    public void Recharge(float amount)
    {
        currentCharge = Mathf.Min(currentCharge + amount, maxCharge);
        
        if (currentCharge >= 1f && isEmpty)
        {
            isEmpty = false;
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[DrillBattery] Recharged +{amount}%. Current: {currentCharge:F1}%");
        }
    }

    public void FullRecharge()
    {
        currentCharge = maxCharge;
        isEmpty = false;
        
        if (enableDebugLogs)
        {
            Debug.Log("[DrillBattery] 🔋 Fully recharged to 100%!");
        }
    }

    public bool CanDrill()
    {
        if (disableDrillWhenEmpty && isEmpty)
        {
            return false;
        }
        
        return currentCharge > 0;
    }
}
