using UnityEngine;

namespace SG.Examples
{
    /// <summary> Selects two SG_TrackedHands </summary>
    public class SGEx_SelectHandModel : MonoBehaviour
    {
        public SG.Util.SGEvent ActiveHandConnect = new Util.SGEvent();
        public SG.Util.SGEvent ActiveHandDisconnect = new Util.SGEvent();

        [Header("Left Hand Components")]
        public SG_TrackedHand leftHand;
        public SG_HapticGlove leftGlove;


        [Header("Right Hand Components")]
        public SG_TrackedHand rightHand;
        public SG_HapticGlove rightGlove;




        public SG_TrackedHand ActiveHand
        {
            get; private set;
        }

        public bool Connected
        {
            get { return this.ActiveHand != null; }
        }

        public SG_HapticGlove ActiveGlove
        {
            get
            {
                if (this.ActiveHand != null && ActiveHand.RealHandSource != null && ActiveHand.RealHandSource is SG.SG_HapticGlove)
                {
                    return (SG.SG_HapticGlove) this.ActiveHand.RealHandSource;
                }
                return null;
            }
        }



        void Start()
        {
            //leftGlove.connectsTo = HandSide.LeftHand;
            ////leftHand.SetTrackingProvider(leftGlove);
            //throw new System.NotImplementedException();
           //eftHand.HandModelEnabled = false;

            //rightGlove.connectsTo = HandSide.RightHand;
            ////rightHand.SetTrackingProvider(rightGlove);
            //throw new System.NotImplementedException();
           //ightHand.HandModelEnabled = false;
        }

                    void Update()
            {
                // Check and activate both hands independently
                if (rightHand != null && rightHand.IsConnected())
                {
                    if (!rightHand.HandModelEnabled)
                    {
                        rightHand.HandModelEnabled = true;
                        Debug.Log("Right hand connected!");
                    }
                }
                else if (rightHand != null)
                {
                    if (rightHand.HandModelEnabled)
                    {
                        rightHand.HandModelEnabled = false;
                        Debug.Log("Right hand disconnected!");
                    }
                }

                if (leftHand != null && leftHand.IsConnected())
                {
                    if (!leftHand.HandModelEnabled)
                    {
                        leftHand.HandModelEnabled = true;
                        Debug.Log("Left hand connected!");
                    }
                }
                else if (leftHand != null)
                {
                    if (leftHand.HandModelEnabled)
                    {
                        leftHand.HandModelEnabled = false;
                        Debug.Log("Left hand disconnected!");
                    }
                }
            }



    }

}