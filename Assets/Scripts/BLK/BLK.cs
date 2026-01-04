using UnityEngine;
using System.Collections;

public class DrillState
{
    public int State { get; set; }
    public string AttachedBit { get; set; }
    public string BitRpm { get; set; }
}

public class BLK : MonoBehaviour
{
    private int state = 0;
    private string attached_bit = "None";
    private string bit_rpm = "0";
    public LLMSnapshot.vlmResponse latestVLMResponse;

    // assignable callback for response trigger events
    public System.Action<LLMSnapshot.vlmResponse> OnVLMTrigger;

    public int State
    {
        set { state = value; }
    }

    public string AttachedBit
    {
        set { attached_bit = value; }
    }

    public string BitRpm
    {
        set { bit_rpm = value; }
    }

    void Start()
    {
        // StartCoroutine(CaptureAndSend("http://eac-st-22:8000/caption"));
        StartCoroutine(CaptureAndSend("http://144.167.236.60:8000/caption"));

    }

    IEnumerator CaptureAndSend(string apiUrl)
    {
        Camera targetCamera = Camera.main;

        if (targetCamera == null)
        {
            Debug.LogError("No main camera found for snapshot capture.");
            yield break;
        }

        int resolutionWidth = 854;
        int resolutionHeight = 480;

        var wait = new WaitForSeconds(5f);

        while (true)
        {
            RenderTexture rt = new RenderTexture(resolutionWidth, resolutionHeight, 24);
            targetCamera.targetTexture = rt;
            targetCamera.Render();

            RenderTexture.active = rt;

            Texture2D snapshot = new Texture2D(
                resolutionWidth,
                resolutionHeight,
                TextureFormat.RGB24,
                false
            );

            snapshot.ReadPixels(
                new Rect(0, 0, resolutionWidth, resolutionHeight),
                0,
                0
            );
            snapshot.Apply();

            targetCamera.targetTexture = null;
            RenderTexture.active = null;
            Destroy(rt);

            byte[] imageBytes = snapshot.EncodeToPNG();
            Destroy(snapshot);

            var drillState = new DrillState
            {
                State = state,
                AttachedBit = attached_bit,
                BitRpm = bit_rpm
            };

            Debug.Log("Sending snapshot to API with DrillState: " +
                      "State=" + drillState.State +
                      ", AttachedBit=" + drillState.AttachedBit +
                      ", BitRpm=" + drillState.BitRpm);

            yield return LLMSnapshot.SendToAPI(apiUrl, imageBytes, drillState, response =>
            {
                latestVLMResponse = response;
                OnVLMTrigger?.Invoke(response);
            });

            yield return wait;
        }
    }
}