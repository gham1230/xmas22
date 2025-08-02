using Photon.Pun;
using UnityEngine;

public class RubberDuck : TransferrableObject
{
	public bool disableActivation;

	public bool disableDeactivation;

	private SkinnedMeshRenderer skinRenderer;

	public float duckieLerp;

	private int tempHandPos;

	[GorillaSoundLookup]
	public int squeezeSound = 75;

	[GorillaSoundLookup]
	public int squeezeReleaseSound = 76;

	public float squeezeStrength = 0.05f;

	public float releaseStrength = 0.03f;

	protected override void Awake()
	{
		base.Awake();
		skinRenderer = GetComponent<SkinnedMeshRenderer>();
		myThreshold = 0.7f;
		hysterisis = 0.3f;
	}

	public override void LateUpdate()
	{
		base.LateUpdate();
		float num = 0f;
		if (InHand())
		{
			tempHandPos = ((myOnlineRig != null) ? myOnlineRig.ReturnHandPosition() : myRig.ReturnHandPosition());
			num = ((currentState != PositionState.InLeftHand) ? ((float)Mathf.FloorToInt((float)(tempHandPos % 10) / 1f)) : ((float)Mathf.FloorToInt((float)(tempHandPos % 10000) / 1000f)));
		}
		skinRenderer.SetBlendShapeWeight(0, Mathf.Lerp(skinRenderer.GetBlendShapeWeight(0), num * 11.1f, duckieLerp));
	}

	public override void OnActivate()
	{
		base.OnActivate();
		if (IsMyItem())
		{
			bool flag = currentState == PositionState.InLeftHand;
			if (PhotonNetwork.InRoom && GorillaGameManager.instance != null)
			{
				GorillaGameManager.instance.FindVRRigForPlayer(PhotonNetwork.LocalPlayer).RPC("PlayHandTap", RpcTarget.Others, squeezeSound, flag, 0.33f);
			}
			myRig.PlayHandTapLocal(squeezeSound, flag, 0.33f);
			GorillaTagger.Instance.StartVibration(flag, squeezeStrength, Time.deltaTime);
		}
	}

	public override void OnDeactivate()
	{
		base.OnDeactivate();
		if (IsMyItem())
		{
			bool flag = currentState == PositionState.InLeftHand;
			if (PhotonNetwork.InRoom && GorillaGameManager.instance != null)
			{
				GorillaGameManager.instance.FindVRRigForPlayer(PhotonNetwork.LocalPlayer).RPC("PlayHandTap", RpcTarget.Others, squeezeReleaseSound, flag, 0.33f);
			}
			myRig.PlayHandTapLocal(squeezeReleaseSound, flag, 0.33f);
			GorillaTagger.Instance.StartVibration(flag, releaseStrength, Time.deltaTime);
		}
	}

	public override bool CanActivate()
	{
		return !disableActivation;
	}

	public override bool CanDeactivate()
	{
		return !disableDeactivation;
	}
}
