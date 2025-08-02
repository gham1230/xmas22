using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon;
using Photon.Pun;

public class AntiCheat : twiggypoop
{
    public GameObject player;
    private Rigidbody playerRB;
    public float speed;
    public float speedCap;

    [Header("If")]
    public GameObject message;

    void Update()
    {
        playerRB = player.GetComponent<Rigidbody>();
        speed = playerRB.velocity.magnitude;

        if (speed < 0.005f)
        {
            speed = 0f;
        }

        else
        {
            speed = playerRB.velocity.magnitude;
        }

        if (speed > speedCap)
        {
            StartCoroutine(Delay());
        }
    }


    IEnumerator Delay()
    {
        message.SetActive(true);
        yield return new WaitForSeconds(2f);
        Debug.Log("Player was caught cheating");
        Application.Quit();
    }
}
