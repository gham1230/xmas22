using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using easyInputs;

public class ModMenuManager : MonoBehaviour
{
    public GameObject ModMenu;

    public EasyHand hand;

    public GameObject Controller;

    void Update()
    {
        if (EasyInputs.GetSecondaryButtonTouched(hand))
        {
            ModMenu.SetActive(true);
        }
        else
        {
            ModMenu.SetActive(false);
        }
    }
}
