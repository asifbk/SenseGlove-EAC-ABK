using UnityEngine;
using TMPro;
using SG;   // SenseGlove namespace

public class DualGloveRotationDisplay : MonoBehaviour
{
    [Header("SenseGlove References")]
    public SG_TrackedHand leftHand;       // Assign Left SG_TrackedHand
    public SG_TrackedHand rightHand;      // Assign Right SG_TrackedHand

    [Header("UI References")]
    public TextMeshProUGUI leftHandText;  // Assign TMP text for left hand
    public TextMeshProUGUI rightHandText; // Assign TMP text for right hand

    void Update()
    {
        // Update left glove rotation
        if (leftHand != null && leftHandText != null)
        {
            Quaternion leftRot = leftHand.transform.rotation;
            Vector3 leftEuler = leftRot.eulerAngles;

            leftHandText.text =
                $"<b><color=#00FFFF>LEFT IMU</color></b>\n" +
                $"<size=20><color=#FFD700>X:</color> {leftEuler.x:F1}°\n" +
                $"<color=#FFD700>Y:</color> {leftEuler.y:F1}°\n" +
                $"<color=#FFD700>Z:</color> {leftEuler.z:F1}°</size>";
        }
        else if (leftHandText != null)
        {
            leftHandText.text = "<color=red>Left hand tracking unavailable</color>";
        }

        // Update right glove rotation
        if (rightHand != null && rightHandText != null)
        {
            Quaternion rightRot = rightHand.transform.rotation;
            Vector3 rightEuler = rightRot.eulerAngles;

            rightHandText.text =
                $"<b><color=#00FFFF>RIGHT IMU</color></b>\n" +
                $"<size=20><color=#FFD700>X:</color> {rightEuler.x:F1}°\n" +
                $"<color=#FFD700>Y:</color> {rightEuler.y:F1}°\n" +
                $"<color=#FFD700>Z:</color> {rightEuler.z:F1}°</size>";
        }
        else if (rightHandText != null)
        {
            rightHandText.text = "<color=red>Right hand tracking unavailable</color>";
        }
    }
}
