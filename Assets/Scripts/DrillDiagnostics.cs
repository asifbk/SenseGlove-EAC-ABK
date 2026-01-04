using UnityEngine;

public class DrillDiagnostics : MonoBehaviour
{
    public SG_TriggerLogic triggerLogic;
    public float checkInterval = 1f;
    
    private float lastCheckTime;
    
    void Update()
    {
        if (Time.time - lastCheckTime < checkInterval) return;
        lastCheckTime = Time.time;
        
        if (triggerLogic == null)
        {
            triggerLogic = FindObjectOfType<SG_TriggerLogic>();
            if (triggerLogic == null)
            {
                Debug.LogError("[DrillDiagnostics] Cannot find SG_TriggerLogic in scene!");
                return;
            }
        }
        
        string drillTipName = triggerLogic.drillTip != null ? triggerLogic.drillTip.name : "NULL";
        float rayDist = triggerLogic.rayDistance;
        int layerMask = triggerLogic.carvableLayer.value;
        
        string layersIncluded = "";
        for (int i = 0; i < 32; i++)
        {
            if ((layerMask & (1 << i)) != 0)
            {
                if (layersIncluded.Length > 0) layersIncluded += ", ";
                layersIncluded += LayerMask.LayerToName(i);
            }
        }
        
        Debug.Log($"<color=cyan>[DrillDiagnostics]</color>\n" +
                  $"  Drill Tip: {drillTipName}\n" +
                  $"  Ray Distance: {rayDist}m ({rayDist * 100f}cm)\n" +
                  $"  Carvable Layers: [{layersIncluded}]\n" +
                  $"  Layer Mask Value: {layerMask}");
        
        GameObject[] materials = new GameObject[]
        {
            GameObject.Find("Wood"),
            GameObject.Find("Brass"),
            GameObject.Find("Concrete")
        };
        
        foreach (GameObject mat in materials)
        {
            if (mat != null)
            {
                string layerName = LayerMask.LayerToName(mat.layer);
                bool isInMask = (layerMask & (1 << mat.layer)) != 0;
                string status = isInMask ? "<color=green>✓ INCLUDED</color>" : "<color=red>✗ EXCLUDED</color>";
                Debug.Log($"  {mat.name}: Layer '{layerName}' (#{mat.layer}) {status}");
            }
        }
    }
}
