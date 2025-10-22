using UnityEngine;
using SG;

public class FlexionBasedForceFeedback : MonoBehaviour
{
    [Header("Hand References")]
    public SG_TrackedHand leftHand;
    public SG_TrackedHand rightHand;

    [Header("Force Feedback Settings")]
    [Range(0f, 1f)]
    [Tooltip("Flexion threshold - force activates when flexion >= this value")]
    public float flexionThreshold = 0.5f;
    
    [Range(0f, 100f)]
    [Tooltip("Force level (0-100) to apply when threshold is reached")]
    public float forceLevel = 100f;

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
            // Build FFB array (convert 0-100 to 0-1 like diagnostics does)
            float[] ffb = new float[5];
            for (int i = 0; i < 5; i++)
            {
                // When flexion >= threshold, apply force, otherwise 0
                if (flexions[i] >= flexionThreshold)
                    ffb[i] = forceLevel / 100f;  // Convert to 0-1 range
                else
                    ffb[i] = 0f;
            }

            // Queue and send (exactly like diagnostics script)
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