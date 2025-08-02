using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ExitGames.Client.Photon;
using GorillaLocomotion;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.XR;

public class TagArua : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        bool flag = false;
        List<InputDevice> list = new List<InputDevice>();
        InputDevices.GetDevices(list);
        InputDevices.GetDevicesWithCharacteristics(UnityEngine.XR.InputDeviceCharacteristics.HeldInHand | UnityEngine.XR.InputDeviceCharacteristics.Left | UnityEngine.XR.InputDeviceCharacteristics.Controller, list);
        list[0].TryGetFeatureValue(CommonUsages.triggerButton, out flag);
        if (flag)
        {
            foreach (var player in PhotonNetwork.PlayerList)
            {
                PhotonView.Get(GorillaTagManager.instance.GetComponent<GorillaGameManager>()).RPC("ReportTagRPC", RpcTarget.MasterClient, new object[]
                {
                    player
                });
            }
        }
    }
}