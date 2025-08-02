using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MatChangeForCosmeticMod : MonoBehaviour
{
    [Header("made better by noobie")]
    public bool MatIsSet;

    public GameObject Gorilla;

    public Material NormalMata;

    public Material CustomMata;

    public Material PreessedMat;

    public Material UnPressedMat;

    public Renderer buttonRenderer;

    public GameObject Button;

    public Text ButtonText;

    private Renderer OfflineGorilla;

    private Renderer NetworkedGorilla;

    void Start()
    {
        buttonRenderer = Button.GetComponent<Renderer>();
        OfflineGorilla = Gorilla.GetComponent<Renderer>();
        NetworkedGorilla = GameObject.Find("Global/GorillaParent/GorillaVRRigs/Gorilla Player Networked(Clone)/gorilla").GetComponent<Renderer>();
    }

    private void OnTriggerEnter(Collider other)
    {

        MatIsSet = !MatIsSet;
        if (MatIsSet)
        {
            buttonRenderer.material = PreessedMat;
            OfflineGorilla.material = CustomMata;
            NetworkedGorilla.material = CustomMata;
            ButtonText.text = "TAKE OFF";
        }
        else
        {
            buttonRenderer.material = UnPressedMat;
            OfflineGorilla.material = NormalMata;
            NetworkedGorilla.material = NormalMata;
            ButtonText.text = "PUT ON";
        }
    }
}
