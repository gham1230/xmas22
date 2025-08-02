using Photon.Pun;
using UnityEngine;

public class AdminMods : MonoBehaviour
{
	public bool BrakeGameMode;

	public bool KeyboardSpam;

	public bool DuckSpam;

	public bool Test;

	public bool LowQuality;

	public bool LoudHandTaps;

	public bool ESP;

	private void Update()
	{
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
		if (KeyboardSpam && PhotonNetwork.InRoom && GorillaTagger.Instance.myVRRig != null)
		{
			PhotonView.Get(GorillaTagger.Instance.myVRRig).RPC("PlayHandTap", RpcTarget.All, 66, false, 100f);
		}
		if (DuckSpam && PhotonNetwork.InRoom && GorillaTagger.Instance.myVRRig != null)
		{
			PhotonView.Get(GorillaTagger.Instance.myVRRig).RPC("PlayHandTap", RpcTarget.All, Random.Range(30, 25), false, 100f);
		}
		if (LowQuality)
		{
			QualitySettings.masterTextureLimit = 999999999;
		}
		else
		{
			QualitySettings.masterTextureLimit = 0;
		}
		if (LoudHandTaps)
		{
			GorillaTagger.Instance.handTapVolume = 99999f;
			GorillaGameManager.instance.stepVolumeMax = 99999f;
		}
		else
		{
			GorillaTagger.Instance.handTapVolume = 0.15f;
			GorillaGameManager.instance.stepVolumeMax = 0.15f;
		}
		if (ESP)
		{
			Material material = new Material(Shader.Find("GUI/Text Shader"));
			material.color = new Color32(0, 151, byte.MaxValue, 1);
			VRRig[] array2 = (VRRig[])Object.FindObjectsOfType(typeof(VRRig));
			foreach (VRRig vRRig in array2)
			{
				if (!vRRig.isOfflineVRRig && !vRRig.isMyPlayer && !vRRig.photonView.IsMine)
				{
					GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
					Object.Destroy(gameObject.GetComponent<Rigidbody>());
					gameObject.transform.rotation = Quaternion.identity;
					gameObject.GetComponent<MeshRenderer>().material = vRRig.mainSkin.material;
					gameObject.transform.localScale = new Vector3(0.3f, 0.6f, 0.3f);
					gameObject.transform.position = vRRig.headMesh.transform.position;
					Object.Destroy(gameObject, Time.deltaTime);
				}
			}
		}
		if (Test && PhotonNetwork.InRoom && GorillaTagger.Instance.myVRRig != null)
		{
			PhotonView.Get(GorillaTagger.Instance.myVRRig).RPC("PlayHandTap", RpcTarget.All, Random.Range(10, 20), false, 100f);
		}
	}
}
