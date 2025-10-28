using UnityEngine;
using SG;

public class FlexionBasedForceFeedback : MonoBehaviour
{
    [Header("Hand References")]
    public SG_TrackedHand leftHand;
    public SG_TrackedHand rightHand;

    [Header("Finger Control Settings")]
    public bool enableManualControl = false;  // Toggle between auto and manual mode

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

    [Header("Status")]
    public bool leftHandConnected = false;
    public bool rightHandConnected = false;

    private SG_HapticGlove leftGloveWrapper;
    private SG_HapticGlove rightGloveWrapper;
    
    private SGCore.HapticGlove leftGlove;
    private SGCore.HapticGlove rightGlove;

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

        // Get internal glove references
        if (leftGlove == null && leftGloveWrapper != null && leftHandConnected)
            leftGlove = (SGCore.HapticGlove)leftGloveWrapper.InternalGlove;
            
        if (rightGlove == null && rightGloveWrapper != null && rightHandConnected)
            rightGlove = (SGCore.HapticGlove)rightGloveWrapper.InternalGlove;

        // Send force feedback every frame
        HandleGloveFeedback(leftHand, leftGlove);
        HandleGloveFeedback(rightHand, rightGlove);
    }

    void HandleGloveFeedback(SG_TrackedHand hand, SGCore.HapticGlove glove)
    {
        if (glove == null || !glove.IsConnected()) return;
        
        if (hand.GetNormalizedFlexion(out float[] flexions) && flexions.Length >= 5)
        {
            // Build FFB array (convert 0-100 to 0-1 range)
            float[] ffb = new float[5];

            // Handle each finger individually
            if (thumbEnabled && !enableManualControl)
            {
                ffb[0] = flexions[0] >= thumbFlexionThreshold ? thumbForceLevel / 100f : 0f;
            }
            
            if (indexEnabled && !enableManualControl)
            {
                ffb[1] = flexions[1] >= indexFlexionThreshold ? indexForceLevel / 100f : 0f;
            }
            
            if (middleEnabled && !enableManualControl)
            {
                ffb[2] = flexions[2] >= middleFlexionThreshold ? middleForceLevel / 100f : 0f;
            }
            
            if (ringEnabled && !enableManualControl)
            {
                ffb[3] = flexions[3] >= ringFlexionThreshold ? ringForceLevel / 100f : 0f;
            }

            // Pinky is always in auto mode (uses thumb's settings as default)
            ffb[4] = flexions[4] >= thumbFlexionThreshold ? thumbForceLevel / 100f : 0f;

            // In manual mode, directly use force levels
            if (enableManualControl)
            {
                ffb[0] = thumbEnabled ? thumbForceLevel / 100f : 0f;
                ffb[1] = indexEnabled ? indexForceLevel / 100f : 0f;
                ffb[2] = middleEnabled ? middleForceLevel / 100f : 0f;
                ffb[3] = ringEnabled ? ringForceLevel / 100f : 0f;
                ffb[4] = thumbForceLevel / 100f; // Pinky uses thumb settings
            }

            // Queue and send
            glove.QueueFFBLevels(ffb);
            glove.SendHaptics();
        }
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