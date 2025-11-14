using UnityEngine;

public class SnapWithParenting : MonoBehaviour
{
    [Header("Snap Setup")]
    public Transform snapPoint;           // The exact snap location
    public Transform snapParent;          // The object that will become the parent when snapped
    public float snapDistance = 0.05f;
    public bool snapRotation = true;

    [Header("Unsnap Setup")]
    public float autoUnsnapDistance = 0.10f;
    public bool allowUnsnap = true;

    private Rigidbody rb;
    private SG.SG_Grabable grabable;

    private bool isSnapped = false;
    private bool isGrabbed = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        grabable = GetComponent<SG.SG_Grabable>();
    }

    void Update()
    {
        // 1 — Update SenseGlove grab status
        if (grabable != null)
            isGrabbed = grabable.IsGrabbed();

        // 2 — Unsnap immediately when grabbed
        if (isGrabbed && isSnapped && allowUnsnap)
        {
            Unsnap();
        }

        // 3 — Snap only when released
        if (!isGrabbed)
        {
            Unsnap();
            TrySnap();
        }
    }

    private void TrySnap()
    {
        if (snapPoint == null) return;

        float dist = Vector3.Distance(transform.position, snapPoint.position);

        if (dist < snapDistance)
        {
            Snap();
        }
    }

    private void Snap()
    {
        // Move into correct position & rotation
        transform.position = snapPoint.position;
        if (snapRotation)
            transform.rotation = snapPoint.rotation;

        // Parent the object
        if (snapParent != null)
            transform.SetParent(snapParent);

        // Lock physics
        rb.isKinematic = true;
        rb.useGravity = false;

        //the object collider's isTrigger should be true
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;

        isSnapped = true;
    }

    private void Unsnap()
    {
        // Remove parent → object goes to root level
        transform.SetParent(null);

        // Unlock physics
        rb.isKinematic = false;
        rb.useGravity = true;

        //the object collider's isTrigger should be false
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = false;

        isSnapped = false;
    }
}
