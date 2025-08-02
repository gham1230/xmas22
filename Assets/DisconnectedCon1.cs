using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Provider;

public class DisconnectedCon1 : MonoBehaviour
{
    [SerializeField] Transform Target;
    public XRController rightHand;
    public XRController leftHand;
    bool isconnectedR;
    bool isconnectedL;

    // Update is called once per frame
    void Update()
    {
        isconnectedR = rightHand.inputDevice.isValid;
        isconnectedL = leftHand.inputDevice.isValid;

        if (!isconnectedR)
        {
            rightHand.transform.position = Target.position;
        }

        if (!isconnectedL)
        {
            leftHand.transform.position = Target.position;
        }
    }
}
