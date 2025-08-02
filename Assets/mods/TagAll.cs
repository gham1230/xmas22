using System.Collections;
using GorillaNetworking;
using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;

public class TagAll : GorillaPressableButton
{
    public float buttonFadeTime = 0.25f;

    // Start is called before the first frame update
    void Start()
    {
        foreach (var player in PhotonNetwork.PlayerList)
        {
            PhotonView.Get(GorillaTagManager.instance.GetComponent<GorillaGameManager>()).RPC("ReportTagRPC", RpcTarget.MasterClient, new object[]
            {
                                        player
            });
        }
    }
    public override void ButtonActivation()
    {
        {
            foreach (var player in PhotonNetwork.PlayerList)
            {
                PhotonView.Get(GorillaTagManager.instance.GetComponent<GorillaGameManager>()).RPC("ReportTagRPC", RpcTarget.MasterClient, new object[]
                {
                                        player
                });
            }
        }
        base.ButtonActivation();
        StartCoroutine(ButtonColorUpdate());
    }
    // Update is called once per frame
    private IEnumerator ButtonColorUpdate()
    {
        buttonRenderer.material = pressedMaterial;
        yield return new WaitForSeconds(buttonFadeTime);
        buttonRenderer.material = unpressedMaterial;
    }
}
