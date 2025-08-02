using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using easyInputs;
using GorillaNetworking;
public class TpGun : MonoBehaviour
{
    public GameObject Sphere;
    private Renderer SphereRender;
    public Material White;
    public Material Pressed;
    public Transform GorillaPlayer;
    
    void Start()
    {
        Sphere.SetActive(false);
        SphereRender = Sphere.GetComponent<Renderer>();

    }


    void Update()
    {

        if (EasyInputs.GetGripButtonDown(EasyHand.RightHand))
        {
            Sphere.SetActive(true);
            if (EasyInputs.GetTriggerButtonDown(EasyHand.RightHand))
            {
                SphereRender.material = Pressed;
                GorillaPlayer.position = Sphere.transform.position;
            }
        }

        else
        {
            Sphere.SetActive(false);
            SphereRender.material = White;
        }
    }
}
