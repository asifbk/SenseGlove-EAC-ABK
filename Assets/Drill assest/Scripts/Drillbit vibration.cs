using UnityEngine;
using SG;
using SGCore;
using SGCore.Nova;

[RequireComponent(typeof(SG_Grabable))]
public class DrillBitVibration : MonoBehaviour
{
    [Header("Drill Machine Reference")]
    [Tooltip("The SG_TriggerLogic component on the drill machine")]
    public SG_TriggerLogic drillMachine;

    [Header("Waveform Amplitudes per Channel (0–1)")]
    [Tooltip("Set higher values for larger drill bits, lower for smaller bits")]
    [Range(0f, 1f)] public float thumbAmplitude = 0.3f;
    [Range(0f, 1f)] public float indexAmplitude = 0.6f;
    [Range(0f, 1f)] public float wristAmplitude = 0.8f;

    [Header("Base Waveforms")]
    public SG_CustomWaveform thumbWaveform;
    public SG_CustomWaveform indexWaveform;
    public SG_CustomWaveform wristWaveform;

    [Header("Which Hand?")]
    public SG.HandSide connectsTo = SG.HandSide.LeftHand;

    [Header("Vibration Settings")]
    [Tooltip("Minimum pressure required to start vibration")]
    public float minPressureThreshold = 0.05f;
    [Tooltip("Multiply pressure by this factor for final amplitude")]
    public float pressureMultiplier = 1.0f;

    private SG_Grabable grabable;
    private bool isActive = false;
    private float lastSendTime = 0f;
    private const float sendInterval = 0.1f; // Send updates every 100ms

    void Start()
    {
        grabable = GetComponent<SG_Grabable>();
        
        if (drillMachine == null)
        {
            // Try to find drill machine automatically
            drillMachine = FindObjectOfType<SG_TriggerLogic>();
            if (drillMachine != null)
            {
                Debug.Log($"[DrillBitVibration] Auto-found drill machine: {drillMachine.name}");
            }
            else
            {
                Debug.LogError($"[DrillBitVibration] No SG_TriggerLogic found! Assign manually.");
            }
        }
    }

    void Update()
    {
        if (drillMachine == null || grabable == null) return;

        // Check if this drill bit is the active one
        bool wasActive = isActive;
        isActive = drillMachine.CurrentDrillTip == transform && drillMachine.IsGrabbed;

        // Just became active
        if (isActive && !wasActive)
        {
            Debug.Log($"<color=green>[DrillBitVibration] {gameObject.name} became active</color>");
        }

        // Just became inactive
        if (!isActive && wasActive)
        {
            Debug.Log($"<color=red>[DrillBitVibration] {gameObject.name} became inactive</color>");
            StopVibrations();
        }

        // Update vibrations if active
        if (isActive && Time.time - lastSendTime >= sendInterval)
        {
            lastSendTime = Time.time;
            UpdateVibrations();
        }
    }

    private void UpdateVibrations()
    {
        float pressure = drillMachine.CurrentPressure;

        if (pressure < minPressureThreshold)
        {
            StopVibrations();
            return;
        }

        // Calculate global amplitude based on pressure
        float globalAmp = Mathf.Clamp01(pressure * pressureMultiplier);

        // Get the glove
        HapticGlove glove = GetGloveForSide(connectsTo);
        if (glove != null && glove.IsConnected())
        {
            SendVibrations(glove, globalAmp);
        }
    }

    private void SendVibrations(HapticGlove glove, float globalAmp)
    {
        // Thumb
        if (thumbWaveform != null)
        {
            var wfThumb = thumbWaveform.GetWaveform();
            wfThumb.Amplitude = Mathf.Clamp01(globalAmp * thumbAmplitude);
            SG_CustomWaveform.CallCorrectWaveform(glove, wfThumb, VibrationLocation.Thumb_Tip);
        }

        // Index
        if (indexWaveform != null)
        {
            var wfIndex = indexWaveform.GetWaveform();
            wfIndex.Amplitude = Mathf.Clamp01(globalAmp * indexAmplitude);
            SG_CustomWaveform.CallCorrectWaveform(glove, wfIndex, VibrationLocation.Index_Tip);
        }

        // Wrist
        if (wristWaveform != null)
        {
            var wfWrist = wristWaveform.GetWaveform();
            wfWrist.Amplitude = Mathf.Clamp01(globalAmp * wristAmplitude);
            if (glove is NovaGlove)
                SG_CustomWaveform.CallCorrectWaveform(glove, wfWrist, VibrationLocation.WholeHand);
            else
                SG_CustomWaveform.CallCorrectWaveform(glove, wfWrist, VibrationLocation.Palm_IndexSide);
        }
    }

    private void StopVibrations()
    {
        HapticGlove glove = GetGloveForSide(connectsTo);
        if (glove != null)
        {
            var wfZero = new SGCore.CustomWaveform();
            wfZero.Amplitude = 0f;
            SG_CustomWaveform.CallCorrectWaveform(glove, wfZero, VibrationLocation.Thumb_Tip);
            SG_CustomWaveform.CallCorrectWaveform(glove, wfZero, VibrationLocation.Index_Tip);
            if (glove is NovaGlove)
                SG_CustomWaveform.CallCorrectWaveform(glove, wfZero, VibrationLocation.WholeHand);
            else
                SG_CustomWaveform.CallCorrectWaveform(glove, wfZero, VibrationLocation.Palm_IndexSide);
        }
    }

    private HapticGlove GetGloveForSide(SG.HandSide side)
    {
        HapticGlove[] allGloves = HapticGlove.GetHapticGloves(true);
        foreach (var glove in allGloves)
        {
            bool isRight = glove.IsRight();
            if (side == SG.HandSide.LeftHand && !isRight) return glove;
            if (side == SG.HandSide.RightHand && isRight) return glove;
        }
        return null;
    }

    void OnDestroy()
    {
        StopVibrations();
    }
}
