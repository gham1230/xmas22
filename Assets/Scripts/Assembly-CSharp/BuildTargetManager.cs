using UnityEngine;

public class BuildTargetManager : MonoBehaviour
{
	public enum BuildTowards
	{
		Steam = 0,
		OculusPC = 1,
		Quest = 2,
		Viveport = 3
	}

	public BuildTowards newBuildTarget;

	public bool isBeta;

	public bool isQA;

	public bool enableAllCosmetics;

	public OVRManager ovrManager;

	private string path = "Assets/csc.rsp";

	public BuildTowards currentBuildTargetDONOTCHANGE;

	public GorillaTagger gorillaTagger;

	public GameObject[] betaDisableObjects;

	public GameObject[] betaEnableObjects;

	public string GetPath()
	{
		return path;
	}
}
