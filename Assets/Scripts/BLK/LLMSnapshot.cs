using UnityEngine;
using System.Collections;

public static class LLMSnapshot
{
    [System.Serializable]
    public class vlmResponse 
    {
        //  {"description":"The drill is in contact with a slab, but the wrong HSS Bit is attached for drilling wood. The drill is not active.","state":0,"material":"wood","bit":"HSS Bit","contact":true,"switch_pressed":false,"feedback":"Use the Brad Point Bit for drilling wood. Ensure the drill is in contact with the slab before pressing the switch.","trigger":true}
        public string description;
        public int state;
        public string material;
        public string bit;
        public bool contact;
        public bool switch_pressed;
        public string feedback;
        public bool trigger;
    }

    public static IEnumerator SendToAPI(string url, byte[] imageBytes, DrillState drillState, System.Action<vlmResponse> callback = null)
    {
        var form = new WWWForm();
        form.AddBinaryData("file", imageBytes, "snapshot.png", "image/png");
        form.AddField("state", drillState.State);
        form.AddField("attached_bit", drillState.AttachedBit);
        form.AddField("bit_rpm", drillState.BitRpm.ToString());

        using (var req = UnityEngine.Networking.UnityWebRequest.Post(url, form))
        {
            yield return req.SendWebRequest();

            var responseText = req.downloadHandler != null ? req.downloadHandler.text : "<no response body>";
            Debug.Log("API Response: " + responseText);
            var vlmResponse = JsonUtility.FromJson<vlmResponse>(responseText);

            if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
                Debug.LogError("API Upload Failed: " + req.error);
            else
                Debug.Log("API Upload Success");
            callback?.Invoke(vlmResponse);
        }
    }
}