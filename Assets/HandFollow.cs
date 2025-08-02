using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandFollow : MonoBehaviour
{
    //Made By NotLucy For Fixing Buttons
    //made this the left hand controller collider and on the right hand controller Collider make it right hand controller
    public GameObject Hand;
    //make this the f_index.03.L and on the right make it d_index.03.R
    public GameObject FollowingHand;

    void Update()
    {
        //this will make it track
        Hand.transform.position = FollowingHand.transform.position;
        Hand.transform.rotation = FollowingHand.transform.rotation;
    }
}
