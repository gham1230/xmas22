using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using easyInputs;
using GorillaNetworking;
public class GunTemplateByFantas : MonoBehaviour
{
    public GameObject Sphere;
    private Renderer SphereRender;
    public Material White;
    public Material Pressed;
    public GameObject RightHand;
    //dont worry about all of this

    void Start()
    {
        Sphere.SetActive(false);
        //make sure it disables on start
        SphereRender = Sphere.GetComponent<Renderer>();
        //render
    }


    void Update()
    {

        if (EasyInputs.GetGripButtonDown(EasyHand.RightHand))
        {
            SphereRender.material = White;
            //Material Stuff
            Sphere.SetActive(true);
            if (EasyInputs.GetTriggerButtonDown(EasyHand.RightHand))
            {
                SphereRender.material = Pressed;

                //Put Your Code Here



                
            }
        }

        else
        {
            Sphere.SetActive(false);
            //when grip is not callled the spheres disables
        }
    }
}
