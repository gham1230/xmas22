using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class Grab : MonoBehaviour
{
    public XRController RightHandController;
    public XRController LeftHandController;

    public float interactSphereRadius = 0.05f;

    public GameObject ObjectToGrab;
    public GameObject otherObjectToGrab;
    public GameObject otherObjectToGra;

    private Transform handTransform; // Store the hand's transform

    private void Update()
    {
        CheckInteraction(RightHandController);
        CheckInteraction(LeftHandController);
    }

    private void CheckInteraction(XRController handController)
    {
        if (handController.inputDevice.TryGetFeatureValue(CommonUsages.gripButton, out bool gripPressed))
        {
            if (gripPressed)
            {
                Collider[] colliders = Physics.OverlapSphere(handController.transform.position, interactSphereRadius);

                foreach (Collider collider in colliders)
                {
                    if (collider.gameObject == ObjectToGrab ||
                        collider.gameObject == otherObjectToGrab ||
                        collider.gameObject == otherObjectToGra)
                    {
                        // Store the hand's transform
                        handTransform = handController.transform;

                        // Set the objects' positions to match the hand's position
                        ObjectToGrab.transform.position = handTransform.position;
                        otherObjectToGrab.transform.position = handTransform.position;
                        otherObjectToGra.transform.position = handTransform.position;

                        // Attach the objects to the hand's position without snapping
                        ObjectToGrab.transform.SetParent(handTransform, worldPositionStays: true);
                        otherObjectToGrab.transform.SetParent(handTransform, worldPositionStays: true);
                        otherObjectToGra.transform.SetParent(handTransform, worldPositionStays: true);
                    }
                }
            }
            else
            {
                // If the grip button is not pressed, detach objects from the hand
                if (handTransform != null)
                {
                    ObjectToGrab.transform.SetParent(null);
                    otherObjectToGrab.transform.SetParent(null);
                    otherObjectToGra.transform.SetParent(null);

                    handTransform = null; // Reset the handTransform
                }
            }
        }
    }
}
