using UnityEngine;
using TMPro;
using SG;

[RequireComponent(typeof(SG_Grabable))]
public class DrillInfoFloatingUI : MonoBehaviour
{
    [Header("UI Prefab")]
    public GameObject infoCanvasPrefab;     // Assign your Canvas prefab (world-space)
    public Vector3 offset = new Vector3(0, 0.15f, 0);  // Floating above drill

    [Header("Drill Specifications")]
    public float drillDiameter = 8f;        // mm
    public float drillLength = 120f;        // mm
    public float drillPitch = 1.5f;         // mm
    
    [Header("Compatible Materials")]
    public string[] compatibleMaterials = { "Wood", "Concrete", "Brass" }; // Add compatible materials from inspector

    private GameObject infoCanvasInstance;
    private TextMeshProUGUI infoText;
    private SG_Grabable grabable;

    void Start()
    {
        grabable = GetComponent<SG_Grabable>();

        // Instantiate UI canvas
        if (infoCanvasPrefab != null)
        {
            infoCanvasInstance = Instantiate(infoCanvasPrefab);
            infoText = infoCanvasInstance.GetComponentInChildren<TextMeshProUGUI>();
            infoCanvasInstance.SetActive(false);
        }
    }

    void Update()
    {
        if (grabable == null || infoCanvasInstance == null || infoText == null)
            return;

        // When grabbed
        if (grabable.IsGrabbed())
        {
            infoCanvasInstance.SetActive(true);

            infoText.text =
                $"<b>{gameObject.name}</b>\n" +
                $"Diameter: {drillDiameter} mm\n" +
                $"Length: {drillLength} mm\n" +
                $"Pitch: {drillPitch} mm\n" +
                $"Compatible for: {string.Join(", ", compatibleMaterials)}";

            // Set position above the drill
            infoCanvasInstance.transform.position = transform.position + offset;

            // Make it always face the main camera (billboard)
            if (Camera.main != null)
                infoCanvasInstance.transform.rotation = Camera.main.transform.rotation;
        }
        else
        {
            // Hide UI
            infoCanvasInstance.SetActive(false);
        }
    }
}
