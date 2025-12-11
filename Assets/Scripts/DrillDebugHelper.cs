using UnityEngine;

public class DrillDebugHelper : MonoBehaviour
{
    public DrillHeatSystem heatSystem;
    public Transform[] drillBits;
    
    [Header("Test Settings")]
    [Range(0, 100)] public float testHeat = 75f;
    public bool applyTestHeat = false;
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            DebugAllDrillBits();
        }
        
        if (Input.GetKeyDown(KeyCode.M))
        {
            DebugCurrentMaterial();
        }
        
        if (applyTestHeat && heatSystem != null)
        {
            heatSystem.enableManualHeatControl = true;
            heatSystem.manualHeatValue = testHeat;
        }
    }
    
    void DebugAllDrillBits()
    {
        Debug.Log("=== DRILL BIT ANALYSIS ===");
        
        foreach (Transform bit in drillBits)
        {
            if (bit == null) continue;
            
            CapsuleCollider col = bit.GetComponent<CapsuleCollider>();
            MeshRenderer rend = bit.GetComponent<MeshRenderer>();
            
            if (col != null)
            {
                float effectiveRadius = col.radius * Mathf.Max(bit.lossyScale.x, bit.lossyScale.z);
                float effectiveDiameter = effectiveRadius * 2f * 1000f; // Convert to mm
                
                Debug.Log($"<color=cyan>{bit.name}:</color>\n" +
                          $"  Scale: X={bit.lossyScale.x:F4}, Y={bit.lossyScale.y:F4}, Z={bit.lossyScale.z:F4}\n" +
                          $"  Collider Radius: {col.radius:F4}m\n" +
                          $"  Collider Height: {col.height:F4}m\n" +
                          $"  Collider Direction: {col.direction} (0=X, 1=Y, 2=Z)\n" +
                          $"  Effective Radius: {effectiveRadius:F6}m ({effectiveRadius * 1000f:F2}mm)\n" +
                          $"  <b>Effective Diameter: {effectiveDiameter:F2}mm</b>");
            }
            
            if (rend != null && rend.materials.Length > 0)
            {
                Material mat = rend.materials[0];
                Debug.Log($"  Material: {mat.name}\n" +
                          $"  Shader: {mat.shader.name}\n" +
                          $"  Has _EmissionColor: {mat.HasProperty("_EmissionColor")}\n" +
                          $"  Has _Color: {mat.HasProperty("_Color")}\n" +
                          $"  Current _Color: {(mat.HasProperty("_Color") ? mat.GetColor("_Color").ToString() : "N/A")}");
            }
        }
        
        Debug.Log("=== END ANALYSIS ===");
    }
    
    void DebugCurrentMaterial()
    {
        if (heatSystem == null || heatSystem.drillBitTip == null) 
        {
            Debug.LogWarning("Heat system or drill bit tip is null!");
            return;
        }
        
        MeshRenderer rend = heatSystem.drillBitTip.GetComponent<MeshRenderer>();
        if (rend == null || rend.materials.Length == 0)
        {
            Debug.LogWarning("No renderer or materials found!");
            return;
        }
        
        Material mat = rend.materials[0];
        Debug.Log($"<color=yellow>=== CURRENT DRILL BIT MATERIAL ===</color>\n" +
                  $"Drill Bit: {heatSystem.drillBitTip.name}\n" +
                  $"Heat: {heatSystem.currentHeat:F1}°C\n" +
                  $"Material: {mat.name}\n" +
                  $"Shader: {mat.shader.name}\n" +
                  $"Has _EmissionColor: {mat.HasProperty("_EmissionColor")}\n" +
                  $"Has _Color: {mat.HasProperty("_Color")}\n" +
                  $"_Color value: {(mat.HasProperty("_Color") ? mat.GetColor("_Color").ToString() : "N/A")}\n" +
                  $"_EmissionColor value: {(mat.HasProperty("_EmissionColor") ? mat.GetColor("_EmissionColor").ToString() : "N/A")}");
    }
}
