using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    public bool IsFollowing;
    public GameObject Point;
    public GameObject CameraTablet;


    void Update()
    {
        if (IsFollowing)
        {
            Point.transform.position = CameraTablet.transform.position;
            Point.transform.rotation = CameraTablet.transform.rotation;
        }
    }
}
