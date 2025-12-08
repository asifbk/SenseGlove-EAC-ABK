using UnityEngine;
using TMPro;

public class DrillHeatVisualizer : MonoBehaviour
{
    [Header("References")]
    public DrillHeatSystem heatSystem;
    public TextMeshProUGUI heatText;
    public TextMeshProUGUI statusText;
    
    [Header("UI Elements")]
    public UnityEngine.UI.Image heatBar;
    public Color coolBarColor = Color.green;
    public Color warmBarColor = Color.yellow;
    public Color hotBarColor = Color.red;
    
    [Header("Visual Indicators")]
    public GameObject burningIndicator;
    public GameObject overheatingIndicator;
    
    void Update()
    {
        if (heatSystem == null) return;
        
        float heatPercent = heatSystem.GetHeatPercentage();
        
        if (heatText != null)
        {
            heatText.text = $"Heat: {heatPercent * 100:F0}%";
        }
        
        if (heatBar != null)
        {
            heatBar.fillAmount = heatPercent;
            
            if (heatPercent < 0.5f)
                heatBar.color = Color.Lerp(coolBarColor, warmBarColor, heatPercent * 2f);
            else
                heatBar.color = Color.Lerp(warmBarColor, hotBarColor, (heatPercent - 0.5f) * 2f);
        }
        
        if (statusText != null)
        {
            string status = "COOL";
            if (heatPercent >= 0.7f)
                status = "<color=red>OVERHEATING!</color>";
            else if (heatPercent >= 0.5f)
                status = "<color=orange>BURNING</color>";
            else if (heatPercent >= 0.25f)
                status = "<color=yellow>WARMING</color>";
            
            statusText.text = status;
        }
        
        if (burningIndicator != null)
        {
            burningIndicator.SetActive(heatSystem.IsBurning());
        }
        
        if (overheatingIndicator != null)
        {
            overheatingIndicator.SetActive(heatSystem.IsOverheating());
        }
    }
}
