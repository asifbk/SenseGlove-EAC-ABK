using SGCore;
using SGCore.Haptics;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SG.Examples
{
    public class SGEx_GloveDiagnostics : MonoBehaviour
    {
        // Reference to the hand selector example script. This provides access to the
        // currently selected left/right glove wrappers and associated tracked hands.
        [Header("References")]
        public SGEx_SelectHandModel handSelector;

        // UI elements used by the diagnostics panel.
        // - titleText: status line that shows connection state
        // - fingerFFB: per-finger force-feedback sliders (0-100 from SG_InputSlider)
        // - fingerVibration: per-finger vibration sliders
        // - thumperVibration: wrist/thumper vibration slider (optional)
        [Header("UI Components")]
        public Text titleText;
        public SG_InputSlider[] fingerFFB = new SG_InputSlider[0];
        public SG_InputSlider[] fingerVibration = new SG_InputSlider[0];
        public SG_InputSlider thumperVibration;

        // Buttons on the diagnostics UI for quick actions
        public Button ffbOn, ffbOff, toggleFFB, buzzOn, buzzOff, toggleBuzz;

        // Keyboard shortcuts that map to some diagnostic actions. These are only
        // handled when Unity's old input system is active (non ENABLE_INPUT_SYSTEM build).
        [Header("KeyBinds")]
        public KeyCode resetWristKey = KeyCode.P;
        public KeyCode testThumperKey = KeyCode.T;
        public KeyCode resetCalibrKey = KeyCode.C;
        public KeyCode testBuzzKey = KeyCode.B;
        public KeyCode testFFbKey = KeyCode.F;

        // Internal references to the low-level SGCore HapticGlove instances. These
        // are populated at runtime from the higher-level wrapper objects exposed
        // by the example hand selector.
        private SGCore.HapticGlove leftHapticGlove = null;
        private SGCore.HapticGlove rightHapticGlove = null;
        private bool sComRuns = false; // indicates whether SenseCom (the middleware) is running

        void Start()
        {
            // Check whether SenseCom is running. If not, the diagnostics UI will
            // show a warning because no gloves can be detected/connected.
            sComRuns = SGCore.DeviceList.SenseCommRunning();
            titleText.text = sComRuns ? "Awaiting connection with gloves..." :
                                        "SenseCom isn't running — no glove will be detected!";
            titleText.color = sComRuns ? Color.white : Color.red;

            // Wire up UI buttons to helper methods. These provide quick control
            // for enabling/disabling/toggling FFB and vibration levels.
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
            // Show a concise connection status in the title text
            titleText.text = $"{leftInfo} | {rightInfo}";

            // Each frame we construct haptic command arrays from the UI sliders
            // and send them to each connected glove. This keeps the diagnostics
            // panel responsive for interactive testing.
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
            // Nothing to do if the glove reference isn't valid or is disconnected
            if (glove == null || !glove.IsConnected()) return;

            // Build force-feedback (FFB) array from the UI sliders. SG_InputSlider
            // provides a 0-100 value, the glove API expects 0-1 floats, so divide.
            float[] ffb = new float[fingerFFB.Length];
            for (int i = 0; i < fingerFFB.Length; i++)
                ffb[i] = fingerFFB[i].SlideValue / 100f;

            // Build vibration levels for each finger
            float[] buzz = new float[fingerVibration.Length];
            for (int i = 0; i < fingerVibration.Length; i++)
                buzz[i] = fingerVibration[i].SlideValue / 100f;

            // Wrist/thumper vibration (Nova-specific). Optional slider.
            float wrist = thumperVibration != null ? thumperVibration.SlideValue / 100f : 0f;

            // Queue levels on the low-level glove API. The example uses separate
            // queue calls for force and vibration, then sends them together.
            glove.QueueFFBLevels(ffb);
            glove.QueueVibroLevels(buzz);

            // Nova gloves expose an extra wrist/thumper control; queue when applicable.
            if (glove is SGCore.Nova.NovaGlove nova)
                nova.QueueWristLevel(wrist);

            // Finally, send the queued haptics to the device.
            glove.SendHaptics();

            // Debugging helper (commented out by default)
            // Debug.Log($"[{side} Glove] Sent FFB + Vibration | Wrist={wrist:F2}");
        }

        //---------------------------------------------------------------------
        // UI helpers
        //---------------------------------------------------------------------

        public void SetFFB(bool state)
        {
            // Set all force-feedback sliders to either max (100) or min (0).
            int magn = state ? 100 : 0;
            foreach (var s in fingerFFB) s.SlideValue = magn;
        }

        public void ToggleFFB() => SetFFB(!FFBEnabled);

        public bool FFBEnabled
        {
            get
            {
                // Returns true only if every FFB slider is at max (100)
                foreach (var s in fingerFFB)
                    if (s.SlideValue < 100) return false;
                return true;
            }
        }

        public void SetVibration(bool state)
        {
            // Set all vibration sliders (including optional wrist thumper) to on/off
            int magn = state ? 100 : 0;
            foreach (var s in fingerVibration) s.SlideValue = magn;
            if (thumperVibration != null) thumperVibration.SlideValue = magn;
        }

        public void ToggleVibration() => SetVibration(!VibrationEnabled);

        public bool VibrationEnabled
        {
            get
            {
                // Returns true only if every finger vibration slider is at max and
                // the thumper (if present) is also at max.
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
            // Attempt to read the IMU orientation form each glove and apply it to
            // the associated hand model's animation calibration. This re-aligns
            // the virtual wrist to the glove IMU's current orientation.
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
            // Reset the saved calibration for each tracked hand. The boolean
            // parameter indicates whether to persist the reset (false = temporary)
            if (handSelector.leftHand != null)
                handSelector.leftHand.calibration.ResetCalibration(false);
            if (handSelector.rightHand != null)
                handSelector.rightHand.calibration.ResetCalibration(false);
        }

        void OnApplicationQuit()
        {
            // Ensure haptics are stopped when the application quits to avoid
            // leaving the hardware in an active state.
            if (leftHapticGlove != null) leftHapticGlove.StopHaptics();
            if (rightHapticGlove != null) rightHapticGlove.StopHaptics();
        }
    }
}
