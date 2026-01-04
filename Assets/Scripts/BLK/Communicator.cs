using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using Valve.VR.InteractionSystem;
using Unity.VisualScripting;

public class Communicator : MonoBehaviour
{
    public bool isInContact = false;
    public string currentBit = "None"; // None, Brad Point Bit, Masonry Bit, HSS Bit
    public int bitRpm = 0; // Range (0 - 10)

    public GameObject[] availableBits; // Assign in 
    public GameObject[] availableMaterials; // Assign in inspector

    public GameObject drillHolder; // Assign in inspector

    public BLK blkScript; // Assign in inspector
    public SG_TriggerLogic triggerLogic; // Assign in inspector
    public AudioSource warningSound; // Assign in inspector


    void Start()
    {
        if (blkScript != null)
        {
            // Assign callback for VLM trigger events
            blkScript.OnVLMTrigger += onVLMTrigger;
        }
    }

    void onVLMTrigger(LLMSnapshot.vlmResponse response)
    {
        Debug.Log("VLM Triggered Callback: " + response.trigger + " | Feedback: " + response.feedback);
        // Additional logic based on VLM response can be added here
        if (response.trigger == true && !warningSound.isPlaying){
            warningSound.Play();}
        else if (response.trigger == false && warningSound.isPlaying){
            warningSound.Stop();}
    }

    void Update()
    {
        // check drillHolder's children to see if a bit is attached, and see if it is one of the available bits
        if (drillHolder.transform.childCount > 0)
        {
            GameObject attachedBit = drillHolder.transform.GetChild(0).gameObject;
            if (System.Array.Exists(availableBits, bit => bit.name == attachedBit.name))
            {
                currentBit = attachedBit.name;
            }
            else
            {
                currentBit = "None";
            }
        }
        else
        {
            currentBit = "None";
        }

        // Incontact detection logic using colliders from attached bit and available materials
        isInContact = false;
        if (currentBit != "None")
        {
            GameObject attachedBit = drillHolder.transform.GetChild(0).gameObject;
            Collider bitCollider = attachedBit.GetComponent<Collider>();
            if (bitCollider != null)
            {
                foreach (GameObject material in availableMaterials)
                {
                    Collider materialCollider = material.GetComponent<Collider>();
                    if (materialCollider != null && bitCollider.bounds.Intersects(materialCollider.bounds))
                    {
                        isInContact = true;
                        break;
                    }
                }
            }
        }

        // /GetComponent bitrpm as currentRotationSpeed from triggerLogic and map it to range 0-10
        bitRpm = Mathf.Clamp(Mathf.RoundToInt((triggerLogic.currentRotationSpeed / triggerLogic.maxRotationSpeed) * 10), 0, 10);

        // Update BLK script with current status
        blkScript.AttachedBit = currentBit;
        blkScript.BitRpm = bitRpm.ToString();
        if (currentBit != "None")
        {
            blkScript.State = isInContact ? 2 : 1;
        }
        else
        {
            blkScript.State = 0;
        }
    }

}