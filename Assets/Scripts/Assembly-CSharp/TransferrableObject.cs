using System;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.XR;

public class TransferrableObject : HoldableObject
{
	public enum ItemStates
	{
		State0 = 1,
		State1 = 2,
		State2 = 4,
		State3 = 8,
		State4 = 0x10
	}

	[Flags]
	public enum PositionState
	{
		OnLeftArm = 1,
		OnRightArm = 2,
		InLeftHand = 4,
		InRightHand = 8,
		OnChest = 0x10,
		OnLeftShoulder = 0x20,
		OnRightShoulder = 0x40,
		Dropped = 0x80,
		None = 0
	}

	public enum InterpolateState
	{
		None = 0,
		Interpolating = 1
	}

	protected EquipmentInteractor interactor;

	public VRRig myRig;

	public VRRig myOnlineRig;

	public bool latched;

	private float indexTrigger;

	public bool testActivate;

	public bool testDeactivate;

	public float myThreshold = 0.8f;

	public float hysterisis = 0.05f;

	public bool flipOnXForLeftHand;

	public bool flipOnYForLeftHand;

	public bool flipOnXForLeftArm;

	public bool disableStealing;

	private PositionState initState;

	public ItemStates itemState;

	public BodyDockPositions.DropPositions storedZone;

	protected PositionState previousState;

	public PositionState currentState;

	public BodyDockPositions.DropPositions dockPositions;

	public VRRig targetRig;

	public BodyDockPositions targetDock;

	private VRRigAnchorOverrides anchorOverrides;

	public bool canAutoGrabLeft;

	public bool canAutoGrabRight;

	public int objectIndex;

	[Tooltip("In Holdables.prefab, assign to the parent of this transform.\nExample: 'Holdables/YellowHandBootsRight' is the anchor of 'Holdables/YellowHandBootsRight/YELLOW HAND BOOTS'")]
	public Transform anchor;

	[Tooltip("In Holdables.prefab, assign to the Collider to grab this object")]
	public InteractionPoint gripInteractor;

	[Tooltip("(Optional) Use this to override the transform used when the object is in the hand.\nExample: 'GHOST BALLOON' uses child 'grabPtAnchor' which is the end of the balloon's string.")]
	public Transform grabAnchor;

	public int myIndex;

	[Tooltip("(Optional)")]
	public GameObject[] gameObjectsActiveOnlyWhileHeld;

	protected GameObject worldShareableInstance;

	private float interpTime = 0.1f;

	private float interpDt;

	private Vector3 interpStartPos;

	private Quaternion interpStartRot;

	protected int enabledOnFrame = -1;

	private Vector3 initOffset;

	private Quaternion initRotation;

	public bool canDrop;

	public bool shareable;

	public bool detatchOnGrab;

	private bool wasHover;

	private bool isHover;

	private bool disableItem;

	public const int kPositionStateCount = 8;

	public InterpolateState interpState;

	protected virtual void Awake()
	{
		latched = false;
		initOffset = base.transform.localPosition;
		initRotation = base.transform.localRotation;
	}

	protected virtual void Start()
	{
	}

	public override void OnEnable()
	{
		base.OnEnable();
		if (myRig == null && myOnlineRig != null && myOnlineRig.photonView != null && myOnlineRig.photonView.IsMine)
		{
			base.gameObject.SetActive(value: false);
			return;
		}
		if (myRig == null && myOnlineRig == null)
		{
			base.gameObject.SetActive(value: false);
			return;
		}
		objectIndex = targetDock.ReturnTransferrableItemIndex(myIndex);
		if (myRig != null && myRig.isOfflineVRRig)
		{
			if (currentState == PositionState.OnLeftArm)
			{
				storedZone = BodyDockPositions.DropPositions.LeftArm;
			}
			else if (currentState == PositionState.OnRightArm)
			{
				storedZone = BodyDockPositions.DropPositions.RightArm;
			}
			else if (currentState == PositionState.OnLeftShoulder)
			{
				storedZone = BodyDockPositions.DropPositions.LeftBack;
			}
			else if (currentState == PositionState.OnRightShoulder)
			{
				storedZone = BodyDockPositions.DropPositions.RightBack;
			}
			else
			{
				storedZone = BodyDockPositions.DropPositions.Chest;
			}
		}
		if (objectIndex == -1)
		{
			base.gameObject.SetActive(value: false);
			return;
		}
		if (currentState == PositionState.OnLeftArm && flipOnXForLeftArm)
		{
			Transform transform = GetAnchor(currentState);
			transform.localScale = new Vector3(0f - transform.localScale.x, transform.localScale.y, transform.localScale.z);
		}
		initState = currentState;
		enabledOnFrame = Time.frameCount;
		SpawnShareableObject();
	}

	public override void OnDisable()
	{
		base.OnDisable();
		enabledOnFrame = -1;
	}

	private void SpawnShareableObject()
	{
		if (PhotonNetwork.InRoom && (canDrop || shareable) && !(worldShareableInstance != null))
		{
			object[] data = new object[2]
			{
				myIndex,
				PhotonNetwork.LocalPlayer
			};
			worldShareableInstance = PhotonNetwork.Instantiate("Objects/equipment/WorldShareableItem", base.transform.position, base.transform.rotation, 0, data);
			if (myRig != null && worldShareableInstance != null)
			{
				OnWorldShareableItemSpawn();
			}
		}
	}

	public override void OnJoinedRoom()
	{
		base.OnJoinedRoom();
		SpawnShareableObject();
	}

	public override void OnLeftRoom()
	{
		base.OnLeftRoom();
		if (worldShareableInstance != null)
		{
			PhotonNetwork.Destroy(worldShareableInstance);
		}
		OnWorldShareableItemDeallocated(PhotonNetwork.LocalPlayer);
	}

	public override void OnPlayerLeftRoom(Player otherPlayer)
	{
		base.OnPlayerLeftRoom(otherPlayer);
		OnWorldShareableItemDeallocated(otherPlayer);
	}

	public void SetWorldShareableItem(GameObject item)
	{
		worldShareableInstance = item;
		OnWorldShareableItemSpawn();
	}

	protected virtual void OnWorldShareableItemSpawn()
	{
	}

	protected virtual void OnWorldShareableItemDeallocated(Player player)
	{
	}

	public virtual void LateUpdate()
	{
		if (interactor == null)
		{
			interactor = EquipmentInteractor.instance;
		}
		if (IsMyItem())
		{
			LateUpdateLocal();
		}
		else
		{
			LateUpdateReplicated();
		}
		LateUpdateShared();
		previousState = currentState;
	}

	protected Transform DefaultAnchor()
	{
		if (!(anchor == null))
		{
			return anchor;
		}
		return base.transform;
	}

	private Transform GetAnchor(PositionState pos)
	{
		if (grabAnchor == null)
		{
			return DefaultAnchor();
		}
		if (InHand())
		{
			return grabAnchor;
		}
		return DefaultAnchor();
	}

	protected bool Attached()
	{
		bool flag = InHand() && detatchOnGrab;
		if (!Dropped())
		{
			return !flag;
		}
		return false;
	}

	private void UpdateFollowXform()
	{
		if (targetRig == null)
		{
			return;
		}
		if (targetDock == null)
		{
			targetDock = targetRig.GetComponent<BodyDockPositions>();
		}
		if (anchorOverrides == null)
		{
			anchorOverrides = targetRig.GetComponent<VRRigAnchorOverrides>();
		}
		Transform transform = GetAnchor(currentState);
		Transform transform2 = transform;
		switch (currentState)
		{
		case PositionState.OnLeftArm:
			transform2 = anchorOverrides.AnchorOverride(currentState, targetDock.leftArmTransform);
			break;
		case PositionState.OnRightArm:
			transform2 = anchorOverrides.AnchorOverride(currentState, targetDock.rightArmTransform);
			break;
		case PositionState.InLeftHand:
			transform2 = anchorOverrides.AnchorOverride(currentState, targetDock.leftHandTransform);
			break;
		case PositionState.InRightHand:
			transform2 = anchorOverrides.AnchorOverride(currentState, targetDock.rightHandTransform);
			break;
		case PositionState.OnChest:
			transform2 = anchorOverrides.AnchorOverride(currentState, targetDock.chestTransform);
			break;
		case PositionState.OnLeftShoulder:
			transform2 = anchorOverrides.AnchorOverride(currentState, targetDock.leftBackTransform);
			break;
		case PositionState.OnRightShoulder:
			transform2 = anchorOverrides.AnchorOverride(currentState, targetDock.rightBackTransform);
			break;
		}
		switch (interpState)
		{
		case InterpolateState.None:
			if (transform2 != transform.parent)
			{
				if (Time.frameCount == enabledOnFrame)
				{
					transform.parent = transform2;
					transform.localPosition = Vector3.zero;
					transform.localRotation = Quaternion.identity;
				}
				else
				{
					interpState = InterpolateState.Interpolating;
					interpDt = interpTime;
					interpStartPos = transform.transform.position;
					interpStartRot = transform.transform.rotation;
				}
			}
			break;
		case InterpolateState.Interpolating:
		{
			float t = Mathf.Clamp((interpTime - interpDt) / interpTime, 0f, 1f);
			transform.transform.position = Vector3.Lerp(interpStartPos, transform2.transform.position, t);
			transform.transform.rotation = Quaternion.Slerp(interpStartRot, transform2.transform.rotation, t);
			interpDt -= Time.deltaTime;
			if (interpDt <= 0f)
			{
				transform.parent = transform2;
				interpState = InterpolateState.None;
				transform.localPosition = Vector3.zero;
				transform.localRotation = Quaternion.identity;
				transform.localScale = Vector3.one;
				if (flipOnXForLeftHand && currentState == PositionState.InLeftHand)
				{
					transform.localScale = new Vector3(-1f, 1f, 1f);
				}
				if (flipOnYForLeftHand && currentState == PositionState.InLeftHand)
				{
					transform.localScale = new Vector3(1f, -1f, 1f);
				}
			}
			break;
		}
		}
	}

	public void DropItem()
	{
		base.transform.parent = null;
	}

	protected virtual void LateUpdateShared()
	{
		disableItem = true;
		for (int i = 0; i < targetRig.ActiveTransferrableObjectIndexLength(); i++)
		{
			if (targetRig.ActiveTransferrableObjectIndex(i) == myIndex)
			{
				disableItem = false;
				break;
			}
		}
		if (disableItem)
		{
			base.gameObject.SetActive(value: false);
			return;
		}
		if (previousState != currentState && detatchOnGrab && InHand())
		{
			base.transform.parent = null;
		}
		if (currentState != PositionState.Dropped)
		{
			UpdateFollowXform();
		}
		else if (canDrop)
		{
			DropItem();
		}
	}

	protected void ResetXf()
	{
		if (canDrop)
		{
			Transform transform = DefaultAnchor();
			if (base.transform != transform && base.transform.parent != transform)
			{
				base.transform.parent = transform;
			}
			base.transform.localPosition = initOffset;
			base.transform.localRotation = initRotation;
		}
	}

	protected void ReDock()
	{
		if (IsMyItem())
		{
			currentState = initState;
		}
		ResetXf();
	}

	private void HandleLocalInput()
	{
		GameObject[] array;
		if (!InHand())
		{
			array = gameObjectsActiveOnlyWhileHeld;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].SetActive(value: false);
			}
			return;
		}
		array = gameObjectsActiveOnlyWhileHeld;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetActive(value: true);
		}
		XRNode node = ((currentState == PositionState.InLeftHand) ? XRNode.LeftHand : XRNode.RightHand);
		indexTrigger = ControllerInputPoller.TriggerFloat(node);
		bool num = !latched && indexTrigger >= myThreshold;
		bool flag = latched && indexTrigger < myThreshold - hysterisis;
		if (num || testActivate)
		{
			testActivate = false;
			if (CanActivate())
			{
				OnActivate();
			}
		}
		else if (flag || testDeactivate)
		{
			testDeactivate = false;
			if (CanDeactivate())
			{
				OnDeactivate();
			}
		}
	}

	protected virtual void LateUpdateLocal()
	{
		wasHover = isHover;
		isHover = false;
		if (PhotonNetwork.InRoom)
		{
			myRig.SetTransferrablePosStates(objectIndex, currentState);
			myRig.SetTransferrableItemStates(objectIndex, itemState);
		}
		targetRig = myRig;
		HandleLocalInput();
	}

	protected virtual void LateUpdateReplicated()
	{
		currentState = myOnlineRig.TransferrablePosStates(objectIndex);
		itemState = myOnlineRig.TransferrableItemStates(objectIndex);
		targetRig = myOnlineRig;
		if (!(myOnlineRig != null))
		{
			return;
		}
		bool flag = true;
		for (int i = 0; i < myOnlineRig.ActiveTransferrableObjectIndexLength(); i++)
		{
			if (myOnlineRig.ActiveTransferrableObjectIndex(i) == myIndex)
			{
				flag = false;
				GameObject[] array = gameObjectsActiveOnlyWhileHeld;
				for (int j = 0; j < array.Length; j++)
				{
					array[j].SetActive(InHand());
				}
			}
		}
		if (flag)
		{
			base.gameObject.SetActive(value: false);
		}
	}

	public virtual void ResetToDefaultState()
	{
		canAutoGrabLeft = true;
		canAutoGrabRight = true;
		wasHover = false;
		isHover = false;
		ResetXf();
	}

	public override void OnGrab(InteractionPoint pointGrabbed, GameObject grabbingHand)
	{
		if (!IsMyItem())
		{
			return;
		}
		if (grabbingHand == interactor.leftHand && currentState != PositionState.OnLeftArm)
		{
			if (currentState != PositionState.InRightHand || !disableStealing)
			{
				canAutoGrabLeft = false;
				currentState = PositionState.InLeftHand;
				EquipmentInteractor.instance.UpdateHandEquipment(this, forLeftHand: true);
				GorillaTagger.Instance.StartVibration(forLeftController: true, GorillaTagger.Instance.tapHapticStrength / 8f, GorillaTagger.Instance.tapHapticDuration * 0.5f);
			}
		}
		else if (grabbingHand == interactor.rightHand && currentState != PositionState.OnRightArm && (currentState != PositionState.InLeftHand || !disableStealing))
		{
			canAutoGrabRight = false;
			currentState = PositionState.InRightHand;
			EquipmentInteractor.instance.UpdateHandEquipment(this, forLeftHand: false);
			GorillaTagger.Instance.StartVibration(forLeftController: false, GorillaTagger.Instance.tapHapticStrength / 8f, GorillaTagger.Instance.tapHapticDuration * 0.5f);
		}
	}

	public override void OnRelease(DropZone zoneReleased, GameObject releasingHand)
	{
		if (!IsMyItem() || !CanDeactivate() || !IsHeld() || ((!(releasingHand == EquipmentInteractor.instance.rightHand) || !(this == EquipmentInteractor.instance.rightHandHeldEquipment)) && (!(releasingHand == EquipmentInteractor.instance.leftHand) || !(this == EquipmentInteractor.instance.leftHandHeldEquipment))))
		{
			return;
		}
		if (releasingHand == EquipmentInteractor.instance.leftHand)
		{
			canAutoGrabLeft = true;
		}
		else
		{
			canAutoGrabRight = true;
		}
		if (zoneReleased != null)
		{
			bool num = currentState == PositionState.InLeftHand && zoneReleased.dropPosition == BodyDockPositions.DropPositions.LeftArm;
			bool flag = currentState == PositionState.InRightHand && zoneReleased.dropPosition == BodyDockPositions.DropPositions.RightArm;
			if (num || flag)
			{
				return;
			}
			if (targetDock.DropZoneStorageUsed(zoneReleased.dropPosition) == -1 && zoneReleased.forBodyDock == targetDock && (zoneReleased.dropPosition & dockPositions) != 0)
			{
				storedZone = zoneReleased.dropPosition;
			}
		}
		DropItemCleanup();
		EquipmentInteractor.instance.UpdateHandEquipment(null, releasingHand == EquipmentInteractor.instance.leftHand);
	}

	public override void DropItemCleanup()
	{
		if (canDrop)
		{
			currentState = PositionState.Dropped;
			return;
		}
		switch (storedZone)
		{
		case BodyDockPositions.DropPositions.LeftArm:
			currentState = PositionState.OnLeftArm;
			break;
		case BodyDockPositions.DropPositions.RightArm:
			currentState = PositionState.OnRightArm;
			break;
		case BodyDockPositions.DropPositions.Chest:
			currentState = PositionState.OnChest;
			break;
		case BodyDockPositions.DropPositions.LeftBack:
			currentState = PositionState.OnLeftShoulder;
			break;
		case BodyDockPositions.DropPositions.RightBack:
			currentState = PositionState.OnRightShoulder;
			break;
		}
	}

	public virtual void OnHover(InteractionPoint pointHovered, GameObject hoveringHand)
	{
		if (IsMyItem())
		{
			if (!wasHover)
			{
				GorillaTagger.Instance.StartVibration(hoveringHand == EquipmentInteractor.instance.leftHand, GorillaTagger.Instance.tapHapticStrength / 8f, GorillaTagger.Instance.tapHapticDuration * 0.5f);
			}
			isHover = true;
		}
	}

	protected void ActivateItemFX(float hapticStrength, float hapticDuration, int soundIndex, float soundVolume)
	{
		bool flag = currentState == PositionState.InLeftHand;
		if (myOnlineRig != null)
		{
			PhotonView.Get(myOnlineRig).RPC("PlayHandTap", RpcTarget.Others, soundIndex, flag, 0.1f);
		}
		myRig.PlayHandTapLocal(soundIndex, flag, soundVolume);
		GorillaTagger.Instance.StartVibration(flag, hapticStrength, hapticDuration);
	}

	public virtual void PlayNote(int note, float volume)
	{
	}

	public virtual bool AutoGrabTrue(bool leftGrabbingHand)
	{
		if (!leftGrabbingHand)
		{
			return canAutoGrabRight;
		}
		return canAutoGrabLeft;
	}

	public virtual bool CanActivate()
	{
		return true;
	}

	public virtual bool CanDeactivate()
	{
		return true;
	}

	public virtual void OnActivate()
	{
		latched = true;
	}

	public virtual void OnDeactivate()
	{
		latched = false;
	}

	public virtual bool IsMyItem()
	{
		if (myRig != null)
		{
			return myRig.isOfflineVRRig;
		}
		return false;
	}

	protected virtual bool IsHeld()
	{
		if (!(EquipmentInteractor.instance.leftHandHeldEquipment == this))
		{
			return EquipmentInteractor.instance.rightHandHeldEquipment == this;
		}
		return true;
	}

	public bool InHand()
	{
		if (currentState != PositionState.InLeftHand)
		{
			return currentState == PositionState.InRightHand;
		}
		return true;
	}

	public bool Dropped()
	{
		return currentState == PositionState.Dropped;
	}

	public bool InLeftHand()
	{
		return currentState == PositionState.InLeftHand;
	}

	public bool InRightHand()
	{
		return currentState == PositionState.InRightHand;
	}

	public bool OnChest()
	{
		return currentState == PositionState.OnChest;
	}

	public bool OnShoulder()
	{
		if (currentState != PositionState.OnLeftShoulder)
		{
			return currentState == PositionState.OnRightShoulder;
		}
		return true;
	}

	protected Player OwningPlayer()
	{
		if (myRig == null)
		{
			return myOnlineRig.photonView.Owner;
		}
		return PhotonNetwork.LocalPlayer;
	}
}
