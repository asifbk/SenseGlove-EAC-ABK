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
        Debug.LogWarning("DrillBitCompatibility: Checking compatibility with material: " + material.name);
        if (string.IsNullOrEmpty(compatibleMaterialTag))
        {
            return true;
        }
        
        string materialTag = material.tag;
        string materialLayerName = LayerMask.LayerToName(material.layer);
        
        bool tagMatch = material.CompareTag(compatibleMaterialTag);
        bool layerMatch = material.layer == LayerMask.NameToLayer(compatibleMaterialTag);
        bool isCompatible = tagMatch || layerMatch;
        
        if (!isCompatible)
        {
            if (Time.time - lastIncompatibleWarningTime > warningDisplayDuration)
            {
                Debug.LogWarning($"<color=red>[{gameObject.name}] ✗ Cannot drill {material.name}!</color>\n" +
                                $"  This drill bit requires: '{compatibleMaterialTag}'\n" +
                                $"  Material has tag: '{materialTag}' (Match: {tagMatch})\n" +
                                $"  Material has layer: '{materialLayerName}' (Match: {layerMatch})");
                lastIncompatibleWarningTime = Time.time;
            }
        }
        else
        {
            Debug.Log($"<color=green>[{gameObject.name}] ✓ Compatible with {material.name}</color>\n" +
                     $"  Required: '{compatibleMaterialTag}' | Tag: '{materialTag}' | Layer: '{materialLayerName}'");
        }
        
        return isCompatible;
    }
    
    public bool IsCompatibleWith(string materialTag, string materialLayer)
    {
        Debug.LogWarning("DrillBitCompatibility: Checking compatibility with material tag: " + materialTag + " and layer: " + materialLayer);
        if (string.IsNullOrEmpty(compatibleMaterialTag))
        {
            return true;
        }
        
        bool isCompatible = materialTag == compatibleMaterialTag || materialLayer == compatibleMaterialTag;
        
        return isCompatible;
    }
}
