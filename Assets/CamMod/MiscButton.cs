using System.Collections;
using UnityEngine;

public class MiscButton : MonoBehaviour
{
    public GameObject[] objectsToDisable;
    public GameObject[] objectsToEnable;

    private bool isOn;
    public float delay = 0.25f;

    private void OnTriggerEnter(Collider other)
    {
        if (isOn)
        {
            StartCoroutine(DisableObjects());
        }
        else
        {
            StartCoroutine(EnableObjects());
        }
    }

    private IEnumerator EnableObjects()
    {
        foreach (GameObject obj in objectsToDisable)
        {
            obj.SetActive(false);
        }

        foreach (GameObject obj in objectsToEnable)
        {
            obj.SetActive(true);
        }
        isOn = true;

        yield return new WaitForSeconds(delay);
    }

    private IEnumerator DisableObjects()
    {
        foreach (GameObject obj in objectsToDisable)
        {
            obj.SetActive(true);
        }

        foreach (GameObject obj in objectsToEnable)
        {
            obj.SetActive(false);
        }
        isOn = false;

        yield return new WaitForSeconds(delay);
    }
}
