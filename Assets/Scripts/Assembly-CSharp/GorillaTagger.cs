using System.Collections;
using GorillaLocomotion;
using GorillaNetworking;
using Photon.Pun;
using Photon.Realtime;
using Photon.Voice.Unity;
using UnityEngine;
using UnityEngine.XR;

public class GorillaTagger : MonoBehaviour
{
	public enum StatusEffect
	{
		None = 0,
		Frozen = 1,
		Slowed = 2,
		Dead = 3,
		Infected = 4,
		It = 5
	}

	private static GorillaTagger _instance;

	public static bool hasInstance;

	public bool inCosmeticsRoom;

	public SphereCollider headCollider;

	public CapsuleCollider bodyCollider;

	private Vector3 lastLeftHandPositionForTag;

	private Vector3 lastRightHandPositionForTag;

	private Vector3 lastBodyPositionForTag;

	private Vector3 lastHeadPositionForTag;

	public Transform rightHandTransform;

	public Transform leftHandTransform;

	public float hapticWaitSeconds = 0.05f;

	public float handTapVolume = 0.1f;

	public float tapCoolDown = 0.15f;

	public float lastLeftTap;

	public float lastRightTap;

	public float tapHapticDuration = 0.05f;

	public float tapHapticStrength = 0.5f;

	public float tagHapticDuration = 0.15f;

	public float tagHapticStrength = 1f;

	public float taggedHapticDuration = 0.35f;

	public float taggedHapticStrength = 1f;

	private bool leftHandTouching;

	private bool rightHandTouching;

	public float taggedTime;

	public float tagCooldown;

	public float slowCooldown = 3f;

	public VRRig myVRRig;

	public VRRig offlineVRRig;

	public GameObject mainCamera;

	public bool testTutorial;

	public bool disableTutorial;

	public bool frameRateUpdated;

	public GameObject leftHandTriggerCollider;

	public GameObject rightHandTriggerCollider;

	public Camera mirrorCamera;

	public AudioSource leftHandSlideSource;

	public AudioSource rightHandSlideSource;

	public bool overrideNotInFocus;

	private Vector3 leftRaycastSweep;

	private Vector3 leftHeadRaycastSweep;

	private Vector3 rightRaycastSweep;

	private Vector3 rightHeadRaycastSweep;

	private Vector3 headRaycastSweep;

	private Vector3 bodyRaycastSweep;

	private InputDevice rightDevice;

	private InputDevice leftDevice;

	private bool primaryButtonPressRight;

	private bool secondaryButtonPressRight;

	private bool primaryButtonPressLeft;

	private bool secondaryButtonPressLeft;

	private RaycastHit hitInfo;

	public Photon.Realtime.Player otherPlayer;

	private Photon.Realtime.Player tryPlayer;

	private Vector3 topVector;

	private Vector3 bottomVector;

	private Vector3 bodyVector;

	private int tempInt;

	private InputDevice inputDevice;

	private bool wasInOverlay;

	private PhotonView tempView;

	public StatusEffect currentStatus;

	public float statusStartTime;

	public float statusEndTime;

	private float refreshRate;

	private float baseSlideControl;

	private int gorillaTagColliderLayerMask;

	private RaycastHit[] nonAllocRaycastHits = new RaycastHit[30];

	private int nonAllocHits;

	private Recorder myRecorder;

	public static GorillaTagger Instance => _instance;

	public float sphereCastRadius => 0.03f;

	protected void Awake()
	{
		if (_instance != null && _instance != this)
		{
			Object.Destroy(base.gameObject);
		}
		else
		{
			_instance = this;
			hasInstance = true;
		}
		if (!disableTutorial && (testTutorial || (PlayerPrefs.GetString("tutorial") != "done" && PhotonNetworkController.Instance.gameVersion != "dev")))
		{
			base.transform.parent.position = new Vector3(-140f, 28f, -102f);
			base.transform.parent.eulerAngles = new Vector3(0f, 180f, 0f);
			GorillaLocomotion.Player.Instance.InitializeValues();
			PlayerPrefs.SetFloat("redValue", Random.value);
			PlayerPrefs.SetFloat("greenValue", Random.value);
			PlayerPrefs.SetFloat("blueValue", Random.value);
			PlayerPrefs.Save();
			UpdateColor(PlayerPrefs.GetFloat("redValue", 0f), PlayerPrefs.GetFloat("greenValue", 0f), PlayerPrefs.GetFloat("blueValue", 0f));
		}
		inputDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
		wasInOverlay = false;
		baseSlideControl = GorillaLocomotion.Player.Instance.slideControl;
		gorillaTagColliderLayerMask = LayerMask.GetMask("Gorilla Tag Collider");
	}

	protected void Start()
	{
		if (XRSettings.loadedDeviceName == "OpenVR")
		{
			GorillaLocomotion.Player.Instance.leftHandOffset = new Vector3(-0.02f, 0f, -0.07f);
			GorillaLocomotion.Player.Instance.rightHandOffset = new Vector3(0.02f, 0f, -0.07f);
		}
		bodyVector = new Vector3(0f, bodyCollider.height / 2f - bodyCollider.radius, 0f);
	}

	protected void LateUpdate()
	{
		/*if (OpenVR.Overlay != null)
		{
			if (!OpenVR.Overlay.IsDashboardVisible() && !overrideNotInFocus)
			{
				if (!leftHandTriggerCollider.activeSelf)
				{
					leftHandTriggerCollider.SetActive(value: true);
					rightHandTriggerCollider.SetActive(value: true);
				}
				GorillaLocomotion.Player.Instance.inOverlay = false;
			}
			else
			{
				if (leftHandTriggerCollider.activeSelf)
				{
					leftHandTriggerCollider.SetActive(value: false);
					rightHandTriggerCollider.SetActive(value: true);
				}
				GorillaLocomotion.Player.Instance.inOverlay = true;
			}
		}*/
		if (XRSettings.loadedDeviceName == "Oculus")
		{
			if (OVRManager.hasInputFocus && !overrideNotInFocus)
			{
				if (!leftHandTriggerCollider.activeSelf)
				{
					leftHandTriggerCollider.SetActive(value: true);
					rightHandTriggerCollider.SetActive(value: true);
				}
				GorillaLocomotion.Player.Instance.inOverlay = false;
				if (wasInOverlay && CosmeticsController.instance != null)
				{
					CosmeticsController.instance.LeaveSystemMenu();
				}
				wasInOverlay = false;
			}
			else
			{
				if (leftHandTriggerCollider.activeSelf)
				{
					leftHandTriggerCollider.SetActive(value: false);
					rightHandTriggerCollider.SetActive(value: true);
				}
				GorillaLocomotion.Player.Instance.inOverlay = true;
				wasInOverlay = true;
			}
		}
		/*
		if (XRDevice.isPresent && Application.platform != RuntimePlatform.Android)
		{
			if (Mathf.Abs(Time.fixedDeltaTime - 1f / XRDevice.refreshRate) > 0.0001f)
			{
				Time.fixedDeltaTime = 1f / XRDevice.refreshRate;
				GorillaLocomotion.Player.Instance.velocityHistorySize = Mathf.Max(Mathf.Min(Mathf.FloorToInt(XRDevice.refreshRate * (1f / 12f)), 10), 6);
				if (GorillaLocomotion.Player.Instance.velocityHistorySize > 9)
				{
					GorillaLocomotion.Player.Instance.velocityHistorySize--;
				}
				Debug.Log("new history size: " + GorillaLocomotion.Player.Instance.velocityHistorySize);
				GorillaLocomotion.Player.Instance.slideControl = 1f - CalcSlideControl(XRDevice.refreshRate);
				GorillaLocomotion.Player.Instance.InitializeValues();
			}
		}*/
		else if (Application.platform != RuntimePlatform.Android && OVRManager.instance != null && OVRManager.OVRManagerinitialized && OVRManager.instance.gameObject != null && OVRManager.instance.gameObject.activeSelf)
		{
			Object.Destroy(OVRManager.instance.gameObject);
		}
		/*
		if (!frameRateUpdated && Application.platform == RuntimePlatform.Android && OVRManager.instance.gameObject.activeSelf)
		{
			int num = OVRManager.display.displayFrequenciesAvailable.Length - 1;
			float num2;
			for (num2 = OVRManager.display.displayFrequenciesAvailable[num]; num2 > 90f; num2 = OVRManager.display.displayFrequenciesAvailable[num])
			{
				num--;
				if (num < 0)
				{
					break;
				}
			}
			if (Mathf.Abs(Time.fixedDeltaTime - 1f / num2 * 0.98f) > 0.0001f)
			{
				Time.fixedDeltaTime = 1f / num2 * 0.98f;
				OVRPlugin.systemDisplayFrequency = num2;
				GorillaLocomotion.Player.Instance.velocityHistorySize = Mathf.FloorToInt(num2 * (1f / 12f));
				if (GorillaLocomotion.Player.Instance.velocityHistorySize > 9)
				{
					GorillaLocomotion.Player.Instance.velocityHistorySize--;
				}
				GorillaLocomotion.Player.Instance.slideControl = 1f - CalcSlideControl(XRDevice.refreshRate);
				GorillaLocomotion.Player.Instance.InitializeValues();
				OVRManager.instance.gameObject.SetActive(value: false);
				frameRateUpdated = true;
			}
		}
		if (!XRDevice.isPresent && Application.platform != RuntimePlatform.Android && Mathf.Abs(Time.fixedDeltaTime - 1f / 144f) > 0.0001f)
		{
			Debug.Log("updating delta time. was: " + Time.fixedDeltaTime + ". now it's " + 1f / 144f);
			Application.targetFrameRate = 144;
			Time.fixedDeltaTime = 1f / 144f;
			GorillaLocomotion.Player.Instance.velocityHistorySize = Mathf.Min(Mathf.FloorToInt(12f), 10);
			if (GorillaLocomotion.Player.Instance.velocityHistorySize > 9)
			{
				GorillaLocomotion.Player.Instance.velocityHistorySize--;
			}
			Debug.Log("new history size: " + GorillaLocomotion.Player.Instance.velocityHistorySize);
			GorillaLocomotion.Player.Instance.slideControl = 1f - CalcSlideControl(144f);
			GorillaLocomotion.Player.Instance.InitializeValues();
		}*/
		leftRaycastSweep = leftHandTransform.position - lastLeftHandPositionForTag;
		leftHeadRaycastSweep = leftHandTransform.position - headCollider.transform.position;
		rightRaycastSweep = rightHandTransform.position - lastRightHandPositionForTag;
		rightHeadRaycastSweep = rightHandTransform.position - headCollider.transform.position;
		headRaycastSweep = headCollider.transform.position - lastHeadPositionForTag;
		bodyRaycastSweep = bodyCollider.transform.position - lastBodyPositionForTag;
		otherPlayer = null;
		float num3 = sphereCastRadius * GorillaLocomotion.Player.Instance.scale;
		nonAllocHits = Physics.SphereCastNonAlloc(lastLeftHandPositionForTag, num3, leftRaycastSweep.normalized, nonAllocRaycastHits, Mathf.Max(leftRaycastSweep.magnitude, num3), gorillaTagColliderLayerMask, QueryTriggerInteraction.Collide);
		if (nonAllocHits > 0 && TryToTag(nonAllocRaycastHits[0], isBodyTag: false, out tryPlayer))
		{
			otherPlayer = tryPlayer;
		}
		nonAllocHits = Physics.SphereCastNonAlloc(headCollider.transform.position, num3, leftHeadRaycastSweep.normalized, nonAllocRaycastHits, Mathf.Max(leftHeadRaycastSweep.magnitude, num3), gorillaTagColliderLayerMask, QueryTriggerInteraction.Collide);
		if (nonAllocHits > 0 && TryToTag(nonAllocRaycastHits[0], isBodyTag: false, out tryPlayer))
		{
			otherPlayer = tryPlayer;
		}
		nonAllocHits = Physics.SphereCastNonAlloc(lastRightHandPositionForTag, num3, rightRaycastSweep.normalized, nonAllocRaycastHits, Mathf.Max(rightRaycastSweep.magnitude, num3), gorillaTagColliderLayerMask, QueryTriggerInteraction.Collide);
		if (nonAllocHits > 0 && TryToTag(nonAllocRaycastHits[0], isBodyTag: false, out tryPlayer))
		{
			otherPlayer = tryPlayer;
		}
		nonAllocHits = Physics.SphereCastNonAlloc(headCollider.transform.position, num3, rightHeadRaycastSweep.normalized, nonAllocRaycastHits, Mathf.Max(rightHeadRaycastSweep.magnitude, num3), gorillaTagColliderLayerMask, QueryTriggerInteraction.Collide);
		if (nonAllocHits > 0 && TryToTag(nonAllocRaycastHits[0], isBodyTag: false, out tryPlayer))
		{
			otherPlayer = tryPlayer;
		}
		nonAllocHits = Physics.SphereCastNonAlloc(headCollider.transform.position, headCollider.radius * headCollider.transform.localScale.x * GorillaLocomotion.Player.Instance.scale, headRaycastSweep.normalized, nonAllocRaycastHits, Mathf.Max(headRaycastSweep.magnitude, num3), gorillaTagColliderLayerMask, QueryTriggerInteraction.Collide);
		if (nonAllocHits > 0 && TryToTag(nonAllocRaycastHits[0], isBodyTag: true, out tryPlayer))
		{
			otherPlayer = tryPlayer;
		}
		topVector = lastBodyPositionForTag + bodyVector;
		bottomVector = lastBodyPositionForTag - bodyVector;
		nonAllocHits = Physics.CapsuleCastNonAlloc(topVector, bottomVector, bodyCollider.radius * 2f * GorillaLocomotion.Player.Instance.scale, bodyRaycastSweep.normalized, nonAllocRaycastHits, Mathf.Max(bodyRaycastSweep.magnitude, num3), gorillaTagColliderLayerMask, QueryTriggerInteraction.Collide);
		if (nonAllocHits > 0 && TryToTag(nonAllocRaycastHits[0], isBodyTag: true, out tryPlayer))
		{
			otherPlayer = tryPlayer;
		}
		if (otherPlayer != null && GorillaGameManager.instance != null)
		{
			Debug.Log("tagging someone yeet");
			PhotonView.Get(GorillaGameManager.instance.GetComponent<GorillaGameManager>()).RPC("ReportTagRPC", RpcTarget.MasterClient, otherPlayer);
		}
		if (myVRRig == null && PhotonNetwork.InRoom)
		{
			foreach (VRRig vrrig in GorillaParent.instance.vrrigs)
			{
				if (vrrig != null && !vrrig.isOfflineVRRig && vrrig.photonView != null && vrrig.photonView.IsMine)
				{
					myVRRig = vrrig;
				}
			}
		}
		if (!GorillaLocomotion.Player.Instance.IsHandSliding(forLeftHand: true) && GorillaLocomotion.Player.Instance.IsHandTouching(forLeftHand: true) && !leftHandTouching && Time.time > lastLeftTap + tapCoolDown && !GorillaLocomotion.Player.Instance.inOverlay)
		{
			StartVibration(forLeftController: true, tapHapticStrength, tapHapticDuration);
			tempInt = ((GorillaLocomotion.Player.Instance.leftHandSurfaceOverride != null) ? GorillaLocomotion.Player.Instance.leftHandSurfaceOverride.overrideIndex : GorillaLocomotion.Player.Instance.leftHandMaterialTouchIndex);
			if (PhotonNetwork.InRoom && myVRRig != null)
			{
				PhotonView.Get(myVRRig).RPC("PlayHandTap", RpcTarget.Others, tempInt, true, handTapVolume);
			}
			offlineVRRig.PlayHandTapLocal(tempInt, isLeftHand: true, handTapVolume);
			lastLeftTap = Time.time;
		}
		else if (GorillaLocomotion.Player.Instance.IsHandSliding(forLeftHand: true) && !GorillaLocomotion.Player.Instance.inOverlay)
		{
			StartVibration(forLeftController: true, tapHapticStrength / 5f, Time.fixedDeltaTime);
			if (!leftHandSlideSource.isPlaying)
			{
				leftHandSlideSource.Play();
			}
		}
		if (!GorillaLocomotion.Player.Instance.IsHandSliding(forLeftHand: true))
		{
			leftHandSlideSource.Stop();
		}
		if (!GorillaLocomotion.Player.Instance.IsHandSliding(forLeftHand: false) && GorillaLocomotion.Player.Instance.IsHandTouching(forLeftHand: false) && !rightHandTouching && Time.time > lastRightTap + tapCoolDown && !GorillaLocomotion.Player.Instance.inOverlay)
		{
			StartVibration(forLeftController: false, tapHapticStrength, tapHapticDuration);
			tempInt = ((GorillaLocomotion.Player.Instance.rightHandSurfaceOverride != null) ? GorillaLocomotion.Player.Instance.rightHandSurfaceOverride.overrideIndex : GorillaLocomotion.Player.Instance.rightHandMaterialTouchIndex);
			if (PhotonNetwork.InRoom && myVRRig != null)
			{
				PhotonView.Get(myVRRig).RPC("PlayHandTap", RpcTarget.Others, tempInt, false, handTapVolume);
			}
			offlineVRRig.PlayHandTapLocal(tempInt, isLeftHand: false, handTapVolume);
			lastRightTap = Time.time;
		}
		else if (GorillaLocomotion.Player.Instance.IsHandSliding(forLeftHand: false) && !GorillaLocomotion.Player.Instance.inOverlay)
		{
			StartVibration(forLeftController: false, tapHapticStrength / 5f, Time.fixedDeltaTime);
			if (!rightHandSlideSource.isPlaying)
			{
				rightHandSlideSource.Play();
			}
		}
		if (!GorillaLocomotion.Player.Instance.IsHandSliding(forLeftHand: false))
		{
			rightHandSlideSource.Stop();
		}
		CheckEndStatusEffect();
		leftHandTouching = GorillaLocomotion.Player.Instance.IsHandTouching(forLeftHand: true);
		rightHandTouching = GorillaLocomotion.Player.Instance.IsHandTouching(forLeftHand: false);
		lastLeftHandPositionForTag = leftHandTransform.position;
		lastRightHandPositionForTag = rightHandTransform.position;
		lastBodyPositionForTag = bodyCollider.transform.position;
		lastHeadPositionForTag = headCollider.transform.position;
		if (GorillaComputer.instance.voiceChatOn == "TRUE")
		{
			myRecorder = PhotonNetworkController.Instance.GetComponent<Recorder>();
			if (GorillaComputer.instance.pttType != "ALL CHAT")
			{
				primaryButtonPressRight = false;
				secondaryButtonPressRight = false;
				primaryButtonPressLeft = false;
				secondaryButtonPressLeft = false;
				primaryButtonPressRight = ControllerInputPoller.PrimaryButtonPress(XRNode.RightHand);
				secondaryButtonPressRight = ControllerInputPoller.SecondaryButtonPress(XRNode.RightHand);
				primaryButtonPressLeft = ControllerInputPoller.PrimaryButtonPress(XRNode.LeftHand);
				secondaryButtonPressLeft = ControllerInputPoller.PrimaryButtonPress(XRNode.LeftHand);
				if (primaryButtonPressRight || secondaryButtonPressRight || primaryButtonPressLeft || secondaryButtonPressLeft)
				{
					if (GorillaComputer.instance.pttType == "PUSH TO MUTE")
					{
						myRecorder.TransmitEnabled = false;
					}
					else if (GorillaComputer.instance.pttType == "PUSH TO TALK")
					{
						myRecorder.TransmitEnabled = true;
					}
				}
				else if (GorillaComputer.instance.pttType == "PUSH TO MUTE")
				{
					myRecorder.TransmitEnabled = true;
				}
				else if (GorillaComputer.instance.pttType == "PUSH TO TALK")
				{
					myRecorder.TransmitEnabled = false;
				}
			}
			else if (!myRecorder.TransmitEnabled)
			{
				myRecorder.TransmitEnabled = true;
			}
		}
		else if (PhotonNetworkController.Instance.GetComponent<Recorder>().TransmitEnabled)
		{
			PhotonNetworkController.Instance.GetComponent<Recorder>().TransmitEnabled = false;
		}
	}

	private bool TryToTag(RaycastHit hitInfo, bool isBodyTag, out Photon.Realtime.Player taggedPlayer)
	{
		if (PhotonNetwork.InRoom)
		{
			tempView = hitInfo.collider.GetComponentInParent<PhotonView>();
			if (tempView != null && PhotonNetwork.LocalPlayer != tempView.Owner && GorillaGameManager.instance != null && GorillaGameManager.instance.LocalCanTag(PhotonNetwork.LocalPlayer, tempView.Owner) && Time.time > taggedTime + tagCooldown)
			{
				if (!isBodyTag)
				{
					StartVibration(((leftHandTransform.position - hitInfo.collider.transform.position).magnitude < (rightHandTransform.position - hitInfo.collider.transform.position).magnitude) ? true : false, tagHapticStrength, tagHapticDuration);
				}
				else
				{
					StartVibration(forLeftController: true, tagHapticStrength, tagHapticDuration);
					StartVibration(forLeftController: false, tagHapticStrength, tagHapticDuration);
				}
				taggedPlayer = tempView.Owner;
				return true;
			}
		}
		taggedPlayer = null;
		return false;
	}

	public void StartVibration(bool forLeftController, float amplitude, float duration)
	{
		StartCoroutine(HapticPulses(forLeftController, amplitude, duration));
	}

	private IEnumerator HapticPulses(bool forLeftController, float amplitude, float duration)
	{
		float startTime = Time.time;
		uint channel = 0u;
		InputDevice device = ((!forLeftController) ? InputDevices.GetDeviceAtXRNode(XRNode.RightHand) : InputDevices.GetDeviceAtXRNode(XRNode.LeftHand));
		while (Time.time < startTime + duration)
		{
			device.SendHapticImpulse(channel, amplitude, hapticWaitSeconds);
			yield return new WaitForSeconds(hapticWaitSeconds * 0.9f);
		}
	}

	public void UpdateColor(float red, float green, float blue)
	{
		if (GorillaComputer.instance != null)
		{
			offlineVRRig.InitializeNoobMaterialLocal(red, green, blue, GorillaComputer.instance.leftHanded);
		}
		else
		{
			offlineVRRig.InitializeNoobMaterialLocal(red, green, blue, leftHanded: false);
		}
		offlineVRRig.mainSkin.material = offlineVRRig.materialsToChangeTo[0];
	}

	protected void OnTriggerEnter(Collider other)
	{
		if (PhotonNetwork.InRoom && other.gameObject.layer == 15 && other.gameObject != null && other.gameObject.GetComponent<GorillaTriggerBox>() != null)
		{
			other.gameObject.GetComponent<GorillaTriggerBox>().OnBoxTriggered();
		}
		if ((bool)other.GetComponentInChildren<GorillaTriggerBox>())
		{
			other.GetComponentInChildren<GorillaTriggerBox>().OnBoxTriggered();
		}
		else if ((bool)other.GetComponentInParent<GorillaTrigger>())
		{
			other.GetComponentInParent<GorillaTrigger>().OnTriggered();
		}
	}

	public void ShowCosmeticParticles(bool showParticles)
	{
		if (showParticles)
		{
			mainCamera.GetComponent<Camera>().cullingMask |= 1 << LayerMask.NameToLayer("GorillaCosmeticParticle");
			mirrorCamera.cullingMask |= 1 << LayerMask.NameToLayer("GorillaCosmeticParticle");
		}
		else
		{
			mainCamera.GetComponent<Camera>().cullingMask &= ~(1 << LayerMask.NameToLayer("GorillaCosmeticParticle"));
			mirrorCamera.cullingMask &= ~(1 << LayerMask.NameToLayer("GorillaCosmeticParticle"));
		}
	}

	public void ApplyStatusEffect(StatusEffect newStatus, float duration)
	{
		EndStatusEffect(currentStatus);
		currentStatus = newStatus;
		statusEndTime = Time.time + duration;
		switch (newStatus)
		{
		case StatusEffect.Frozen:
			GorillaLocomotion.Player.Instance.disableMovement = true;
			break;
		case StatusEffect.None:
		case StatusEffect.Slowed:
			break;
		}
	}

	private void CheckEndStatusEffect()
	{
		if (Time.time > statusEndTime)
		{
			EndStatusEffect(currentStatus);
		}
	}

	private void EndStatusEffect(StatusEffect effectToEnd)
	{
		switch (effectToEnd)
		{
		case StatusEffect.Frozen:
			GorillaLocomotion.Player.Instance.disableMovement = false;
			currentStatus = StatusEffect.None;
			break;
		case StatusEffect.Slowed:
			currentStatus = StatusEffect.None;
			break;
		case StatusEffect.None:
			break;
		}
	}

	private float CalcSlideControl(float fps)
	{
		return Mathf.Pow(Mathf.Pow(1f - baseSlideControl, 120f), 1f / fps);
	}
}
