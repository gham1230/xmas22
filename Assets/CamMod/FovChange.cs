using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FovChange : MonoBehaviour
{
    public Camera Front;
    public Camera Back;

    private float minValue = 10f;
    private float maxValue = 130f;
    private float changeAmount = 5f;

    public TextMesh Fovtext;

    public bool isfovup;

    void OnTriggerEnter(Collider other)
    {
        float currentFov = Front.fieldOfView;

        if (isfovup && currentFov < maxValue)
        {
            Front.fieldOfView = Mathf.Clamp(currentFov + changeAmount, minValue, maxValue);
            Back.fieldOfView = Mathf.Clamp(currentFov + changeAmount, minValue, maxValue);
        }

        if (!isfovup && currentFov > minValue)
        {
            Front.fieldOfView = Mathf.Clamp(currentFov - changeAmount, minValue, maxValue);
            Back.fieldOfView = Mathf.Clamp(currentFov - changeAmount, minValue, maxValue);
        }

        // Update the TextMesh component with the new field of view value
        Fovtext.text = Front.fieldOfView.ToString();

        currentFov = Front.fieldOfView;
    }
}
