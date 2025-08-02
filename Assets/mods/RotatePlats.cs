using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class RotatePlats : MonoBehaviour
{
	private InputDevice targetDevice;

	private GameObject Cube;

	public bool isSpawned = false;

	public GameObject PlatformPrefab;

	public Transform Hand;

	public InputDeviceCharacteristics Controller;

	private void Start()
	{
		List<InputDevice> list = new List<InputDevice>();
		InputDevices.GetDevicesWithCharacteristics(Controller, list);
		if (list.Count > 0)
		{
			targetDevice = list[0];
		}
	}

	private void Update()
	{
		if (targetDevice.TryGetFeatureValue(CommonUsages.grip, out var value) && value > 0.1f)
		{
			if (!isSpawned)
			{
				Object.Destroy(Cube);
				isSpawned = true;
				float x = Hand.transform.position.x;
				float z = Hand.transform.position.z;
				float y = Hand.transform.position.y - 0.1f;
				Cube = Object.Instantiate(PlatformPrefab, new Vector3(x, y, z), Quaternion.identity);
				Cube.transform.rotation = Hand.rotation;
			}
		}
		else
		{
			isSpawned = false;
			Object.Destroy(Cube);
		}
	}
}
