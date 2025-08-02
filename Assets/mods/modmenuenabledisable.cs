using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class modmenuenabledisable : MonoBehaviour
{
    public GameObject controllers;
    public float proximityRange;
    public Material enabled;
    public Material disable;
    public GameObject theMod;

    private Renderer rend;
    private bool isEnabled = false;
    private bool isInRange = false;

    void Start()
    {
        rend = GetComponent<Renderer>();
        rend.material = disable;
        theMod.SetActive(false);
    }

    void Update()
    {
        float distance = Vector3.Distance(transform.position, controllers.transform.position);

        if (distance <= proximityRange && !isInRange)
        {
            isInRange = true;
            isEnabled = !isEnabled;
            rend.material = isEnabled ? enabled : disable;
            theMod.SetActive(!isEnabled);
        }
        else if (distance > proximityRange && isInRange)
        {
            isInRange = false;
        }
    }
}
