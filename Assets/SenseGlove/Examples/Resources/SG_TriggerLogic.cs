using UnityEngine;
using SG;
using SGCore;
using System.Linq;

public class SG_TriggerLogic : MonoBehaviour
{
    // ---------------- ENUMS -----------------
    public enum RotationAxis { X, Y, Z }
    public enum DrillAxis { Forward, Backward, Up, Down, Right, Left }

    // ---------------- REFERENCES -----------------
    [Header("SenseGlove Settings")]
    public SG_Grabable grabable;
    public Finger respondsTo = Finger.Index;

    [Header("Flexion Settings")]
    [Range(0, 1)] public float startFlexion = 0.2f;
    [Range(0, 1)] public float endFlexion = 0.8f;
    private float latestPressure = 0f;

    [Header("Safety Systems")]
    public DrillHeatSystem heatSystem;

    // ---------------- ROTATION -----------------
    [Header("Drill Rotation Settings")]
    public Transform rotatingHead;
    public RotationAxis rotationAxis = RotationAxis.X;
    public float maxRotationSpeed = 1200f;
    public float resistanceFactor = 0.4f;
    public float currentRotationSpeed = 0f;
    private bool isTouchingWood = false;

    // ---------------- CARVING / RAYCAST -----------------
    [Header("Carving Settings")]
    public Transform drillTip;
    public bool autoFindActiveDrillBit = true;
    public DrillAxis drillDirection = DrillAxis.Down;
    public float rayDistance = 1.0f;
    public float rayOffset = 0.1f;
    public LayerMask carvableLayer;

    [Header("Mesh Deformation Settings")]
    public float drillRadius = 0.3f;
    public bool autoDetectDrillSize = true;
    public float carveSpeed = 3f;
    public float deformCooldown = 0.02f;
    private float lastDeformTime = 0f;

    // ---------------- SOUND & PARTICLES -----------------
    [Header("Sound & Effects")]
    public AudioSource drillSound;
    public ParticleSystem woodDustParticles;
    public string woodTag = "Wood";
    public WoodDustGenerator dustGenerator;

    [Header("Debug")]
    public bool enableDebugLogs = true;

    // ---------------- PUBLIC ACCESSORS -----------------
    public float CurrentPressure => latestPressure;
    public bool IsTouchingWood => isTouchingWood;
    public bool IsGrabbed => grabable != null && grabable.IsGrabbed();
    public Transform CurrentDrillTip => drillTip;

    // ----------------- UPDATE LOOP ----------------------
    void Start()
    {
        if (autoDetectDrillSize && drillTip != null)
        {
            DetectDrillSize();
        }

        if (heatSystem == null)
        {
            heatSystem = GetComponent<DrillHeatSystem>();
        }
    }

    void Update()
    {
        if (grabable == null || !grabable.IsGrabbed()) return;

        if (autoFindActiveDrillBit)
        {
            FindActiveDrillBit();
        }

        UpdateTriggerPressure();
        VerifyFingerLockStatus();
        HandleRotation();
        HandleCarving();
        HandleAudio();
        HandleParticles();
    }

    // ---------------------------------------------------------
    // UNIVERSAL DRILL BIT FINDER
    // ---------------------------------------------------------
    private Transform FindDeepChildContains(Transform parent, string text)
    {
        text = text.ToLower();
        foreach (Transform child in parent)
        {
            if (child.name.ToLower().Contains(text))
                return child;

            Transform found = FindDeepChildContains(child, text);
            if (found != null)
                return found;
        }
        return null;
    }

    private void FindActiveDrillBit()
    {
        Transform drillHolder = FindDeepChildContains(transform, "drill holder");
        if (drillHolder == null)
            drillHolder = FindDeepChildContains(transform, "holder");
        if (drillHolder != null)
        {
            foreach (Transform child in drillHolder)
            {
                Debug.LogWarning("SG_TriggerLogic: Checking child: " + child.name);
                string n = child.name.ToLower();
                if (child.gameObject.activeInHierarchy && n.Contains("bit"))
                {
                    if (drillTip != child)
                    {
                        drillTip = child;
                        #if UNITY_EDITOR
                        UnityEditor.EditorUtility.SetDirty(this);
                        #endif
                        Debug.Log("✓ Switched to INTERNAL drill bit: " + child.name);
                        if (autoDetectDrillSize)
                            DetectDrillSize();
                    }
                    return;
                }
            }
        }

        GameObject[] sceneBits = GameObject.FindObjectsOfType<GameObject>()
            .Where(go => go.activeInHierarchy && go.name.ToLower().Contains("drill") && go.name.ToLower().Contains("bit"))
            .ToArray();
        foreach (GameObject bit in sceneBits)
        {
            if (drillTip != bit.transform)
            {
                drillTip = bit.transform;
                #if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
                #endif
                Debug.Log("✓ Switched to SCENE drill bit: " + bit.name);
                if (autoDetectDrillSize)
                    DetectDrillSize();
            }
            return;
        }
    }

    // ---------------------------------------------------------
    // AUTO-DETECT DRILL SIZE
    // ---------------------------------------------------------
    private void DetectDrillSize()
    {
        Collider col = drillTip.GetComponent<Collider>();
        if (col != null)
        {
            if (col is CapsuleCollider capsule)
                drillRadius = capsule.radius * Mathf.Max(drillTip.lossyScale.x, drillTip.lossyScale.z);
            else if (col is SphereCollider sphere)
                drillRadius = sphere.radius * Mathf.Max(drillTip.lossyScale.x, drillTip.lossyScale.z);
            else if (col is BoxCollider box)
                drillRadius = Mathf.Max(box.size.x * drillTip.lossyScale.x, box.size.z * drillTip.lossyScale.z) / 2f;
            else
                drillRadius = Mathf.Max(col.bounds.extents.x, col.bounds.extents.z);
        }
        else
        {
            MeshFilter mf = drillTip.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                Bounds b = mf.sharedMesh.bounds;
                drillRadius = Mathf.Max(b.extents.x * drillTip.lossyScale.x, b.extents.z * drillTip.lossyScale.z);
            }
        }

        Debug.Log($"<color=yellow>Detected drill size: {drillTip.name} | Radius: {drillRadius:F4}m ({drillRadius * 2000f:F1}mm diameter)</color>");
    }

    // ---------------------------------------------------------
    // FLEXION → PRESSURE (WITH HEAT LOCK PRIORITY)
    // ---------------------------------------------------------
    private void UpdateTriggerPressure()
    {
        SG_TrackedHand hand = grabable.ScriptsGrabbingMe()[0].TrackedHand;
        float[] flexions;
        if (hand.GetNormalizedFlexion(out flexions))
        {
            float currFlex = flexions[(int)respondsTo];
            float rawPressure = Mathf.InverseLerp(startFlexion, endFlexion, currFlex);

            if (heatSystem != null && heatSystem.IsDrillLocked())
            {
                latestPressure = 0f;
            }
            else
            {
                latestPressure = rawPressure;
            }

            // ========== FIXED: HEAT LOCK HAS PRIORITY ==========
            // Check if finger is locked by heat system
            bool fingerIsLockedByHeat = heatSystem != null && heatSystem.IsFingerLocked();
            
            if (fingerIsLockedByHeat)
            {
                // HEAT SYSTEM HAS EXCLUSIVE CONTROL
                // Send ZERO force to prevent trigger from interfering with lock
                grabable.QueueFFBCmd(Finger.Index, 0f);
                
                if (enableDebugLogs && Time.frameCount % 60 == 0)
                {
                    Debug.Log($"<color=orange>[SG_TriggerLogic] 🔒 HEAT LOCK ACTIVE - Suppressing trigger FFB (rawPressure: {rawPressure:F2})</color>");
                }
            }
            else if (latestPressure > 0.05f)
            {
                // NORMAL OPERATION: Send trigger-based FFB
                grabable.QueueFFBCmd(Finger.Index, latestPressure);
            }
            else
            {
                // No pressure, no lock - release FFB
                grabable.QueueFFBCmd(Finger.Index, 0f);
            }
        }
    }

    // ---------------------------------------------------------
    // VERIFY FINGER LOCK STATUS (DEBUG)
    // ---------------------------------------------------------
    private void VerifyFingerLockStatus()
    {
        if (!enableDebugLogs || heatSystem == null) return;
        if (Time.frameCount % 30 != 0) return; // Log every 30 frames (~2x per second at 60fps)
        
        bool shouldBeLocked = heatSystem.currentHeat >= heatSystem.maxHeat;
        bool isActuallyLocked = heatSystem.IsFingerLocked();
        bool isDrillLocked = heatSystem.IsDrillLocked();
        
        // Only log if there's a mismatch or if actively locking
        if (shouldBeLocked || isActuallyLocked || isDrillLocked)
        {
            Debug.Log($"<color=cyan>[Finger Lock Verification]</color>\n" +
                     $"  Current Heat: {heatSystem.currentHeat:F1}°C / {heatSystem.maxHeat}°C\n" +
                     $"  Hot Threshold: {heatSystem.hotColorThreshold}°C\n" +
                     $"  Should Be Locked: {shouldBeLocked}\n" +
                     $"  Actually Locked: {isActuallyLocked}\n" +
                     $"  Drill Locked (Safety): {isDrillLocked}\n" +
                     $"  Finger Lock Enabled: {heatSystem.enableFingerLock}\n" +
                     $"  Latest Pressure: {latestPressure:F2}");
            
            if (shouldBeLocked != isActuallyLocked)
            {
                Debug.LogError($"<color=red>⚠️ STATE MISMATCH - Should lock but isn't!</color>");
            }
        }
    }

    // ---------------------------------------------------------
    // ROTATION
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
    // RAYCAST CARVING
    // ---------------------------------------------------------
    private Vector3 GetDrillDirection()
    {
        switch (drillDirection)
        {
            case DrillAxis.Forward: return drillTip.forward;
            case DrillAxis.Backward: return -drillTip.forward;
            case DrillAxis.Up: return drillTip.up;
            case DrillAxis.Down: return -drillTip.up;
            case DrillAxis.Right: return drillTip.right;
            case DrillAxis.Left: return -drillTip.right;
        }
        return drillTip.forward;
    }

    private void HandleCarving()
    {
        if (drillTip == null)
        {
            if (enableDebugLogs) Debug.LogWarning("[SG_TriggerLogic] HandleCarving: drillTip is null");
            return;
        }
        if (latestPressure < 0.1f)
        {
            if (enableDebugLogs) Debug.Log($"[SG_TriggerLogic] Pressure too low: {latestPressure}");
            return;
        }

        Vector3 dir = GetDrillDirection();
        Vector3 origin = drillTip.position + (dir * rayOffset);
        Debug.DrawRay(origin, dir * rayDistance, Color.red, 0.5f);

        RaycastHit hit;
        bool didHit = carvableLayer.value != 0 ?
            Physics.Raycast(origin, dir, out hit, rayDistance, carvableLayer) :
            Physics.Raycast(origin, dir, out hit, rayDistance);

        if (enableDebugLogs) Debug.Log($"[SG_TriggerLogic] Raycast result: {didHit} | Layer mask: {carvableLayer.value}");

        if (didHit && hit.collider.transform.IsChildOf(transform))
        {
            if (enableDebugLogs) Debug.Log("[SG_TriggerLogic] Hit is child of drill - ignoring");
            return;
        }

        if (didHit)
        {
            if (enableDebugLogs) Debug.Log($"[SG_TriggerLogic] Raycast HIT: {hit.collider.name} on layer {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
            
            ExistingModelCarving carvable = hit.collider.GetComponent<ExistingModelCarving>();
            if (carvable != null)
            {
                if (Time.time - lastDeformTime >= deformCooldown)
                {
                    lastDeformTime = Time.time;
                    float pressureMultiplier = Mathf.Lerp(0.3f, 1f, latestPressure);
                    if (enableDebugLogs) Debug.Log($"[SG_TriggerLogic] Calling CarveAtPosition on {carvable.gameObject.name}");
                    carvable.SetDrillBit(drillTip);
                    carvable.CarveAtPosition(hit.point, drillRadius, carveSpeed * pressureMultiplier);
                    woodDustParticles.Emit(10);
                    if (dustGenerator != null)
                        dustGenerator.SpawnDust();
                    isTouchingWood = true;
                }
                else
                {
                    if (enableDebugLogs) Debug.Log($"[SG_TriggerLogic] Carving on cooldown. Time since last: {Time.time - lastDeformTime}");
                }
            }
            else
            {
                if (enableDebugLogs) Debug.LogWarning($"[SG_TriggerLogic] Hit {hit.collider.name} but no ExistingModelCarving component found");
                isTouchingWood = false;
            }
        }
        else
        {
            if (enableDebugLogs) Debug.Log("[SG_TriggerLogic] Raycast MISS");
            isTouchingWood = false;
        }
    }

    // ---------------------------------------------------------
    // SOUND + PARTICLES
    // ---------------------------------------------------------
    private void HandleAudio()
    {
        if (drillSound == null) return;
        
        bool shouldPlay = latestPressure > 0.1f && IsGrabbed;
        
        if (shouldPlay && !drillSound.isPlaying)
            drillSound.Play();
        else if (!shouldPlay && drillSound.isPlaying)
            drillSound.Stop();
            
        if (shouldPlay)
        {
            drillSound.pitch = Mathf.Lerp(0.8f, 1.5f, latestPressure);
            drillSound.volume = Mathf.Lerp(0.1f, 1.0f, latestPressure);
        }
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
    // COLLISION TAG CHECK
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