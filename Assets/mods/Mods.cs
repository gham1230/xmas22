using System.Collections.Generic;
using GorillaLocomotion;
using Photon.Pun;
using UnityEngine;
using UnityEngine.XR;
using easyInputs;

public class Mods : MonoBehaviour
{
	public bool disconnect;

	public bool joinbulic;

	public bool quit;

	private List<InputDevice> list;

	private bool flying = false;

	[Header("bools of mods [EDITED BY FANTAS]")]
	public bool IsGhostMonk;

	public GameObject GorillaPlayer;

	public bool UpAndDown;

	public bool IsFlyMonk;

	public bool BrakeGameMode;

	public bool NoTagFreeze;

	public bool TagFreeze;

	public bool SlideControl;

	public bool NoSlideControl;

	public bool WalkFreeze;

	public bool Longarms;

	public bool normalarms;

	public bool ShakeHand;

	public bool SpazHead;

	public bool InvisSelf;

	private Quaternion Spaz()
	{
		return Quaternion.Euler(Random.Range(-360, 360), Random.Range(-360, 360), Random.Range(-360, 360));
	}

	private void Update()
	{
		if (IsGhostMonk)
		{
			bool value = false;
			list = new List<InputDevice>();
			InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Right, list);
			list[0].TryGetFeatureValue(CommonUsages.primaryButton, out value);
			if (value)
			{
				GorillaTagger.Instance.myVRRig.enabled = false;
			}
			else
			{
				GorillaTagger.Instance.myVRRig.enabled = true;
			}
		}
		if (IsFlyMonk)
		{
			bool value2 = false;
			bool value3 = false;
			list = new List<InputDevice>();
			InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Right, list);
			list[0].TryGetFeatureValue(CommonUsages.primaryButton, out value2);
			list[0].TryGetFeatureValue(CommonUsages.secondaryButton, out value3);
			if (value2)
			{
				Player.Instance.transform.position += Player.Instance.headCollider.transform.forward * Time.deltaTime * 30f;
				Player.Instance.GetComponent<Rigidbody>().velocity = Vector3.zero;
				if (!flying)
				{
					flying = true;
				}
			}
			else if (flying)
			{
				Player.Instance.GetComponent<Rigidbody>().velocity = Player.Instance.headCollider.transform.forward * Time.deltaTime * 36f;
				flying = false;
			}
		}
		if (NoTagFreeze)
		{
			Player.Instance.disableMovement = false;
		}
		if (Longarms)
		{
			GorillaPlayer.transform.localScale *= 1.5f;
		}
		if (normalarms)
		{
			GorillaPlayer.transform.localScale = Vector3.one;
		}
		if (ShakeHand)
		{
			GameObject gameObject = GameObject.Find("Global/GorillaParent/GorillaVRRigs/Gorilla Player Networked(Clone)/VR Constraints/RightArm/Right Arm IK/TargetWrist");
			GameObject gameObject2 = GameObject.Find("Global/GorillaParent/GorillaVRRigs/Gorilla Player Networked(Clone)/VR Constraints/LeftArm/Left Arm IK/TargetWrist");
			gameObject.transform.rotation = Spaz();
			gameObject2.transform.rotation = Spaz();
		}
		if (SpazHead)
		{
			GameObject gameObject3 = GameObject.Find("Global/GorillaParent/GorillaVRRigs/Gorilla Player Networked(Clone)/VR Constraints/Head Constraint");
			gameObject3.transform.rotation = Spaz();
		}
		if (InvisSelf)
		{
			bool value4 = false;
			list = new List<InputDevice>();
			InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Right, list);
			list[0].TryGetFeatureValue(CommonUsages.primaryButton, out value4);
			if (value4)
			{
				GorillaTagger.Instance.myVRRig.enabled = false;
				GorillaTagger.Instance.myVRRig.transform.position = new Vector3(100f, 100f, 100f);
			}
			else
			{
				GorillaTagger.Instance.myVRRig.enabled = true;
			}
		}
		if (UpAndDown)
		{
			if (EasyInputs.GetTriggerButtonDown(EasyHand.RightHand))
			{
				Player.Instance.GetComponent<Rigidbody>().AddForce(new Vector3(0f, 1000f, 0f), ForceMode.Acceleration);
			}
			if (EasyInputs.GetTriggerButtonDown(EasyHand.LeftHand))
			{
				Player.Instance.GetComponent<Rigidbody>().AddForce(new Vector3(0f, -1000f, 0f), ForceMode.Acceleration);
			}
		}
		if (!WalkFreeze)
		{
			return;
		}
		bool value5 = false;
		list = new List<InputDevice>();
		InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Right, list);
		list[0].TryGetFeatureValue(CommonUsages.primaryButton, out value5);
		if (value5)
		{
			GorillaTagger.Instance.myVRRig.enabled = false;
			GameObject gameObject4 = GameObject.Find("Player VR Controller/GorillaPlayer/TurnParent/RightHand Controller");
			GorillaTagger.Instance.myVRRig.transform.position = GorillaPlayer.transform.position;
		}
		else
		{
			GorillaTagger.Instance.myVRRig.enabled = true;
		}
		if (BrakeGameMode)
		{
			GorillaTagManager[] array = Object.FindObjectsOfType<GorillaTagManager>();
			foreach (GorillaTagManager gorillaTagManager in array)
			{
				gorillaTagManager.currentInfected.Clear();
				gorillaTagManager.InfectionEnd();
				gorillaTagManager.ClearInfectionState();
				gorillaTagManager.infectedModeThreshold = 0;
				gorillaTagManager.currentInfectedArray = new int[0];
			}
		}
		if (disconnect)
		{
			PhotonNetwork.Disconnect();
		}
		if (joinbulic)
		{
			PhotonNetwork.JoinRandomRoom();
		}
		if (quit)
		{
			Application.Quit();
		}
	}
}
