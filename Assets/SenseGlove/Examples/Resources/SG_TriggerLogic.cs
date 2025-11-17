using UnityEngine;
using SG;
using SGCore;

public class SG_TriggerLogic : MonoBehaviour
{
    // ---------------- ENUMS -----------------

    public enum RotationAxis
    {
        X, Y, Z
    }

    public enum DrillAxis
    {
        Forward,
        Backward,
        Up,
        Down,
        Right,
        Left
    }

    // --------------- REFERENCES -----------------

    [Header("SenseGlove Settings")]
    public SG_Grabable grabable;
    public Finger respondsTo = Finger.Index;
    public VibrationLocation vibrationLocation = VibrationLocation.WholeHand;

    [Header("Flexion Settings")]
    [Range(0, 1)] public float startFlexion = 0.2f;
    [Range(0, 1)] public float endFlexion = 0.8f;

    private float latestPressure = 0f;
    private float lastCmdSent = 0f;
    private const float sendCooldown = 0.12f;

    // ---------------- ROTATION -----------------

    [Header("Drill Rotation Settings")]
    public Transform rotatingHead;
    public RotationAxis rotationAxis = RotationAxis.X;
    public float maxRotationSpeed = 1200f;
    public float resistanceFactor = 0.4f;

    private float currentRotationSpeed = 0f;
    private bool isTouchingWood = false;

    // ---------------- CARVING / RAYCAST -----------------

    [Header("Carving Settings")]
    public Transform drillTip;
    public DrillAxis drillDirection = DrillAxis.Forward;
    public float rayDistance = 0.25f;
    public LayerMask carvableLayer; // What can be carved
    
    [Header("Mesh Deformation Settings")]
    public float drillRadius = 0.3f;           // Size of the hole
    public float carveSpeed = 3f;              // How fast it carves
    public float deformCooldown = 0.02f;       // Reduced for smoother carving
    private float lastDeformTime = 0f;

    // ---------------- SOUND & PARTICLES -----------------

    [Header("Sound & Effects")]
    public AudioSource drillSound;
    public ParticleSystem woodDustParticles;
    public string woodTag = "Wood";

    // ----------------- UPDATE LOOP ----------------------

    void Update()
    {
        if (grabable == null || !grabable.IsGrabbed())
            return;

        UpdateTriggerPressure();
        HandleRotation();
        HandleCarving();
        HandleAudio();
        HandleParticles();
    }

    // ---------------------------------------------------------
    //                     FLEXION → PRESSURE
    // ---------------------------------------------------------

    private void UpdateTriggerPressure()
    {
        SG_TrackedHand hand = grabable.ScriptsGrabbingMe()[0].TrackedHand;
        float[] flexions;

        if (hand.GetNormalizedFlexion(out flexions))
        {
            float currFlex = flexions[(int)respondsTo];
            latestPressure = Mathf.InverseLerp(startFlexion, endFlexion, currFlex);
        }

        // Haptics
        if (latestPressure > 0.05f && Time.time - lastCmdSent >= sendCooldown)
        {
            lastCmdSent = Time.time;
            int amp = Mathf.RoundToInt(latestPressure * 100);
            grabable.SendVibrationCmd(vibrationLocation, amp, 0.1f, 170f);
            grabable.QueueFFBCmd(Finger.Index, latestPressure);
        }
    }

    // ---------------------------------------------------------
    //                       ROTATION
    // ---------------------------------------------------------

    private void HandleRotation()
    {
        if (rotatingHead == null) return;

        float targetSpeed = Mathf.Lerp(0f, maxRotationSpeed, latestPressure);
        if (isTouchingWood) targetSpeed *= resistanceFactor;

        currentRotationSpeed = Mathf.Lerp(currentRotationSpeed, targetSpeed, 8f * Time.deltaTime);

        Vector3 axis = rotationAxis == RotationAxis.X ? Vector3.right :
                       rotationAxis == RotationAxis.Y ? Vector3.up :
                       Vector3.forward;

        rotatingHead.Rotate(axis * currentRotationSpeed * Time.deltaTime, Space.Self);
    }

    // ---------------------------------------------------------
    //                 RAYCAST CARVING (UPDATED)
    // ---------------------------------------------------------

    private Vector3 GetDrillDirection()
    {
        switch (drillDirection)
        {
            case DrillAxis.Forward:  return drillTip.forward;
            case DrillAxis.Backward: return -drillTip.forward;
            case DrillAxis.Up:       return drillTip.up;
            case DrillAxis.Down:     return -drillTip.up;
            case DrillAxis.Right:    return drillTip.right;
            case DrillAxis.Left:     return -drillTip.right;
        }
        return drillTip.forward;
    }

    private void HandleCarving()
    {
        if (drillTip == null)
        {
            Debug.LogError("❌ Drill Tip is NULL!");
            return;
        }
        
        Debug.Log($"⚡ Pressure: {latestPressure}");
        
        if (latestPressure < 0.1f) // REDUCED from 0.2 to 0.1
        {
            Debug.Log("⚠️ Pressure too low to carve");
            return;
        }

        Vector3 origin = drillTip.position;
        Vector3 dir = GetDrillDirection();

        Debug.DrawRay(origin, dir * rayDistance, Color.red, 1f);
        Debug.Log($"🎯 Raycasting from {origin} in direction {dir}, distance {rayDistance}");

        // Use layer mask if set, otherwise raycast everything
        bool didHit = carvableLayer.value != 0 
            ? Physics.Raycast(origin, dir, out RaycastHit hit, rayDistance, carvableLayer)
            : Physics.Raycast(origin, dir, out hit, rayDistance);

        if (didHit)
        {
            Debug.Log($"✅ HIT: {hit.collider.name} at {hit.point}");
            
            ExistingModelCarving carvable = hit.collider.GetComponent<ExistingModelCarving>();
            
            if (carvable != null)
            {
                Debug.Log("✅ Found ExistingModelCarving component!");
                
                if (Time.time - lastDeformTime >= deformCooldown)
                {
                    lastDeformTime = Time.time;
                    float pressureMultiplier = Mathf.Lerp(0.3f, 1f, latestPressure);
                    
                    Debug.Log($"🔨 CARVING! Radius:{drillRadius}, Speed:{carveSpeed * pressureMultiplier}");
                    carvable.CarveAtPosition(hit.point, drillRadius, carveSpeed * pressureMultiplier);
                    
                    isTouchingWood = true;
                }
            }
            else
            {
                Debug.LogWarning($"❌ No ExistingModelCarving on {hit.collider.name}");
                isTouchingWood = false;
            }
        }
        else
        {
            Debug.Log("❌ Raycast missed - no hit");
            isTouchingWood = false;
        }
    }

    // ---------------------------------------------------------
    //              SOUND + PARTICLES
    // ---------------------------------------------------------

    private void HandleAudio()
    {
        if (drillSound == null) return;

        if (latestPressure > 0.1f && !drillSound.isPlaying)
            drillSound.Play();
        else if (latestPressure <= 0.1f && drillSound.isPlaying)
            drillSound.Stop();

        drillSound.pitch = Mathf.Lerp(0.8f, 1.5f, latestPressure);
        drillSound.volume = Mathf.Lerp(0.1f, 1.0f, latestPressure);
    }

    private void HandleParticles()
    {
        if (woodDustParticles == null) return;

        if (isTouchingWood && latestPressure > 0.3f)
        {
            if (!woodDustParticles.isPlaying) woodDustParticles.Play();
        }
        else
        {
            if (woodDustParticles.isPlaying) woodDustParticles.Stop();
        }
    }

    // ---------------------------------------------------------
    //                      COLLISION TAG CHECK
    // ---------------------------------------------------------

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(woodTag)) isTouchingWood = true;
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag(woodTag)) isTouchingWood = true;
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag(woodTag)) isTouchingWood = false;
    }
}