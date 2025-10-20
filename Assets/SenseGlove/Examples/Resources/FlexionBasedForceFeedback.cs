using UnityEngine;
using SG;

public class FlexionBasedForceFeedback : MonoBehaviour
{
    [Header("Hand References")]
    public SG_TrackedHand leftHand;   // Assign Left SG_TrackedHand
    public SG_TrackedHand rightHand;  // Assign Right SG_TrackedHand

    [Header("Glove References")]
    public SG_HapticGlove leftGlove;   // Assign Left SG_HapticGlove
    public SG_HapticGlove rightGlove;  // Assign Right SG_HapticGlove

    [Header("Force Feedback Settings")]
    [Range(0f, 1f)]
    public float flexionThresholdMin = 0.5f;  // Flexion value where FF starts
    [Range(0f, 1f)]
    public float flexionThresholdMax = 1.0f;  // Flexion value where FF is maximum
    
    [Range(0f, 1f)]
    public float minForceLevel = 0f;      // Minimum force feedback (0.0 - 1.0)
    [Range(0f, 1f)]
    public float maxForceLevel = 1f;      // Maximum force feedback (0.0 - 1.0)

    [Header("Update Settings")]
    public float updateInterval = 0.05f;
    private float lastUpdateTime = 0f;

    // Arrays to store force levels for each finger (0.0 to 1.0)
    private float[] leftForceLevels = new float[5];
    private float[] rightForceLevels = new float[5];

    void Update()
    {
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            if (leftHand != null && leftGlove != null)
                UpdateForceFeedback(leftHand, leftGlove, leftForceLevels);

            if (rightHand != null && rightGlove != null)
                UpdateForceFeedback(rightHand, rightGlove, rightForceLevels);

            lastUpdateTime = Time.time;
        }
    }

    private void UpdateForceFeedback(SG_TrackedHand hand, SG_HapticGlove glove, float[] forceLevels)
    {
        if (hand.GetNormalizedFlexion(out float[] flexions) && flexions.Length >= 5)
        {
            // Calculate force feedback for each finger based on flexion
            for (int i = 0; i < 5; i++)
            {
                forceLevels[i] = CalculateForceLevel(flexions[i]);
            }

            // Send force feedback to the glove using the IHandFeedbackDevice interface
            // QueueFFBCmd expects float[] with values from 0.0 to 1.0
            glove.QueueFFBCmd(forceLevels);
        }
    }

    private float CalculateForceLevel(float flexionValue)
    {
        // Map flexion value to force feedback level (0.0 to 1.0)
        if (flexionValue <= flexionThresholdMin)
        {
            return minForceLevel;
        }
        else if (flexionValue >= flexionThresholdMax)
        {
            return maxForceLevel;
        }
        else
        {
            // Linear interpolation between min and max
            float normalizedFlex = (flexionValue - flexionThresholdMin) / 
                                   (flexionThresholdMax - flexionThresholdMin);
            
            return Mathf.Lerp(minForceLevel, maxForceLevel, normalizedFlex);
        }
    }

    // Manual control - set force for specific finger using the SGCore.Finger enum
    public void SetForceForFinger(bool isLeftHand, SGCore.Finger finger, float forceLevel)
    {
        forceLevel = Mathf.Clamp01(forceLevel); // Ensure 0.0 to 1.0 range
        
        if (isLeftHand && leftGlove != null)
        {
            leftGlove.QueueFFBCmd(finger, forceLevel);
        }
        else if (!isLeftHand && rightGlove != null)
        {
            rightGlove.QueueFFBCmd(finger, forceLevel);
        }
    }

    // Stop all force feedback
    public void StopAllForceFeedback()
    {
        float[] zeroForces = new float[5] { 0f, 0f, 0f, 0f, 0f };
        
        if (leftGlove != null)
            leftGlove.QueueFFBCmd(zeroForces);
        
        if (rightGlove != null)
            rightGlove.QueueFFBCmd(zeroForces);
    }

    void OnDisable()
    {
        // Stop force feedback when script is disabled
        StopAllForceFeedback();
    }

    void OnDestroy()
    {
        // Stop force feedback when script is destroyed
        StopAllForceFeedback();
    }
}