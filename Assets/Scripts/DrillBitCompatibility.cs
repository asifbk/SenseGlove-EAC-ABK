using UnityEngine;

public class DrillBitCompatibility : MonoBehaviour
{
    [Header("Material Compatibility")]
    [Tooltip("The specific material tag this drill bit can drill into")]
    public string compatibleMaterialTag = "";
    
    [Header("Feedback Settings")]
    [Tooltip("Color to display when trying to drill incompatible material")]
    public Color incompatibleColor = new Color(1f, 0.3f, 0.3f, 0.8f);
    
    [Tooltip("Duration to show incompatible material warning")]
    public float warningDisplayDuration = 2f;
    
    private float lastIncompatibleWarningTime = -999f;
    
    public bool IsCompatibleWith(GameObject material)
    {
        if (string.IsNullOrEmpty(compatibleMaterialTag))
        {
            return true;
        }
        
        bool isCompatible = material.CompareTag(compatibleMaterialTag) || 
                           material.layer == LayerMask.NameToLayer(compatibleMaterialTag);
        
        if (!isCompatible)
        {
            if (Time.time - lastIncompatibleWarningTime > warningDisplayDuration)
            {
                Debug.LogWarning($"[{gameObject.name}] Cannot drill {material.name}! This drill bit is designed for {compatibleMaterialTag} only.");
                lastIncompatibleWarningTime = Time.time;
            }
        }
        
        return isCompatible;
    }
    
    public bool IsCompatibleWith(string materialTag, string materialLayer)
    {
        if (string.IsNullOrEmpty(compatibleMaterialTag))
        {
            return true;
        }
        
        bool isCompatible = materialTag == compatibleMaterialTag || materialLayer == compatibleMaterialTag;
        
        return isCompatible;
    }
}
