using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlipButton : MonoBehaviour
{
    public GameObject FrontCamera;
    public GameObject BackCamera;
    public float delay = 0.25f;
    private bool isFront = true; // Start with front camera active

    private void OnTriggerEnter(Collider other)
    {
        if (!isFront)
        {
            StartCoroutine(ActivateCamera(FrontCamera));
        }
        else
        {
            StartCoroutine(ActivateCamera(BackCamera));
        }
    }

    private IEnumerator ActivateCamera(GameObject cameraToActivate)
    {
        // Deactivate the current camera
        if (isFront)
        {
            FrontCamera.SetActive(false);
        }
        else
        {
            BackCamera.SetActive(false);
        }

        // Activate the desired camera and wait for the delay
        cameraToActivate.SetActive(true);
        yield return new WaitForSeconds(delay);

        // Update the flag and deactivate the camera
        isFront = !isFront;
        cameraToActivate.SetActive(false);
    }
}
