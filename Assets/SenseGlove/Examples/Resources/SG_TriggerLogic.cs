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
    private float currentRotationSpeed = 0f;
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
                string n = child.name.ToLower();
                if (child.gameObject.activeInHierarchy && n.Contains("drill"))
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
    // FLEXION → PRESSURE (NO VIBRATION)
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

            if (latestPressure > 0.05f)
            {
                grabable.QueueFFBCmd(Finger.Index, latestPressure);
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
        if (drillTip == null) return;
        if (latestPressure < 0.1f) return;

        Vector3 dir = GetDrillDirection();
        Vector3 origin = drillTip.position + (dir * rayOffset);
        Debug.DrawRay(origin, dir * rayDistance, Color.red, 0.5f);

        RaycastHit hit;
        bool didHit = carvableLayer.value != 0 ?
            Physics.Raycast(origin, dir, out hit, rayDistance, carvableLayer) :
            Physics.Raycast(origin, dir, out hit, rayDistance);

        if (didHit && hit.collider.transform.IsChildOf(transform)) return;

        if (didHit)
        {
            ExistingModelCarving carvable = hit.collider.GetComponent<ExistingModelCarving>();
            if (carvable != null)
            {
                if (Time.time - lastDeformTime >= deformCooldown)
                {
                    lastDeformTime = Time.time;
                    float pressureMultiplier = Mathf.Lerp(0.3f, 1f, latestPressure);
                    carvable.SetDrillBit(drillTip);
                    carvable.CarveAtPosition(hit.point, drillRadius, carveSpeed * pressureMultiplier);
                    woodDustParticles.Emit(10);
                    if (dustGenerator != null)
                        dustGenerator.SpawnDust();
                    isTouchingWood = true;
                }
            }
            else
            {
                isTouchingWood = false;
            }
        }
        else
        {
            isTouchingWood = false;
        }
    }

    // ---------------------------------------------------------
    // SOUND + PARTICLES
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
