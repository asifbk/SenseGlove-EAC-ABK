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
        UpdateHandDisplay(leftHand, leftHandText, "LEFT IMU");
        UpdateHandDisplay(rightHand, rightHandText, "RIGHT IMU");
    }

    private void UpdateHandDisplay(SG_TrackedHand hand, TextMeshProUGUI text, string label)
    {
        if (hand == null || text == null)
        {
            if (text != null)
                text.text = $"<color=red>{label} tracking unavailable</color>";
            return;
        }

        // ---------- IMU ROTATION ----------
        Quaternion handRot = hand.transform.rotation;
        Vector3 euler = handRot.eulerAngles;

        // ---------- THUMB ABDUCTION ----------
        float abductionAngle = 0f;

        // Retrieve current hand pose using RealHandPose
        SG_HandPose pose = hand.GetHandPose(SG_TrackedHand.TrackingLevel.RealHandPose);

        if (pose != null && pose.jointAngles != null && pose.jointAngles.Length > 0)
        {
            // jointAngles[0][0].y → Thumb CMC abduction/adduction
            abductionAngle = pose.jointAngles[0][0].y;
        }

        // ---------- DISPLAY ----------
        text.text =
            $"<b><color=#00FFFF>{label}</color></b>\n" +
            $"<size=15>" +
            $"<color=#FFD700>X:</color> {euler.x:F1}°\n" +
            $"<color=#FFD700>Y:</color> {euler.y:F1}°\n" +
            $"<color=#FFD700>Z:</color> {euler.z:F1}°\n" +
            $"<color=#00FF00>Thumb Abd:</color> {abductionAngle:F1}°" +
            $"</size>";
    }
}
