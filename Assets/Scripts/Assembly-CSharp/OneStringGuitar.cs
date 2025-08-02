using System.Collections.Generic;
using GorillaLocomotion;
using Photon.Pun;
using UnityEngine;

public class OneStringGuitar : TransferrableObject
{
	private enum GuitarStates
	{
		Club = 1,
		HeldReverseGrip = 2,
		Playing = 4
	}

	public Vector3 chestOffsetLeft;

	public Vector3 chestOffsetRight;

	public Quaternion holdingOffsetRotationLeft;

	public Quaternion holdingOffsetRotationRight;

	public Quaternion chestRotationOffset;

	public Collider currentChestCollider;

	public Collider chestColliderLeft;

	public Collider chestColliderRight;

	public float lerpValue = 0.25f;

	public AudioSource audioSource;

	public Transform parentHand;

	public Transform parentHandLeft;

	public Transform parentHandRight;

	public float unsnapDistance;

	public float snapDistance;

	public Vector3 startPositionLeft;

	public Quaternion startQuatLeft;

	public Vector3 reverseGripPositionLeft;

	public Quaternion reverseGripQuatLeft;

	public Vector3 startPositionRight;

	public Quaternion startQuatRight;

	public Vector3 reverseGripPositionRight;

	public Quaternion reverseGripQuatRight;

	public float angleLerpSnap = 1f;

	public float vectorLerpSnap = 0.01f;

	private bool angleSnapped;

	private bool positionSnapped;

	public Transform chestTouch;

	private int collidersHitCount;

	private Collider[] collidersHit = new Collider[20];

	private RaycastHit[] raycastHits = new RaycastHit[20];

	private List<RaycastHit> raycastHitList = new List<RaycastHit>();

	private RaycastHit nullHit;

	public Collider[] collidersToBeIn;

	public LayerMask interactableMask;

	public int currentFretIndex;

	public int lastFretIndex;

	public Collider[] frets;

	private List<Collider> fretsList = new List<Collider>();

	public AudioClip[] audioClips;

	private GorillaTriggerColliderHandIndicator leftHandIndicator;

	private GorillaTriggerColliderHandIndicator rightHandIndicator;

	private GorillaTriggerColliderHandIndicator fretHandIndicator;

	private GorillaTriggerColliderHandIndicator strumHandIndicator;

	private float sphereRadius;

	private bool anyHit;

	private bool handIn;

	private Vector3 spherecastSweep;

	public Collider strumCollider;

	public float maxVolume = 1f;

	public float minVolume = 0.05f;

	public float maxVelocity = 2f;

	private List<Collider> strumList = new List<Collider>();

	public int selfInstrumentIndex;

	private GuitarStates lastState;

	protected override void Start()
	{
		base.Start();
		leftHandIndicator = GorillaTagger.Instance.leftHandTriggerCollider.GetComponent<GorillaTriggerColliderHandIndicator>();
		rightHandIndicator = GorillaTagger.Instance.rightHandTriggerCollider.GetComponent<GorillaTriggerColliderHandIndicator>();
		sphereRadius = leftHandIndicator.GetComponent<SphereCollider>().radius;
		itemState = ItemStates.State0;
		nullHit = default(RaycastHit);
		strumList.Add(strumCollider);
		lastState = GuitarStates.Club;
		for (int i = 0; i < frets.Length; i++)
		{
			fretsList.Add(frets[i]);
		}
		if (currentState == PositionState.InLeftHand)
		{
			fretHandIndicator = leftHandIndicator;
			strumHandIndicator = rightHandIndicator;
			if (myRig != null && myRig.isOfflineVRRig)
			{
				parentHand = Player.Instance.leftHandFollower;
			}
		}
		else
		{
			fretHandIndicator = rightHandIndicator;
			strumHandIndicator = leftHandIndicator;
			if (myRig != null && myRig.isOfflineVRRig)
			{
				parentHand = Player.Instance.rightHandFollower;
			}
		}
	}

	public override void OnRelease(DropZone zoneReleased, GameObject releasingHand)
	{
		base.OnRelease(zoneReleased, releasingHand);
		if (CanDeactivate() && !InHand())
		{
			itemState = ItemStates.State0;
		}
	}

	protected override void LateUpdateShared()
	{
		base.LateUpdateShared();
		if (lastState != (GuitarStates)itemState)
		{
			angleSnapped = false;
			positionSnapped = false;
		}
		if (itemState == ItemStates.State0)
		{
			Vector3 positionTarget = ((currentState == PositionState.InLeftHand) ? startPositionLeft : startPositionRight);
			Quaternion rotationTarget = ((currentState == PositionState.InLeftHand) ? startQuatLeft : startQuatRight);
			UpdateNonPlayingPosition(positionTarget, rotationTarget);
		}
		else if (itemState == ItemStates.State1)
		{
			Vector3 positionTarget2 = ((currentState == PositionState.InLeftHand) ? reverseGripPositionLeft : reverseGripPositionRight);
			Quaternion rotationTarget2 = ((currentState == PositionState.InLeftHand) ? reverseGripQuatLeft : reverseGripQuatRight);
			UpdateNonPlayingPosition(positionTarget2, rotationTarget2);
			if (IsMyItem() && (chestTouch.transform.position - currentChestCollider.transform.position).magnitude < snapDistance)
			{
				itemState = ItemStates.State2;
				angleSnapped = false;
				positionSnapped = false;
			}
		}
		else if (itemState == ItemStates.State2)
		{
			Quaternion quaternion = ((currentState == PositionState.InLeftHand) ? holdingOffsetRotationLeft : holdingOffsetRotationRight);
			Vector3 vector = ((currentState == PositionState.InLeftHand) ? chestOffsetLeft : chestOffsetRight);
			Quaternion quaternion2 = Quaternion.LookRotation(parentHand.position - currentChestCollider.transform.position) * quaternion;
			if (!angleSnapped && Quaternion.Angle(base.transform.rotation, quaternion2) > angleLerpSnap)
			{
				base.transform.rotation = Quaternion.Slerp(base.transform.rotation, quaternion2, lerpValue);
			}
			else
			{
				angleSnapped = true;
				base.transform.rotation = quaternion2;
			}
			Vector3 vector2 = currentChestCollider.transform.position + base.transform.rotation * vector;
			if (!positionSnapped && (base.transform.position - vector2).magnitude > vectorLerpSnap)
			{
				base.transform.position = Vector3.Lerp(base.transform.position, currentChestCollider.transform.position + base.transform.rotation * vector, lerpValue);
			}
			else
			{
				positionSnapped = true;
				base.transform.position = vector2;
			}
			if (currentState == PositionState.InRightHand)
			{
				parentHand = parentHandRight;
			}
			else
			{
				parentHand = parentHandLeft;
			}
			if (IsMyItem())
			{
				if (currentState == PositionState.InRightHand)
				{
					currentChestCollider = chestColliderRight;
					fretHandIndicator = rightHandIndicator;
					strumHandIndicator = leftHandIndicator;
				}
				else
				{
					currentChestCollider = chestColliderLeft;
					fretHandIndicator = leftHandIndicator;
					strumHandIndicator = rightHandIndicator;
				}
				if (Unsnap())
				{
					itemState = ItemStates.State1;
					angleSnapped = false;
					positionSnapped = false;
					if (currentState == PositionState.InLeftHand)
					{
						EquipmentInteractor.instance.wasLeftGrabPressed = true;
					}
					else
					{
						EquipmentInteractor.instance.wasRightGrabPressed = true;
					}
				}
				else
				{
					if (!handIn)
					{
						CheckFretFinger(fretHandIndicator.transform);
						HitChecker.CheckHandHit(ref collidersHitCount, interactableMask, sphereRadius, ref nullHit, ref raycastHits, ref raycastHitList, ref spherecastSweep, ref strumHandIndicator);
						if (collidersHitCount > 0)
						{
							for (int i = 0; i < collidersHitCount; i++)
							{
								if (raycastHits[i].collider != null && strumCollider == raycastHits[i].collider)
								{
									GorillaTagger.Instance.StartVibration(strumHandIndicator.isLeftHand, GorillaTagger.Instance.tapHapticStrength / 6f, GorillaTagger.Instance.tapHapticDuration);
									PlayNote(currentFretIndex, Mathf.Max(Mathf.Min(1f, strumHandIndicator.currentVelocity.magnitude / maxVelocity) * maxVolume, minVolume));
									if (PhotonNetwork.InRoom)
									{
										PhotonView.Get(GorillaTagger.Instance.myVRRig).RPC("PlaySelfOnlyInstrument", RpcTarget.Others, selfInstrumentIndex, currentFretIndex, audioSource.volume);
									}
									break;
								}
							}
						}
					}
					handIn = HitChecker.CheckHandIn(ref anyHit, ref collidersHit, sphereRadius, interactableMask, ref strumHandIndicator, ref strumList);
				}
			}
		}
		lastState = (GuitarStates)itemState;
	}

	public override void PlayNote(int note, float volume)
	{
		audioSource.time = 0.005f;
		audioSource.clip = audioClips[note];
		audioSource.volume = volume;
		audioSource.Play();
		base.PlayNote(note, volume);
	}

	private bool Unsnap()
	{
		return (parentHand.position - chestTouch.position).magnitude > unsnapDistance;
	}

	private void CheckFretFinger(Transform finger)
	{
		for (int i = 0; i < collidersHit.Length; i++)
		{
			collidersHit[i] = null;
		}
		collidersHitCount = Physics.OverlapSphereNonAlloc(finger.position, sphereRadius, collidersHit, interactableMask, QueryTriggerInteraction.Collide);
		currentFretIndex = 5;
		if (collidersHitCount > 0)
		{
			for (int j = 0; j < collidersHit.Length; j++)
			{
				if (fretsList.Contains(collidersHit[j]))
				{
					currentFretIndex = fretsList.IndexOf(collidersHit[j]);
					if (currentFretIndex != lastFretIndex)
					{
						GorillaTagger.Instance.StartVibration(fretHandIndicator.isLeftHand, GorillaTagger.Instance.tapHapticStrength / 6f, GorillaTagger.Instance.tapHapticDuration);
					}
					lastFretIndex = currentFretIndex;
					break;
				}
			}
		}
		else
		{
			if (lastFretIndex != -1)
			{
				GorillaTagger.Instance.StartVibration(fretHandIndicator.isLeftHand, GorillaTagger.Instance.tapHapticStrength / 6f, GorillaTagger.Instance.tapHapticDuration);
			}
			lastFretIndex = -1;
		}
	}

	public void UpdateNonPlayingPosition(Vector3 positionTarget, Quaternion rotationTarget)
	{
		if (!angleSnapped)
		{
			if (Quaternion.Angle(rotationTarget, base.transform.localRotation) < angleLerpSnap)
			{
				angleSnapped = true;
				base.transform.localRotation = rotationTarget;
			}
			else
			{
				base.transform.localRotation = Quaternion.Slerp(base.transform.localRotation, rotationTarget, lerpValue);
			}
		}
		if (!positionSnapped)
		{
			if ((base.transform.localPosition - positionTarget).magnitude < vectorLerpSnap)
			{
				positionSnapped = true;
				base.transform.localPosition = positionTarget;
			}
			else
			{
				base.transform.localPosition = Vector3.Lerp(base.transform.localPosition, positionTarget, lerpValue);
			}
		}
	}

	public override bool CanDeactivate()
	{
		if (itemState != ItemStates.State0)
		{
			return itemState == ItemStates.State1;
		}
		return true;
	}

	public override bool CanActivate()
	{
		if (itemState != ItemStates.State0)
		{
			return itemState == ItemStates.State1;
		}
		return true;
	}

	public override void OnActivate()
	{
		base.OnActivate();
		if (itemState == ItemStates.State0)
		{
			itemState = ItemStates.State1;
		}
		else
		{
			itemState = ItemStates.State0;
		}
	}

	public void GenerateVectorOffsetLeft()
	{
		chestOffsetLeft = base.transform.position - chestColliderLeft.transform.position;
		holdingOffsetRotationLeft = Quaternion.LookRotation(base.transform.position - chestColliderLeft.transform.position);
	}

	public void GenerateVectorOffsetRight()
	{
		chestOffsetRight = base.transform.position - chestColliderRight.transform.position;
		holdingOffsetRotationRight = Quaternion.LookRotation(base.transform.position - chestColliderRight.transform.position);
	}

	public void GenerateReverseGripOffsetLeft()
	{
		reverseGripPositionLeft = base.transform.localPosition;
		reverseGripQuatLeft = base.transform.localRotation;
	}

	public void GenerateClubOffsetLeft()
	{
		startPositionLeft = base.transform.localPosition;
		startQuatLeft = base.transform.localRotation;
	}

	public void GenerateReverseGripOffsetRight()
	{
		reverseGripPositionRight = base.transform.localPosition;
		reverseGripQuatRight = base.transform.localRotation;
	}

	public void GenerateClubOffsetRight()
	{
		startPositionRight = base.transform.localPosition;
		startQuatRight = base.transform.localRotation;
	}

	public void TestClubPositionRight()
	{
		base.transform.localPosition = startPositionRight;
		base.transform.localRotation = startQuatRight;
	}

	public void TestReverseGripPositionRight()
	{
		base.transform.localPosition = reverseGripPositionRight;
		base.transform.localRotation = reverseGripQuatRight;
	}

	public void TestPlayingPositionRight()
	{
		base.transform.rotation = Quaternion.LookRotation(parentHand.position - currentChestCollider.transform.position) * holdingOffsetRotationRight;
		base.transform.position = chestColliderRight.transform.position + base.transform.rotation * chestOffsetRight;
	}
}
