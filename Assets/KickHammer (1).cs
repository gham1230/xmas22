using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class KickHammer : MonoBehaviour
{
    public PhotonView PhotonView;
    public GameObject Cosmetic;

    // Awake is called when the script instance is being loaded
    void Awake()
    {
        InitializeKickHammer();
    }

    // FixedUpdate is called every fixed frame-rate frame
    void FixedUpdate()
    {
        if (PhotonView.IsMine)
        {
            HandleLocalPlayerKick();
        }
    }

    private void InitializeKickHammer()
    {
        // Add any initialization logic here
    }

    private void HandleLocalPlayerKick()
    {
        Cosmetic.SetActive(false);
    }
}
