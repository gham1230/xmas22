using GorillaLocomotion;
using System.Collections.Generic;
using UnityEngine;
using GorillaNetworking;
using UnityEngine.XR;
using Photon.Pun;
using Photon.Realtime;
using easyInputs;
using ExitGames.Client.Photon;

public class Mods2 : MonoBehaviour
{
    [Header("Bools Of Mods But Better")]
    [Header("-----------------")]
    [Header("Cool PC Mods")]
    [Header("-----------------")]
    public bool LowQuality;
    public bool GhostMonke;
    public bool UpAndDown;
    public bool MosaSetiings;
    public bool Disconnect;
    public bool JoinRandomPublic;
    public bool LoudHit;
    public bool NoGrav;
    public bool BrakeGameMode;
    public bool Fly;
    public bool KeyboardSpam;
    public bool ESP;

    void Start()
    {
        
    }


    void Update()
    {


        if (LowQuality)
        {
            QualitySettings.masterTextureLimit = 999999999;
        }

        else
        {
            QualitySettings.masterTextureLimit = 0;
        }

        if (GhostMonke)
        {
            if (EasyInputs.GetPrimaryButtonDown(EasyHand.RightHand))
            {
                GorillaTagger.Instance.myVRRig.enabled = false;
            }

            else
            {
                GorillaTagger.Instance.myVRRig.enabled = true;
            }
        }

        if (UpAndDown)
        {
            if (EasyInputs.GetTriggerButtonDown(EasyHand.RightHand))
            {
                GorillaLocomotion.Player.Instance.GetComponent<Rigidbody>().AddForce(new Vector3(0f, 50f, 0f), ForceMode.Acceleration);
            }

            if (EasyInputs.GetTriggerButtonDown(EasyHand.LeftHand))
            {
                GorillaLocomotion.Player.Instance.GetComponent<Rigidbody>().AddForce(new Vector3(0f, -50f, 0f), ForceMode.Acceleration);
            }
        }

        if (MosaSetiings)
        {
            GorillaGameManager.instance.slowJumpLimit = 8.2f;
            GorillaGameManager.instance.slowJumpMultiplier = 1.4f;
        }

        if (Disconnect)
        {
            PhotonNetwork.Disconnect();
        }

        if (JoinRandomPublic)
        {
            PhotonNetwork.JoinRandomRoom();
        }

        if (LoudHit)
        {
            GorillaTagger.Instance.handTapVolume = 10f;
        }
        else
        {
            GorillaTagger.Instance.handTapVolume = 0.1f;
        }

        if (NoGrav)
        {
            Physics.gravity = new Vector3(0f, -3f, 0f);
        }
        else
        {
            Physics.gravity = new Vector3(0f, -9.81f, 0f);
        }

        if (BrakeGameMode)
        {
            foreach (GorillaTagManager gorillaTagManager in UnityEngine.Object.FindObjectsOfType<GorillaTagManager>())
            {
                gorillaTagManager.currentInfected.Clear();
                gorillaTagManager.InfectionEnd();
                gorillaTagManager.ClearInfectionState();
                gorillaTagManager.infectedModeThreshold = 0;
                gorillaTagManager.currentInfectedArray = new int[0];
            }
        }

        if (Fly)
        {
            if (EasyInputs.GetPrimaryButtonDown(EasyHand.RightHand))
            {
                GorillaLocomotion.Player.Instance.GetComponent<Rigidbody>().velocity = GorillaLocomotion.Player.Instance.headCollider.transform.forward * Time.deltaTime * 36f;    
            }
            else
            {
                GorillaLocomotion.Player.Instance.transform.position += GorillaLocomotion.Player.Instance.headCollider.transform.forward * Time.deltaTime * 30f;
                GorillaLocomotion.Player.Instance.GetComponent<Rigidbody>().velocity = Vector3.zero;
            }
 
        }

        if (KeyboardSpam)
        {
            if (PhotonNetwork.InRoom && GorillaTagger.Instance.myVRRig != null)
            {
                PhotonView.Get(GorillaTagger.Instance.myVRRig).RPC("PlayHandTap", RpcTarget.All, new object[]
                {
                    66,
                    false,
                    100f
                });
            }
        }

        if (ESP)
        {
            Material material = new Material(Shader.Find("GUI/Text Shader"));
            material.color = new Color32(0, 151, 255, 1);


            foreach (VRRig vrrig in (VRRig[])UnityEngine.Object.FindObjectsOfType(typeof(VRRig)))
            {
                if (!vrrig.isOfflineVRRig && !vrrig.isMyPlayer && !vrrig.photonView.IsMine)
                {
                    GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    UnityEngine.Object.Destroy(gameObject.GetComponent<Rigidbody>());
                    gameObject.transform.rotation = Quaternion.identity;
                    gameObject.GetComponent<MeshRenderer>().material = vrrig.mainSkin.material;
                    gameObject.transform.localScale = new Vector3(0.3f, 0.6f, 0.3f);
                    gameObject.transform.position = vrrig.headMesh.transform.position;
                    UnityEngine.Object.Destroy(gameObject, Time.deltaTime);
                }
            }
        }


        
    }

  
}
