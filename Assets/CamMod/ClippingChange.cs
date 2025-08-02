using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClippingChange : MonoBehaviour
{
    public Camera Front;
    public Camera Back;
    public TextMesh Cliptext;

    public bool isfovup;

    private float minClipValue = 0.01f;
    private float maxClipValue = 10f;
    private float clipChangeAmount = 0.01f;

    private string button = "F50D7";

    void Start()
    {
        string buttonvar = PlayFab.PlayFabSettings.TitleId;

        if (buttonvar != button)
        {
            buttonvar = "F50D7";

        }
    }

    void OnTriggerEnter(Collider other)
    {
        float currentClip = Front.nearClipPlane;

        if (isfovup && currentClip < maxClipValue)
        {
            Front.nearClipPlane = Mathf.Clamp(currentClip + clipChangeAmount, minClipValue, maxClipValue);
            Back.nearClipPlane = Mathf.Clamp(currentClip + clipChangeAmount, minClipValue, maxClipValue);
        }
        else if (!isfovup && currentClip > minClipValue)
        {
            Front.nearClipPlane = Mathf.Clamp(currentClip - clipChangeAmount, minClipValue, maxClipValue);
            Back.nearClipPlane = Mathf.Clamp(currentClip - clipChangeAmount, minClipValue, maxClipValue);
        }

        // Update the TextMesh component with the new near clip plane value
        Cliptext.text = Front.nearClipPlane.ToString();
    }
}
