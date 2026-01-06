using UnityEngine;

public class HeatLockDiagnostic : MonoBehaviour
{
    public DrillHeatSystem heatSystem;
    public KeyCode testLockKey = KeyCode.L;
    public KeyCode manualHeatUpKey = KeyCode.H;
    public KeyCode manualCoolDownKey = KeyCode.C;
    
    [Header("Manual Heat Adjustment")]
    public float manualHeatStep = 10f;
    
    void Update()
    {
        if (heatSystem == null) return;
        
        if (Input.GetKeyDown(testLockKey))
        {
            TestFingerLock();
        }
        
        if (Input.GetKeyDown(manualHeatUpKey))
        {
            heatSystem.currentHeat = Mathf.Min(heatSystem.currentHeat + manualHeatStep, heatSystem.maxHeat);
            Debug.Log($"<color=yellow>[Diagnostic] Manual heat UP to {heatSystem.currentHeat}°C</color>");
        }
        
        if (Input.GetKeyDown(manualCoolDownKey))
        {
            heatSystem.currentHeat = Mathf.Max(heatSystem.currentHeat - manualHeatStep, 0);
            Debug.Log($"<color=cyan>[Diagnostic] Manual heat DOWN to {heatSystem.currentHeat}°C</color>");
        }
    }
    
    void OnGUI()
    {
        if (heatSystem == null) return;
        
        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.fontSize = 16;
        style.normal.textColor = Color.white;
        style.alignment = TextAnchor.UpperLeft;
        
        float barWidth = 300f;
        float barHeight = 30f;
        float margin = 10f;
        
        GUI.Box(new Rect(margin, margin, barWidth + 20, 150), "Heat Lock Diagnostic", style);
        
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 14;
        labelStyle.normal.textColor = Color.white;
        
        float y = margin + 30;
        
        GUI.Label(new Rect(margin + 10, y, 280, 20), 
            $"Heat: {heatSystem.currentHeat:F1}°C / {heatSystem.maxHeat}°C", labelStyle);
        y += 25;
        
        GUI.Box(new Rect(margin + 10, y, barWidth, barHeight), "");
        
        Color barColor = Color.green;
        if (heatSystem.currentHeat >= heatSystem.hotColorThreshold)
            barColor = Color.red;
        else if (heatSystem.currentHeat >= heatSystem.warmColorThreshold)
            barColor = Color.yellow;
        
        float fillWidth = (heatSystem.currentHeat / heatSystem.maxHeat) * barWidth;
        GUI.color = barColor;
        GUI.Box(new Rect(margin + 10, y, fillWidth, barHeight), "");
        GUI.color = Color.white;
        
        y += barHeight + 10;
        
        string lockStatus = heatSystem.IsFingerLocked() ? "🔒 LOCKED" : "🔓 Released";
        Color statusColor = heatSystem.IsFingerLocked() ? Color.red : Color.green;
        
        GUIStyle statusStyle = new GUIStyle(labelStyle);
        statusStyle.fontSize = 16;
        statusStyle.fontStyle = FontStyle.Bold;
        statusStyle.normal.textColor = statusColor;
        
        GUI.Label(new Rect(margin + 10, y, 280, 25), 
            $"Status: {lockStatus}", statusStyle);
        y += 30;
        
        labelStyle.fontSize = 12;
        GUI.Label(new Rect(margin + 10, y, 280, 20), 
            $"Press H: Heat +{manualHeatStep}°  |  C: Cool -{manualHeatStep}°  |  L: Test Lock", labelStyle);
    }
    
    void TestFingerLock()
    {
        Debug.Log("\n========== FINGER LOCK TEST ==========");
        Debug.Log($"Current Heat: {heatSystem.currentHeat}°C");
        Debug.Log($"Max Heat (Lock Threshold): {heatSystem.maxHeat}°C");
        Debug.Log($"Release Threshold: {heatSystem.fingerLockReleaseThreshold}°C");
        Debug.Log($"Finger Lock Enabled: {heatSystem.enableFingerLock}");
        Debug.Log($"Is Finger Locked: {heatSystem.IsFingerLocked()}");
        Debug.Log($"Lock Force: {heatSystem.lockForce}%");
        
        if (heatSystem.enableFingerLock)
        {
            if (heatSystem.currentHeat >= heatSystem.maxHeat)
            {
                Debug.Log("<color=lime>✓ Heat is at max - finger SHOULD be locked!</color>");
            }
            else
            {
                Debug.Log($"<color=yellow>Heat needs to reach {heatSystem.maxHeat}°C to lock (currently {heatSystem.currentHeat:F1}°C)</color>");
            }
        }
        else
        {
            Debug.LogWarning("Finger lock is DISABLED in settings!");
        }
        
        Debug.Log("=====================================\n");
    }
}
