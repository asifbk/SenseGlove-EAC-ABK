using UnityEngine;

public class BatteryLevelIndicator : MonoBehaviour
{
    [Header("Cell Settings")]
    public int numberOfCells = 6;
    public Vector3 cellSize = new Vector3(0.01f, 0.02f, 0.008f);
    public float cellSpacing = 0.012f;
    public Vector3 cellOffset = new Vector3(0.6f, 0f, 0f);
    public Vector3 cellRotation = new Vector3(0f, 90f, 0f);
    public Vector3 cellContainerScale = new Vector3(5f, 5f, 5f);
    
    [Header("Battery Colors")]
    public Color highChargeColor = new Color(0f, 1f, 0f, 1f);
    public Color mediumChargeColor = new Color(1f, 1f, 0f, 1f);
    public Color lowChargeColor = new Color(1f, 0f, 0f, 1f);
    public Color emptyColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    
    [Header("Thresholds")]
    [Range(0f, 1f)] public float highChargeThreshold = 0.6f;
    [Range(0f, 1f)] public float mediumChargeThreshold = 0.3f;
    
    [Header("Emission Settings")]
    public float highChargeEmissionIntensity = 3f;
    public float mediumChargeEmissionIntensity = 2f;
    public float lowChargeEmissionIntensity = 1.5f;
    public float emptyEmissionIntensity = 0.2f;
    
    [Header("Animation")]
    public bool enableBlinkWhenLow = true;
    public float blinkSpeed = 3f;
    
    [Header("Auto Setup")]
    public bool autoCreateCells = true;
    
    private GameObject[] cellObjects;
    private Renderer[] cellRenderers;
    private Material[] cellMaterials;
    private float currentChargeLevel = 1f;
    private float blinkTimer = 0f;

    void Start()
    {
        if (autoCreateCells)
        {
            CreateBatteryCells();
        }
        
        UpdateBatteryLevel(1f);
    }

    void CreateBatteryCells()
    {
        Transform existingCellsParent = transform.Find("BatteryCells");
        if (existingCellsParent != null)
        {
            DestroyImmediate(existingCellsParent.gameObject);
        }

        GameObject cellsParent = new GameObject("BatteryCells");
        cellsParent.transform.SetParent(transform);
        cellsParent.transform.localPosition = cellOffset;
        cellsParent.transform.localRotation = Quaternion.Euler(cellRotation);
        cellsParent.transform.localScale = cellContainerScale;

        cellObjects = new GameObject[numberOfCells];
        cellRenderers = new Renderer[numberOfCells];
        cellMaterials = new Material[numberOfCells];

        float totalWidth = (numberOfCells - 1) * cellSpacing;
        float startX = -totalWidth * 0.5f;

        for (int i = 0; i < numberOfCells; i++)
        {
            GameObject cell = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cell.name = $"BatteryCell_{i + 1}";
            cell.transform.SetParent(cellsParent.transform);
            
            float xPos = startX + (i * cellSpacing);
            cell.transform.localPosition = new Vector3(xPos, 0f, 0f);
            cell.transform.localRotation = Quaternion.identity;
            cell.transform.localScale = cellSize;

            Collider cellCollider = cell.GetComponent<Collider>();
            if (cellCollider != null)
            {
                Destroy(cellCollider);
            }

            Renderer renderer = cell.GetComponent<Renderer>();
            Material cellMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            cellMaterial.EnableKeyword("_EMISSION");
            renderer.material = cellMaterial;

            cellObjects[i] = cell;
            cellRenderers[i] = renderer;
            cellMaterials[i] = cellMaterial;
        }
    }

    void Update()
    {
        if (enableBlinkWhenLow && currentChargeLevel < mediumChargeThreshold && currentChargeLevel > 0)
        {
            UpdateBlinking();
        }
    }

    public void UpdateBatteryLevel(float chargePercentage)
    {
        currentChargeLevel = Mathf.Clamp01(chargePercentage);
        
        if (cellMaterials == null || cellMaterials.Length == 0)
        {
            return;
        }

        int activeCells = Mathf.CeilToInt(currentChargeLevel * numberOfCells);
        
        for (int i = 0; i < numberOfCells; i++)
        {
            if (i < activeCells)
            {
                float cellCharge = Mathf.InverseLerp(i, i + 1, currentChargeLevel * numberOfCells);
                Color cellColor = GetColorForCharge(currentChargeLevel);
                float emissionIntensity = GetEmissionIntensityForCharge(currentChargeLevel);
                
                if (cellMaterials[i] != null)
                {
                    cellMaterials[i].color = cellColor;
                    Color emissionColor = cellColor * emissionIntensity;
                    cellMaterials[i].SetColor("_EmissionColor", emissionColor);
                }
            }
            else
            {
                if (cellMaterials[i] != null)
                {
                    cellMaterials[i].color = emptyColor;
                    cellMaterials[i].SetColor("_EmissionColor", emptyColor * emptyEmissionIntensity);
                }
            }
        }
    }

    void UpdateBlinking()
    {
        blinkTimer += Time.deltaTime * blinkSpeed;
        float blinkFactor = (Mathf.Sin(blinkTimer) + 1f) * 0.5f;
        
        int activeCells = Mathf.CeilToInt(currentChargeLevel * numberOfCells);
        Color baseColor = GetColorForCharge(currentChargeLevel);
        float baseEmission = GetEmissionIntensityForCharge(currentChargeLevel);
        
        for (int i = 0; i < activeCells; i++)
        {
            if (cellMaterials[i] != null)
            {
                float currentEmission = Mathf.Lerp(emptyEmissionIntensity, baseEmission, blinkFactor);
                Color emissionColor = baseColor * currentEmission;
                cellMaterials[i].SetColor("_EmissionColor", emissionColor);
            }
        }
    }

    Color GetColorForCharge(float charge)
    {
        if (charge <= 0f)
        {
            return emptyColor;
        }
        else if (charge < mediumChargeThreshold)
        {
            return lowChargeColor;
        }
        else if (charge < highChargeThreshold)
        {
            return mediumChargeColor;
        }
        else
        {
            return highChargeColor;
        }
    }

    float GetEmissionIntensityForCharge(float charge)
    {
        if (charge <= 0f)
        {
            return emptyEmissionIntensity;
        }
        else if (charge < mediumChargeThreshold)
        {
            return lowChargeEmissionIntensity;
        }
        else if (charge < highChargeThreshold)
        {
            return mediumChargeEmissionIntensity;
        }
        else
        {
            return highChargeEmissionIntensity;
        }
    }

    void OnDestroy()
    {
        if (cellMaterials != null)
        {
            foreach (Material mat in cellMaterials)
            {
                if (mat != null)
                {
                    Destroy(mat);
                }
            }
        }
    }
}
