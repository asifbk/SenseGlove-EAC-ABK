using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SenseGlove-based drill controller:
/// - Reads finger flexion (trigger press)
/// - Sends vibration feedback
/// - Rotates drill head around a selectable local axis
/// - Optional slowdown & vibration when touching a wood surface
/// - Optional sound and dust particle effects
/// </summary>
public class SG_TriggerDrillLogic : MonoBehaviour
{
    // ------------------- ENUMS -------------------

    /// <summary>
    /// Allows rotation around a selectable local axis.
    /// </summary>
    public enum RotationAxis
    {
        X,
        Y,
        Z
    }

    // ------------------- SENSEGLOVE SETTINGS -------------------

    [Header("SenseGlove Settings")]
    [Tooltip("The SG_Grabable component attached to the drill handle.")]
    public SG.SG_Grabable grabable;

    [Tooltip("Which finger controls the trigger (default: Index).")]
    public SGCore.Finger respondsTo = SGCore.Finger.Index;

    [Tooltip("Where vibration is applied (WholeHand recommended).")]
    public SG.VibrationLocation vibrationLocation = SG.VibrationLocation.WholeHand;

    [Header("Flexion Mapping")]
    [Range(0, 1)] public float startFlexion = 0.2f;  // When finger starts to flex
    [Range(0, 1)] public float endFlexion = 0.8f;    // When trigger is fully pressed

    private float latestPressure = 0.0f;
    private float lastCmdSent = 0.0f;
    public const float sendCooldown = 0.1f;

    // ------------------- DRILL ROTATION SETTINGS -------------------

    [Header("Drill Rotation Settings")]
    [Tooltip("Assign the rotating head transform here.")]
    public Transform rotatingHead;

    [Tooltip("Select which local axis to rotate around.")]
    public RotationAxis rotationAxis = RotationAxis.X;

    [Tooltip("Max spin speed in degrees per second at full trigger press.")]
    public float maxRotationSpeed = 1200f;

    [Tooltip("How much the drill slows down when in contact with wood.")]
    [Range(0.1f, 1f)] public float resistanceFactor = 0.4f;

    private float currentRotationSpeed = 0f;
    private bool isTouchingWood = false;

    // ------------------- SOUND & PARTICLE EFFECTS -------------------

    [Header("Optional Sound & Effects")]
    [Tooltip("Looping drill sound that adjusts with trigger pressure.")]
    public AudioSource drillSound;

    [Tooltip("Optional dust particle system when drilling wood.")]
    public ParticleSystem woodDustParticles;

    [Tooltip("Tag used for wooden surfaces.")]
    public string woodTag = "Wood";

    // ------------------- PROPERTY -------------------

    public float TriggerPressure => grabable != null && grabable.IsGrabbed() ? latestPressure : 0.0f;

    // ------------------- UNITY UPDATE -------------------

    void Update()
    {
        // Only run while drill is held
        if (grabable == null || !grabable.IsGrabbed())
            return;

        SG.SG_TrackedHand firstHand = grabable.ScriptsGrabbingMe()[0].TrackedHand;

        // --- 1. READ FLEXION VALUE ---
        float[] flexions;
        if (firstHand.GetNormalizedFlexion(out flexions))
        {
            float currFlex = flexions[(int)this.respondsTo];

            if (startFlexion == endFlexion)
                latestPressure = currFlex >= startFlexion ? 1.0f : 0.0f;
            else
                latestPressure = SG.Util.SG_Util.Map(currFlex, startFlexion, endFlexion, 0.0f, 1.0f, true);
        }

        // --- 2. SEND HAPTIC FEEDBACK ---
        int amplitude = Mathf.RoundToInt(100.0f * latestPressure);
        float time = Time.timeSinceLevelLoad;

        if (amplitude > 0 && time - lastCmdSent >= sendCooldown)
        {
            lastCmdSent = time;
            grabable.SendVibrationCmd(vibrationLocation, amplitude, 0.1f, 170.0f);
            grabable.QueueFFBCmd(SGCore.Finger.Index, latestPressure);
        }

        // --- 3. ROTATION ---
        if (rotatingHead != null)
        {
            // Base rotation speed (affected by trigger pressure)
            float targetSpeed = Mathf.Lerp(0, maxRotationSpeed, latestPressure);

            // Apply slowdown if touching wood
            if (isTouchingWood)
                targetSpeed *= resistanceFactor;

            // Smooth transition
            currentRotationSpeed = Mathf.Lerp(currentRotationSpeed, targetSpeed, 8f * Time.deltaTime);

            // Determine axis
            Vector3 localAxis = Vector3.right; // Default X
            switch (rotationAxis)
            {
                case RotationAxis.Y:
                    localAxis = Vector3.up;
                    break;
                case RotationAxis.Z:
                    localAxis = Vector3.forward;
                    break;
            }

            // Perform rotation around selected axis in local space
            rotatingHead.Rotate(localAxis * currentRotationSpeed * Time.deltaTime, Space.Self);
        }

        // --- 4. SOUND & DUST ---
        HandleAudio();
        HandleParticles();
    }

    // ------------------- AUDIO -------------------

    private void HandleAudio()
    {
        if (drillSound == null) return;

        if (latestPressure > 0.05f && !drillSound.isPlaying)
            drillSound.Play();
        else if (latestPressure <= 0.05f && drillSound.isPlaying)
            drillSound.Stop();

        drillSound.pitch = Mathf.Lerp(0.8f, 1.5f, latestPressure);
        drillSound.volume = Mathf.Lerp(0.1f, 1.0f, latestPressure);
    }

    // ------------------- PARTICLE EFFECT -------------------

    private void HandleParticles()
    {
        if (woodDustParticles == null) return;

        if (isTouchingWood && latestPressure > 0.3f)
        {
            if (!woodDustParticles.isPlaying)
                woodDustParticles.Play();
        }
        else if (woodDustParticles.isPlaying)
        {
            woodDustParticles.Stop();
        }
    }

    // ------------------- COLLISION HANDLING -------------------

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(woodTag))
            isTouchingWood = true;
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag(woodTag))
            isTouchingWood = true;
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag(woodTag))
            isTouchingWood = false;
    }
}
