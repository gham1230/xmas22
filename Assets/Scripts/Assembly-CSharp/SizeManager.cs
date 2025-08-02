using System.Collections.Generic;
using GorillaLocomotion;
using Photon.Pun;
using UnityEngine;

public class SizeManager : MonoBehaviour
{
	public SizeChanger lastSizeChanger;

	public List<SizeChanger> touchingChangers;

	private LineRenderer[] lineRenderers;

	private List<float> initLineScalar = new List<float>();

	public VRRig targetRig;

	public Player targetPlayer;

	public float magnitudeThreshold = 0.01f;

	private void CollectLineRenderers(GameObject obj)
	{
		lineRenderers = obj.GetComponentsInChildren<LineRenderer>(includeInactive: true);
		_ = lineRenderers.Length;
		LineRenderer[] array = lineRenderers;
		foreach (LineRenderer lineRenderer in array)
		{
			initLineScalar.Add(lineRenderer.widthMultiplier);
		}
	}

	private void Awake()
	{
		if (targetRig != null)
		{
			CollectLineRenderers(targetRig.gameObject);
		}
		else if (targetPlayer != null)
		{
			CollectLineRenderers(GorillaTagger.Instance.offlineVRRig.gameObject);
		}
	}

	private void FixedUpdate()
	{
		float num = 1f;
		if (targetRig != null && !targetRig.isOfflineVRRig && targetRig.photonView != null && targetRig.photonView.Owner != PhotonNetwork.LocalPlayer)
		{
			lastSizeChanger = ControllingChanger(targetRig.transform);
			num = ScaleFromChanger(lastSizeChanger, targetRig.transform);
			targetRig.scaleFactor = num;
		}
		if (targetPlayer != null)
		{
			lastSizeChanger = ControllingChanger(Camera.main.transform);
			num = ScaleFromChanger(lastSizeChanger, Camera.main.transform);
			targetPlayer.scale = num;
		}
		for (int i = 0; i < lineRenderers.Length; i++)
		{
			lineRenderers[i].widthMultiplier = num * initLineScalar[i];
		}
	}

	public SizeChanger ControllingChanger(Transform t)
	{
		for (int num = touchingChangers.Count - 1; num >= 0; num--)
		{
			if (touchingChangers[num] != null && touchingChangers[num].gameObject.activeInHierarchy && (touchingChangers[num].myCollider.ClosestPoint(t.position) - t.position).magnitude < magnitudeThreshold)
			{
				return touchingChangers[num];
			}
		}
		return null;
	}

	public float ScaleFromChanger(SizeChanger sC, Transform t)
	{
		if (sC == null)
		{
			return 1f;
		}
		switch (sC.myType)
		{
		case SizeChanger.ChangerType.Continuous:
		{
			Vector3 vector = Vector3.Project(t.position - sC.startPos.position, sC.endPos.position - sC.startPos.position);
			return Mathf.Clamp(sC.maxScale - vector.magnitude / (sC.startPos.position - sC.endPos.position).magnitude * (sC.maxScale - sC.minScale), sC.minScale, sC.maxScale);
		}
		case SizeChanger.ChangerType.Static:
			return sC.minScale;
		default:
			return 1f;
		}
	}
}
