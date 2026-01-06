using UnityEngine;
using SG;

public class SimpleIndexFingerLockTest : MonoBehaviour
{
    [Header("Hand Reference")]
    public SG_TrackedHand trackedHand;
    
    [Header("Lock Settings")]
    public KeyCode lockKey = KeyCode.Space;
    public KeyCode unlockKey = KeyCode.U;
    [Range(0, 100)]
    public float lockForce = 100f;
    
    [Header("Status")]
    public bool isLocked = false;
    
    private SG_HapticGlove gloveWrapper;
    private SGCore.HapticGlove internalGlove;
    
    void Start()
    {
        if (trackedHand == null)
        {
            Debug.LogError("[SimpleIndexFingerLockTest] TrackedHand not assigned!");
            enabled = false;
            return;
        }
        
        gloveWrapper = trackedHand.GetComponent<SG_HapticGlove>();
        if (gloveWrapper == null)
        {
            Debug.LogError("[SimpleIndexFingerLockTest] SG_HapticGlove not found on TrackedHand!");
            enabled = false;
        }
    }
    
    void Update()
    {
        // Get internal glove if not yet acquired
        if (internalGlove == null && gloveWrapper != null)
        {
            if (trackedHand.IsConnected())
            {
                internalGlove = (SGCore.HapticGlove)gloveWrapper.InternalGlove;
                if (internalGlove != null)
                {
                    Debug.Log($"<color=cyan>[Test] ✓ Internal glove acquired! Type: {internalGlove.GetType().Name}</color>");
                }
            }
        }
        
        // Check connection
        if (internalGlove == null || !internalGlove.IsConnected())
        {
            return;
        }
        
        // Keyboard controls
        if (Input.GetKeyDown(lockKey))
        {
            isLocked = true;
            Debug.Log($"<color=lime>[Test] 🔒 LOCKING INDEX FINGER with {lockForce}% force - Press '{unlockKey}' to release</color>");
        }
        
        if (Input.GetKeyDown(unlockKey))
        {
            isLocked = false;
            Debug.Log($"<color=cyan>[Test] 🔓 UNLOCKING INDEX FINGER</color>");
        }
        
        // Send FFB every frame
        float[] ffb = new float[5];
        ffb[0] = 0f; // Thumb
        ffb[1] = isLocked ? (lockForce / 100f) : 0f; // Index
        ffb[2] = 0f; // Middle
        ffb[3] = 0f; // Ring
        ffb[4] = 0f; // Pinky
        
        internalGlove.QueueFFBLevels(ffb);
        internalGlove.SendHaptics();
        
        // Debug output every second
        if (Time.frameCount % 60 == 0)
        {
            string status = isLocked ? "🔒 LOCKED" : "🔓 Released";
            Debug.Log($"<color=yellow>[Test] {status} | Force: {lockForce}%</color>");
        }
    }
    
    void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.fontSize = 16;
        style.normal.textColor = Color.white;
        style.alignment = TextAnchor.MiddleCenter;
        
        float width = 400f;
        float height = 120f;
        float x = (Screen.width - width) / 2;
        float y = 20f;
        
        GUI.Box(new Rect(x, y, width, height), "", style);
        
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 18;
        labelStyle.fontStyle = FontStyle.Bold;
        labelStyle.alignment = TextAnchor.MiddleCenter;
        labelStyle.normal.textColor = isLocked ? Color.red : Color.green;
        
        string statusText = isLocked ? "🔒 INDEX FINGER LOCKED" : "🔓 Index Finger Released";
        GUI.Label(new Rect(x, y + 10, width, 30), statusText, labelStyle);
        
        labelStyle.fontSize = 14;
        labelStyle.fontStyle = FontStyle.Normal;
        labelStyle.normal.textColor = Color.white;
        
        GUI.Label(new Rect(x, y + 45, width, 25), $"Lock Force: {lockForce}%", labelStyle);
        GUI.Label(new Rect(x, y + 70, width, 25), $"Press '{lockKey}' to LOCK  |  '{unlockKey}' to UNLOCK", labelStyle);
        
        // Connection status
        if (internalGlove == null || !internalGlove.IsConnected())
        {
            labelStyle.normal.textColor = Color.red;
            GUI.Label(new Rect(x, y + 95, width, 20), "⚠️ Glove not connected!", labelStyle);
        }
        else
        {
            labelStyle.fontSize = 12;
            labelStyle.normal.textColor = Color.cyan;
            GUI.Label(new Rect(x, y + 95, width, 20), $"✓ {internalGlove.GetType().Name} connected", labelStyle);
        }
    }
    
    void OnDisable()
    {
        if (internalGlove != null && internalGlove.IsConnected())
        {
            // Release all force
            float[] ffb = new float[5];
            internalGlove.QueueFFBLevels(ffb);
            internalGlove.SendHaptics();
            Debug.Log("[Test] Haptics stopped on disable");
        }
    }
}
