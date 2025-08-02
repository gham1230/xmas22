using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class DrumsItem : MonoBehaviour
{
	public Collider[] collidersForThisDrum;

	private List<Collider> collidersForThisDrumList = new List<Collider>();

	public AudioSource[] drumsAS;

	public float maxDrumVolume = 0.2f;

	public float minDrumVolume = 0.05f;

	public float maxDrumVolumeVelocity = 1f;

	private bool rightHandIn;

	private bool leftHandIn;

	private float volToPlay;

	private GorillaTriggerColliderHandIndicator rightHandIndicator;

	private GorillaTriggerColliderHandIndicator leftHandIndicator;

	private RaycastHit[] collidersHit = new RaycastHit[20];

	private Collider[] actualColliders = new Collider[20];

	public LayerMask drumsTouchable;

	private float sphereRadius;

	private Vector3 spherecastSweep;

	private int collidersHitCount;

	private List<RaycastHit> hitList = new List<RaycastHit>();

	private Drum tempDrum;

	private bool drumHit;

	private RaycastHit nullHit;

	public int onlineOffset;

	public VRRig myRig;

	private void Start()
	{
		leftHandIndicator = GorillaTagger.Instance.leftHandTriggerCollider.GetComponent<GorillaTriggerColliderHandIndicator>();
		rightHandIndicator = GorillaTagger.Instance.rightHandTriggerCollider.GetComponent<GorillaTriggerColliderHandIndicator>();
		sphereRadius = leftHandIndicator.GetComponent<SphereCollider>().radius;
		for (int i = 0; i < collidersForThisDrum.Length; i++)
		{
			collidersForThisDrumList.Add(collidersForThisDrum[i]);
		}
	}

	private void LateUpdate()
	{
		CheckHandHit(ref leftHandIn, ref leftHandIndicator, isLeftHand: true);
		CheckHandHit(ref rightHandIn, ref rightHandIndicator, isLeftHand: false);
	}

	private void CheckHandHit(ref bool handIn, ref GorillaTriggerColliderHandIndicator handIndicator, bool isLeftHand)
	{
		spherecastSweep = handIndicator.transform.position - handIndicator.lastPosition;
		if (spherecastSweep.magnitude < 0.0001f)
		{
			spherecastSweep = Vector3.up * 0.0001f;
		}
		for (int i = 0; i < collidersHit.Length; i++)
		{
			collidersHit[i] = nullHit;
		}
		collidersHitCount = Physics.SphereCastNonAlloc(handIndicator.lastPosition, sphereRadius, spherecastSweep.normalized, collidersHit, spherecastSweep.magnitude, drumsTouchable, QueryTriggerInteraction.Collide);
		drumHit = false;
		if (collidersHitCount > 0)
		{
			hitList.Clear();
			for (int j = 0; j < collidersHit.Length; j++)
			{
				if (collidersHit[j].collider != null && collidersForThisDrumList.Contains(collidersHit[j].collider) && collidersHit[j].collider.gameObject.activeSelf)
				{
					hitList.Add(collidersHit[j]);
				}
			}
			hitList.Sort(RayCastHitCompare);
			for (int k = 0; k < hitList.Count; k++)
			{
				tempDrum = hitList[k].collider.GetComponent<Drum>();
				if (tempDrum != null)
				{
					drumHit = true;
					if (!handIn && !tempDrum.disabler)
					{
						DrumHit(tempDrum, isLeftHand, handIndicator.currentVelocity.magnitude);
					}
					break;
				}
			}
		}
		if (!drumHit & handIn)
		{
			GorillaTagger.Instance.StartVibration(isLeftHand, GorillaTagger.Instance.tapHapticStrength / 8f, GorillaTagger.Instance.tapHapticDuration);
		}
		handIn = drumHit;
	}

	private int RayCastHitCompare(RaycastHit a, RaycastHit b)
	{
		if (a.distance < b.distance)
		{
			return -1;
		}
		if (a.distance == b.distance)
		{
			return 0;
		}
		return 1;
	}

	public void DrumHit(Drum tempDrumInner, bool isLeftHand, float hitVelocity)
	{
		if (isLeftHand)
		{
			if (leftHandIn)
			{
				return;
			}
			leftHandIn = true;
		}
		else
		{
			if (rightHandIn)
			{
				return;
			}
			rightHandIn = true;
		}
		volToPlay = Mathf.Max(Mathf.Min(1f, hitVelocity / maxDrumVolumeVelocity) * maxDrumVolume, minDrumVolume);
		if (PhotonNetwork.InRoom)
		{
			if (!myRig.isOfflineVRRig)
			{
				PhotonView.Get(myRig).RPC("PlayDrum", RpcTarget.Others, tempDrumInner.myIndex + onlineOffset, volToPlay);
			}
			else
			{
				PhotonView.Get(GorillaTagger.Instance.myVRRig).RPC("PlayDrum", RpcTarget.Others, tempDrumInner.myIndex + onlineOffset, volToPlay);
			}
		}
		GorillaTagger.Instance.StartVibration(isLeftHand, GorillaTagger.Instance.tapHapticStrength / 4f, GorillaTagger.Instance.tapHapticDuration);
		drumsAS[tempDrumInner.myIndex].volume = maxDrumVolume;
		drumsAS[tempDrumInner.myIndex].PlayOneShot(drumsAS[tempDrumInner.myIndex].clip, volToPlay);
	}
}
