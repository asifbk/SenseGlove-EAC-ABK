using UnityEngine;
using TMPro;
using SG;

public class SyncedFlexionForceFeedback : MonoBehaviour
{
    [Header("Hand References")]
    public SG_TrackedHand leftHand;
    public SG_TrackedHand rightHand;

    [Header("UI Display")]
    public TextMeshProUGUI leftHandText;
    public TextMeshProUGUI rightHandText;

    [Header("Finger Control Settings")]
    public bool enableManualControl = false;

    [Header("Thumb Settings")]
    public bool thumbEnabled = true;
    [Range(0f, 1f)] public float thumbFlexionThreshold = 0.5f;
    [Range(0f, 100f)] public float thumbForceLevel = 100f;

    [Header("Index Settings")]
    public bool indexEnabled = true;
    [Range(0f, 1f)] public float indexFlexionThreshold = 0.5f;
    [Range(0f, 100f)] public float indexForceLevel = 100f;

    [Header("Middle Settings")]
    public bool middleEnabled = true;
    [Range(0f, 1f)] public float middleFlexionThreshold = 0.5f;
    [Range(0f, 100f)] public float middleForceLevel = 100f;

    [Header("Ring Settings")]
    public bool ringEnabled = true;
    [Range(0f, 1f)] public float ringFlexionThreshold = 0.5f;
    [Range(0f, 100f)] public float ringForceLevel = 100f;

    [Header("Pinky Settings")]
    public bool pinkyEnabled = true;
    [Range(0f, 1f)] public float pinkyFlexionThreshold = 0.5f;
    [Range(0f, 100f)] public float pinkyForceLevel = 100f;

    [Header("Tolerance Settings")]
    [Range(0f, 0.3f)] public float flexionTolerance = 0.1f;  // Allows for haptic system drift

    [Header("Update Settings")]
    public float updateInterval = 0.1f;
    private float lastUpdateTime = 0f;

    [Header("Status")]
    public bool leftHandConnected = false;
    public bool rightHandConnected = false;

    private SG_HapticGlove leftGloveWrapper;
    private SG_HapticGlove rightGloveWrapper;
    
    private SGCore.HapticGlove leftGlove;
    private SGCore.HapticGlove rightGlove;

    // Store current force feedback state for display
    private float[] currentLeftFFB = new float[5];
    private float[] currentRightFFB = new float[5];
    private float[] currentLeftFlexions = new float[5];
    private float[] currentRightFlexions = new float[5];

    void Start()
    {
        if (leftHand != null)
            leftGloveWrapper = leftHand.GetComponent<SG_HapticGlove>();
        
        if (rightHand != null)
            rightGloveWrapper = rightHand.GetComponent<SG_HapticGlove>();
    }

    void Update()
    {
        leftHandConnected = leftHand != null && leftHand.IsConnected();
        rightHandConnected = rightHand != null && rightHand.IsConnected();

        if (leftGlove == null && leftGloveWrapper != null && leftHandConnected)
            leftGlove = (SGCore.HapticGlove)leftGloveWrapper.InternalGlove;
            
        if (rightGlove == null && rightGloveWrapper != null && rightHandConnected)
            rightGlove = (SGCore.HapticGlove)rightGloveWrapper.InternalGlove;

        HandleGloveFeedback(leftHand, leftGlove, currentLeftFFB);
        HandleGloveFeedback(rightHand, rightGlove, currentRightFFB);

        // Update display at set interval
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            if (leftHand != null)
                UpdateFlexDisplay(leftHand, leftHandText, "Left", currentLeftFFB, currentLeftFlexions);

            if (rightHand != null)
                UpdateFlexDisplay(rightHand, rightHandText, "Right", currentRightFFB, currentRightFlexions);

            lastUpdateTime = Time.time;
        }
    }

    void HandleGloveFeedback(SG_TrackedHand hand, SGCore.HapticGlove glove)
    {
        HandleGloveFeedback(hand, glove, null);
    }

    void HandleGloveFeedback(SG_TrackedHand hand, SGCore.HapticGlove glove, float[] ffbArray)
    {
        if (glove == null || !glove.IsConnected()) return;
        
        if (hand.GetNormalizedFlexion(out float[] flexions) && flexions.Length >= 5)
        {
            // Store flexion data
            if (ffbArray != null)
                System.Array.Copy(flexions, 0, 
                    ffbArray == currentLeftFFB ? currentLeftFlexions : currentRightFlexions, 0, 5);

            float[] ffb = new float[5];

            if (!enableManualControl)
            {
                // AUTO MODE: Lock finger when flexion exceeds threshold
                ffb[0] = thumbEnabled && flexions[0] >= (thumbFlexionThreshold - flexionTolerance) 
                    ? thumbForceLevel / 100f : 0f;
                
                ffb[1] = indexEnabled && flexions[1] >= (indexFlexionThreshold - flexionTolerance) 
                    ? indexForceLevel / 100f : 0f;
                
                ffb[2] = middleEnabled && flexions[2] >= (middleFlexionThreshold - flexionTolerance) 
                    ? middleForceLevel / 100f : 0f;
                
                ffb[3] = ringEnabled && flexions[3] >= (ringFlexionThreshold - flexionTolerance) 
                    ? ringForceLevel / 100f : 0f;
                
                ffb[4] = pinkyEnabled && flexions[4] >= (pinkyFlexionThreshold - flexionTolerance) 
                    ? pinkyForceLevel / 100f : 0f;
            }
            else
            {
                // MANUAL MODE: Always apply set force level when enabled
                ffb[0] = thumbEnabled ? thumbForceLevel / 100f : 0f;
                ffb[1] = indexEnabled ? indexForceLevel / 100f : 0f;
                ffb[2] = middleEnabled ? middleForceLevel / 100f : 0f;
                ffb[3] = ringEnabled ? ringForceLevel / 100f : 0f;
                ffb[4] = pinkyEnabled ? pinkyForceLevel / 100f : 0f;
            }

            // Store FFB data for display
            if (ffbArray != null)
                System.Array.Copy(ffb, 0, ffbArray, 0, 5);

            glove.QueueFFBLevels(ffb);
            glove.SendHaptics();
        }
    }

    private void UpdateFlexDisplay(SG_TrackedHand hand, TextMeshProUGUI displayText, 
        string handLabel, float[] ffbArray, float[] flexionArray)
    {
        if (hand.GetNormalizedFlexion(out float[] flexions) && flexions.Length >= 5)
        {
            System.Array.Copy(flexions, 0, flexionArray, 0, 5);

            string text = $"<b>{handLabel} Hand</b>\n";
            text += GetFingerStatus("Thumb", flexions[0], ffbArray[0], thumbFlexionThreshold, thumbForceLevel);
            text += GetFingerStatus("Index", flexions[1], ffbArray[1], indexFlexionThreshold, indexForceLevel);
            text += GetFingerStatus("Middle", flexions[2], ffbArray[2], middleFlexionThreshold, middleForceLevel);
            text += GetFingerStatus("Ring", flexions[3], ffbArray[3], ringFlexionThreshold, ringForceLevel);
            text += GetFingerStatus("Pinky", flexions[4], ffbArray[4], pinkyFlexionThreshold, pinkyForceLevel);

            if (displayText != null)
                displayText.text = text;
        }
        else if (displayText != null)
        {
            displayText.text = $"{handLabel} Hand not connected or invalid data.";
        }
    }

    private string GetFingerStatus(string fingerName, float flexion, float forceFeedback, 
        float threshold, float forceLevel)
    {
        bool isLocked = forceFeedback > 0f;
        string status = isLocked ? "🔒" : "🔓";
        
        return $"{status} {fingerName}: {flexion:F2} (Threshold: {threshold:F2}) " +
               $"| Force: {forceLevel:F0}/100\n";
    }

    void OnDisable()
    {
        StopAllForceFeedback();
    }

    void OnDestroy()
    {
        StopAllForceFeedback();
    }

    void OnApplicationQuit()
    {
        StopAllForceFeedback();
    }

    private void StopAllForceFeedback()
    {
        if (leftGlove != null) leftGlove.StopHaptics();
        if (rightGlove != null) rightGlove.StopHaptics();
    }
}