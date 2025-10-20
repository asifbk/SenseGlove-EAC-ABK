using SGCore;
using SGCore.Haptics;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SG.Examples
{
    public class SGEx_GloveDiagnostics : MonoBehaviour
    {
        [Header("References")]
        public SGEx_SelectHandModel handSelector;

        [Header("UI Components")]
        public Text titleText;
        public SG_InputSlider[] fingerFFB = new SG_InputSlider[0];
        public SG_InputSlider[] fingerVibration = new SG_InputSlider[0];
        public SG_InputSlider thumperVibration;

        public Button ffbOn, ffbOff, toggleFFB, buzzOn, buzzOff, toggleBuzz;

        [Header("KeyBinds")]
        public KeyCode resetWristKey = KeyCode.P;
        public KeyCode testThumperKey = KeyCode.T;
        public KeyCode resetCalibrKey = KeyCode.C;
        public KeyCode testBuzzKey = KeyCode.B;
        public KeyCode testFFbKey = KeyCode.F;

        private SGCore.HapticGlove leftHapticGlove = null;
        private SGCore.HapticGlove rightHapticGlove = null;
        private bool sComRuns = false;

        void Start()
        {
            sComRuns = SGCore.DeviceList.SenseCommRunning();
            titleText.text = sComRuns ? "Awaiting connection with gloves..." :
                                        "SenseCom isn't running — no glove will be detected!";
            titleText.color = sComRuns ? Color.white : Color.red;

            ffbOn.onClick.AddListener(() => SetFFB(true));
            ffbOff.onClick.AddListener(() => SetFFB(false));
            toggleFFB.onClick.AddListener(() => ToggleFFB());

            buzzOn.onClick.AddListener(() => SetVibration(true));
            buzzOff.onClick.AddListener(() => SetVibration(false));
            toggleBuzz.onClick.AddListener(() => ToggleVibration());
        }

        void Update()
        {
            if (!sComRuns)
            {
                sComRuns = SGCore.DeviceList.SenseCommRunning();
                if (sComRuns)
                {
                    titleText.color = Color.white;
                    titleText.text = "Awaiting connection to gloves...";
                }
                return;
            }

            // Detect both gloves dynamically
            if (leftHapticGlove == null && handSelector.leftGlove != null && handSelector.leftGlove.IsConnected())
                leftHapticGlove = (SGCore.HapticGlove)handSelector.leftGlove.InternalGlove;

            if (rightHapticGlove == null && handSelector.rightGlove != null && handSelector.rightGlove.IsConnected())
                rightHapticGlove = (SGCore.HapticGlove)handSelector.rightGlove.InternalGlove;

            // Update title info
            string leftInfo = (leftHapticGlove != null && leftHapticGlove.IsConnected())
                ? "Left: Connected (Left hand)"
                //? "Left: " + leftHapticGlove.GetDeviceID()
                : "Left: not connected";
            string rightInfo = (rightHapticGlove != null && rightHapticGlove.IsConnected())
                ? "Right: Connected (Right hand)"
                //? "Right: " + rightHapticGlove.GetDeviceID()
                : "Right: not connected";
            titleText.text = $"{leftInfo} | {rightInfo}";

            // Send haptics each frame
            HandleGloveFeedback(leftHapticGlove, "Left");
            HandleGloveFeedback(rightHapticGlove, "Right");

#if !ENABLE_INPUT_SYSTEM
            if (Input.GetKeyDown(resetWristKey)) CalibrateIMU();
            if (Input.GetKeyDown(testBuzzKey)) ToggleVibration();
            if (Input.GetKeyDown(testFFbKey)) ToggleFFB();
            if (Input.GetKeyDown(resetCalibrKey)) ResetCalibration();
#endif
        }

        //---------------------------------------------------------------------
        // Helper Methods
        //---------------------------------------------------------------------

        void HandleGloveFeedback(SGCore.HapticGlove glove, string side)
        {
            if (glove == null || !glove.IsConnected()) return;

            float[] ffb = new float[fingerFFB.Length];
            for (int i = 0; i < fingerFFB.Length; i++)
                ffb[i] = fingerFFB[i].SlideValue / 100f;

            float[] buzz = new float[fingerVibration.Length];
            for (int i = 0; i < fingerVibration.Length; i++)
                buzz[i] = fingerVibration[i].SlideValue / 100f;

            float wrist = thumperVibration != null ? thumperVibration.SlideValue / 100f : 0f;

            glove.QueueFFBLevels(ffb);
            glove.QueueVibroLevels(buzz);

            if (glove is SGCore.Nova.NovaGlove nova)
                nova.QueueWristLevel(wrist);

            glove.SendHaptics();

            //Debug.Log($"[{side} Glove] Sent FFB + Vibration | Wrist={wrist:F2}");
        }

        //---------------------------------------------------------------------
        // UI helpers
        //---------------------------------------------------------------------

        public void SetFFB(bool state)
        {
            int magn = state ? 100 : 0;
            foreach (var s in fingerFFB) s.SlideValue = magn;
        }

        public void ToggleFFB() => SetFFB(!FFBEnabled);

        public bool FFBEnabled
        {
            get
            {
                foreach (var s in fingerFFB)
                    if (s.SlideValue < 100) return false;
                return true;
            }
        }

        public void SetVibration(bool state)
        {
            int magn = state ? 100 : 0;
            foreach (var s in fingerVibration) s.SlideValue = magn;
            if (thumperVibration != null) thumperVibration.SlideValue = magn;
        }

        public void ToggleVibration() => SetVibration(!VibrationEnabled);

        public bool VibrationEnabled
        {
            get
            {
                foreach (var s in fingerVibration)
                    if (s.SlideValue < 100) return false;
                return thumperVibration == null || thumperVibration.SlideValue >= 100;
            }
        }

        //---------------------------------------------------------------------
        // Calibration
        //---------------------------------------------------------------------

        public void CalibrateIMU()
        {
            if (handSelector.leftGlove != null)
            {
                if (handSelector.leftGlove.GetIMURotation(out Quaternion imu))
                    handSelector.leftHand.handAnimation.CalibrateWrist(imu);
            }

            if (handSelector.rightGlove != null)
            {
                if (handSelector.rightGlove.GetIMURotation(out Quaternion imu))
                    handSelector.rightHand.handAnimation.CalibrateWrist(imu);
            }

            Debug.Log("IMU recalibrated for both gloves.");
        }

        public void ResetCalibration()
        {
            if (handSelector.leftHand != null)
                handSelector.leftHand.calibration.ResetCalibration(false);
            if (handSelector.rightHand != null)
                handSelector.rightHand.calibration.ResetCalibration(false);
        }

        void OnApplicationQuit()
        {
            if (leftHapticGlove != null) leftHapticGlove.StopHaptics();
            if (rightHapticGlove != null) rightHapticGlove.StopHaptics();
        }
    }
}
