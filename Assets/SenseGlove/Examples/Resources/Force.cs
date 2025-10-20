using UnityEngine;
using TMPro;
using SG;

public class DualHandFlexionDisplay : MonoBehaviour
{
    [Header("Hand References")]
    public SG_TrackedHand leftHand;
    public SG_TrackedHand rightHand;

    [Header("UI Elements")]
    public TextMeshProUGUI leftHandText;
    public TextMeshProUGUI rightHandText;

    [Header("Update Settings")]
    public float updateInterval = 0.1f;
    private float lastUpdateTime = 0f;

    void Update()
    {
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            if (leftHand != null)
                UpdateFlexDisplay(leftHand, leftHandText, "Left");

            if (rightHand != null)
                UpdateFlexDisplay(rightHand, rightHandText, "Right");

            lastUpdateTime = Time.time;
        }
    }

    private void UpdateFlexDisplay(SG_TrackedHand hand, TextMeshProUGUI displayText, string handLabel)
    {
        if (hand.GetNormalizedFlexion(out float[] flexions) && flexions.Length >= 5)
        {
            string text = $"{handLabel} Hand Flexions:\n" +
                          $"Thumb: {flexions[0]:F2}" +
                          $"Index: {flexions[1]:F2}\n" +
                          $"Middle: {flexions[2]:F2}" +
                          $"Ring: {flexions[3]:F2}\n" +
                          $"Pinky: {flexions[4]:F2}";

            if (displayText != null)
                displayText.text = text;
        }
        else if (displayText != null)
        {
            displayText.text = $"{handLabel} Hand not connected or invalid data.";
        }
    }
}