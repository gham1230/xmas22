using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeleteAllPlatforms : MonoBehaviour
{

    public string PlatformName;
    private GameObject Platform;

    void start()
    {
        Platform = GameObject.Find(PlatformName);
    }

    void Update()
    {
        Destroy(Platform);
    }
}
