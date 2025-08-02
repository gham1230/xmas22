using Photon.Pun;
using UnityEngine;
using UnityEngine.XR;

public class FingerFlagWearable : MonoBehaviour
{
	[Header("Wearable Settings")]
	public bool attachedToLeftHand = true;

	[Header("Bones")]
	public Transform pinkyRingBone;

	public Transform pinkyRingAttachPoint;

	public Transform thumbRingBone;

	public Transform thumbRingAttachPoint;

	public Transform[] clothBones;

	public Transform[] clothRigidbodies;

	[Header("Animation")]
	public Animator animator;

	public float extendSpeed = 1.5f;

	public float retractSpeed = 2.25f;

	[Header("Audio")]
	public AudioSource audioSource;

	public AudioClip extendAudioClip;

	public AudioClip retractAudioClip;

	[Header("Vibration")]
	public float extendVibrationDuration = 0.05f;

	public float extendVibrationStrength = 0.2f;

	public float retractVibrationDuration = 0.05f;

	public float retractVibrationStrength = 0.2f;

	private readonly int retractExtendTimeAnimParam = Animator.StringToHash("retractExtendTime");

	private bool networkedExtended;

	private bool extended;

	private bool fullyRetracted;

	private float retractExtendTime;

	private InputDevice inputDevice;

	private VRRig myRig;

	protected void Awake()
	{
		myRig = GetComponentInParent<VRRig>();
	}

	protected void OnEnable()
	{
		if (pinkyRingAttachPoint != null)
		{
			pinkyRingAttachPoint.gameObject.SetActive(value: true);
		}
		if (thumbRingAttachPoint != null)
		{
			thumbRingAttachPoint.gameObject.SetActive(value: true);
		}
		OnExtendStateChanged(playAudio: false);
	}

	protected void OnDisable()
	{
		if (pinkyRingAttachPoint != null)
		{
			pinkyRingAttachPoint.gameObject.SetActive(value: false);
		}
		if (thumbRingAttachPoint != null)
		{
			thumbRingAttachPoint.gameObject.SetActive(value: false);
		}
	}

	private void UpdateLocal()
	{
		int node = (attachedToLeftHand ? 4 : 5);
		bool flag = ControllerInputPoller.GripFloat((XRNode)node) > 0.25f;
		bool flag2 = ControllerInputPoller.PrimaryButtonPress((XRNode)node);
		bool flag3 = ControllerInputPoller.SecondaryButtonPress((XRNode)node);
		bool flag4 = flag && (flag2 || flag3);
		networkedExtended = flag4;
		if (PhotonNetwork.InRoom && (bool)myRig)
		{
			myRig.ExtraSerializedState = (networkedExtended ? 1 : 0);
		}
	}

	private void UpdateShared()
	{
		if (extended != networkedExtended)
		{
			extended = networkedExtended;
			OnExtendStateChanged(playAudio: true);
		}
		bool num = fullyRetracted;
		fullyRetracted = extended && retractExtendTime <= 0f;
		if (num != fullyRetracted)
		{
			Transform[] array = clothRigidbodies;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].gameObject.SetActive(!fullyRetracted);
			}
		}
		UpdateAnimation();
		UpdateBones();
	}

	private void UpdateReplicated()
	{
		if (myRig != null && !myRig.isOfflineVRRig)
		{
			networkedExtended = myRig.ExtraSerializedState != 0;
		}
	}

	public bool IsMyItem()
	{
		if (myRig != null)
		{
			return myRig.isOfflineVRRig;
		}
		return false;
	}

	protected void LateUpdate()
	{
		if (IsMyItem())
		{
			UpdateLocal();
		}
		else
		{
			UpdateReplicated();
		}
		UpdateShared();
	}

	private void UpdateAnimation()
	{
		float num = (extended ? extendSpeed : (0f - retractSpeed));
		retractExtendTime = Mathf.Clamp01(retractExtendTime + Time.deltaTime * num);
		animator.SetFloat(retractExtendTimeAnimParam, retractExtendTime);
	}

	private void UpdateBones()
	{
		for (int i = 0; i < clothBones.Length; i++)
		{
			clothBones[i].rotation = clothRigidbodies[i].rotation;
		}
		pinkyRingBone.SetPositionAndRotation(pinkyRingAttachPoint.position, pinkyRingAttachPoint.rotation);
		thumbRingBone.SetPositionAndRotation(thumbRingAttachPoint.position, thumbRingAttachPoint.rotation);
	}

	public void OnExtendStateChanged(bool playAudio)
	{
		audioSource.clip = (extended ? extendAudioClip : retractAudioClip);
		if (playAudio)
		{
			audioSource.Play();
		}
		if (IsMyItem() && (bool)GorillaTagger.Instance)
		{
			GorillaTagger.Instance.StartVibration(attachedToLeftHand, extended ? extendVibrationDuration : retractVibrationDuration, extended ? extendVibrationStrength : retractVibrationStrength);
		}
	}
}
