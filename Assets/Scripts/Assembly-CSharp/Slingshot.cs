using GorillaLocomotion;
using GorillaNetworking;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.Serialization;

public class Slingshot : TransferrableObject
{
	public enum SlingshotState
	{
		NoState = 1,
		OnChest = 2,
		LeftHandDrawing = 4,
		RightHandDrawing = 8
	}

	public enum SlingshotActions
	{
		Grab = 0,
		Release = 1
	}

	[FormerlySerializedAs("elastic")]
	public LineRenderer elasticLeft;

	public LineRenderer elasticRight;

	public Transform leftArm;

	public Transform rightArm;

	public Transform center;

	public Transform centerOrigin;

	private GameObject dummyProjectile;

	public GameObject drawingHand;

	public GameObject projectilePrefab;

	public GameObject projectileTrail;

	public InteractionPoint nock;

	public InteractionPoint grip;

	public float springConstant;

	public float maxDraw;

	public Transform leftHandSnap;

	public Transform rightHandSnap;

	public Transform chestSnap;

	public AudioClip[] shootSfxClips;

	public AudioSource shootSfx;

	public bool disableWhenNotInRoom;

	private bool hasDummyProjectile;

	private float delayLaunchTime = 0.07f;

	private float minTimeToLaunch = -1f;

	private float dummyProjectileColliderRadius;

	private float dummyProjectileInitialScale;

	private int projectileCount;

	private Vector3[] elasticLeftPoints = new Vector3[2];

	private Vector3[] elasticRightPoints = new Vector3[2];

	private void DestroyDummyProjectile()
	{
		if (hasDummyProjectile)
		{
			dummyProjectile.transform.localScale = Vector3.one * dummyProjectileInitialScale;
			ObjectPools.instance.Destroy(dummyProjectile);
			dummyProjectile = null;
			hasDummyProjectile = false;
		}
	}

	public override void OnEnable()
	{
		currentState = PositionState.OnChest;
		itemState = ItemStates.State0;
		elasticLeft.positionCount = 2;
		elasticRight.positionCount = 2;
		dummyProjectile = null;
		if (myOnlineRig != null)
		{
			myOnlineRig.slingshot = this;
		}
		if (myRig != null)
		{
			myRig.slingshot = this;
		}
		base.OnEnable();
	}

	public override void OnDisable()
	{
		DestroyDummyProjectile();
		base.OnDisable();
	}

	protected override void LateUpdateShared()
	{
		base.LateUpdateShared();
		float x = base.transform.lossyScale.x;
		Vector3 vector;
		if (InDrawingState())
		{
			if (!hasDummyProjectile)
			{
				dummyProjectile = ObjectPools.instance.Instantiate(projectilePrefab);
				hasDummyProjectile = true;
				SphereCollider component = dummyProjectile.GetComponent<SphereCollider>();
				dummyProjectileColliderRadius = component.radius;
				dummyProjectileInitialScale = dummyProjectile.transform.localScale.x;
				GetIsOnTeams(out var blueTeam, out var orangeTeam);
				dummyProjectile.GetComponent<SlingshotProjectile>().ColorizeProjectile(blueTeam, orangeTeam);
			}
			float num = dummyProjectileInitialScale * x;
			dummyProjectile.transform.localScale = Vector3.one * num;
			Vector3 position = drawingHand.transform.position;
			Vector3 position2 = centerOrigin.position;
			Vector3 normalized = (position2 - position).normalized;
			float num2 = (EquipmentInteractor.instance.grabRadius - dummyProjectileColliderRadius) * x;
			vector = position + normalized * num2;
			dummyProjectile.transform.position = vector;
			dummyProjectile.transform.rotation = Quaternion.LookRotation(position2 - vector, Vector3.up);
		}
		else
		{
			DestroyDummyProjectile();
			vector = centerOrigin.position;
		}
		center.position = vector;
		elasticLeftPoints[0] = leftArm.position;
		elasticLeftPoints[1] = (elasticRightPoints[0] = vector);
		elasticRightPoints[1] = rightArm.position;
		elasticLeft.SetPositions(elasticLeftPoints);
		elasticRight.SetPositions(elasticRightPoints);
		if (!PhotonNetwork.InRoom && disableWhenNotInRoom)
		{
			base.gameObject.SetActive(value: false);
		}
	}

	protected override void LateUpdateLocal()
	{
		base.LateUpdateLocal();
		if (InDrawingState())
		{
			if (ForLeftHandSlingshot())
			{
				drawingHand = EquipmentInteractor.instance.rightHand;
			}
			else
			{
				drawingHand = EquipmentInteractor.instance.leftHand;
			}
		}
	}

	protected override void LateUpdateReplicated()
	{
		base.LateUpdateReplicated();
		if (InDrawingState())
		{
			if (ForLeftHandSlingshot())
			{
				drawingHand = rightHandSnap.gameObject;
			}
			else
			{
				drawingHand = leftHandSnap.gameObject;
			}
		}
	}

	public static bool IsSlingShotEnabled()
	{
		if (GorillaTagger.Instance == null || GorillaTagger.Instance.offlineVRRig == null)
		{
			return false;
		}
		return GorillaTagger.Instance.offlineVRRig.cosmeticSet.HasItemOfCategory(CosmeticsController.CosmeticCategory.Slingshot);
	}

	private void GetIsOnTeams(out bool blueTeam, out bool orangeTeam)
	{
		Photon.Realtime.Player player = OwningPlayer();
		blueTeam = false;
		orangeTeam = false;
		if (GorillaGameManager.instance != null)
		{
			GorillaBattleManager component = GorillaGameManager.instance.GetComponent<GorillaBattleManager>();
			if (component != null)
			{
				blueTeam = component.OnBlueTeam(player);
				orangeTeam = component.OnRedTeam(player);
			}
		}
	}

	private void AttachTrail(int trailHash, GameObject newProjectile, Vector3 location, bool blueTeam, bool orangeTeam)
	{
		SlingshotProjectileTrail component = ObjectPools.instance.Instantiate(trailHash).GetComponent<SlingshotProjectileTrail>();
		newProjectile.transform.position = location;
		component.AttachTrail(newProjectile, blueTeam, orangeTeam);
	}

	private void PlayLaunchSfx()
	{
		if (shootSfx != null && shootSfxClips != null && shootSfxClips.Length != 0)
		{
			shootSfx.PlayOneShot(shootSfxClips[Random.Range(0, shootSfxClips.Length)]);
		}
	}

	private void LaunchProjectile()
	{
		int num = PoolUtils.GameObjHashCode(projectilePrefab);
		int num2 = PoolUtils.GameObjHashCode(projectileTrail);
		GameObject gameObject = ObjectPools.instance.Instantiate(num);
		float scale = Mathf.Abs(base.transform.lossyScale.x);
		Vector3 position = dummyProjectile.transform.position;
		Vector3 vector = centerOrigin.position - center.position;
		Vector3 currentVelocity = GorillaLocomotion.Player.Instance.currentVelocity;
		Vector3 vector2 = Mathf.Min(springConstant * maxDraw, vector.magnitude * springConstant) * vector.normalized + currentVelocity;
		GetIsOnTeams(out var blueTeam, out var orangeTeam);
		AttachTrail(num2, gameObject, position, blueTeam, orangeTeam);
		SlingshotProjectile component = gameObject.GetComponent<SlingshotProjectile>();
		if (GorillaGameManager.instance != null)
		{
			int num3 = GorillaGameManager.instance.IncrementLocalPlayerProjectileCount();
			component.Launch(position, vector2, PhotonNetwork.LocalPlayer, blueTeam, orangeTeam, num3, scale);
			bool flag = currentState == PositionState.InLeftHand;
			GorillaGameManager.instance.photonView.RPC("LaunchSlingshotProjectile", RpcTarget.Others, position, vector2, num, num2, flag, num3);
			PlayLaunchSfx();
		}
		else
		{
			component.Launch(position, vector2, PhotonNetwork.LocalPlayer, blueTeam, orangeTeam, 0, scale);
			PlayLaunchSfx();
		}
	}

	public void LaunchNetworkedProjectile(Vector3 location, Vector3 velocity, int projHash, int trailHash, int projectileCounter, float scale, PhotonMessageInfo info)
	{
		GameObject gameObject = ObjectPools.instance.Instantiate(projHash);
		GetIsOnTeams(out var blueTeam, out var orangeTeam);
		if (trailHash != -1)
		{
			AttachTrail(trailHash, gameObject, location, blueTeam, orangeTeam);
		}
		gameObject.GetComponent<SlingshotProjectile>().Launch(location, velocity, info.Sender, blueTeam, orangeTeam, projectileCounter, scale);
		PlayLaunchSfx();
	}

	public override void OnGrab(InteractionPoint pointGrabbed, GameObject grabbingHand)
	{
		if (!IsMyItem())
		{
			return;
		}
		bool flag = pointGrabbed == nock;
		if (flag && !InHand())
		{
			return;
		}
		base.OnGrab(pointGrabbed, grabbingHand);
		if (!InDrawingState() && !OnChest() && flag)
		{
			if (grabbingHand == interactor.leftHand)
			{
				interactor.disableLeftGrab = true;
			}
			else
			{
				interactor.disableRightGrab = true;
			}
			if (ForLeftHandSlingshot())
			{
				itemState = ItemStates.State2;
			}
			else
			{
				itemState = ItemStates.State3;
			}
			minTimeToLaunch = Time.time + delayLaunchTime;
			GorillaTagger.Instance.StartVibration(!ForLeftHandSlingshot(), GorillaTagger.Instance.tapHapticStrength / 2f, GorillaTagger.Instance.tapHapticDuration * 1.5f);
		}
	}

	public override void OnRelease(DropZone zoneReleased, GameObject releasingHand)
	{
		base.OnRelease(zoneReleased, releasingHand);
		if (!IsMyItem())
		{
			return;
		}
		if (InDrawingState() && releasingHand == drawingHand)
		{
			if (releasingHand == interactor.leftHand)
			{
				EquipmentInteractor.instance.disableLeftGrab = false;
			}
			else
			{
				EquipmentInteractor.instance.disableRightGrab = false;
			}
			if (ForLeftHandSlingshot())
			{
				currentState = PositionState.InLeftHand;
			}
			else
			{
				currentState = PositionState.InRightHand;
			}
			itemState = ItemStates.State0;
			GorillaTagger.Instance.StartVibration(ForLeftHandSlingshot(), GorillaTagger.Instance.tapHapticStrength * 2f, GorillaTagger.Instance.tapHapticDuration * 1.5f);
			if (Time.time > minTimeToLaunch)
			{
				LaunchProjectile();
			}
		}
		else
		{
			EquipmentInteractor.instance.disableLeftGrab = false;
			EquipmentInteractor.instance.disableRightGrab = false;
		}
	}

	public override void DropItemCleanup()
	{
		base.DropItemCleanup();
		currentState = PositionState.OnChest;
		itemState = ItemStates.State0;
	}

	public override bool AutoGrabTrue(bool leftGrabbingHand)
	{
		return true;
	}

	private bool ForLeftHandSlingshot()
	{
		if (itemState != ItemStates.State2)
		{
			return currentState == PositionState.InLeftHand;
		}
		return true;
	}

	private bool InDrawingState()
	{
		if (itemState != ItemStates.State2)
		{
			return itemState == ItemStates.State3;
		}
		return true;
	}
}
