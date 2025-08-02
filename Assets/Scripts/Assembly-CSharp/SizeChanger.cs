using System.Collections.Generic;
using UnityEngine;

public class SizeChanger : GorillaTriggerBox
{
	public enum ChangerType
	{
		Static = 0,
		Continuous = 1
	}

	public VRRig rigRef;

	public ChangerType myType;

	public float maxScale;

	public float minScale;

	public Collider myCollider;

	public float insideThreshold = 0.01f;

	public List<VRRig> insideRigs;

	public List<VRRig> leftRigs;

	public float scaleLerp;

	public Transform startPos;

	public Transform endPos;

	private void Awake()
	{
		minScale = Mathf.Max(minScale, 0.01f);
		myCollider = GetComponent<Collider>();
	}

	public void OnTriggerEnter(Collider other)
	{
		if ((bool)other.GetComponent<SphereCollider>())
		{
			VRRig component = other.attachedRigidbody.gameObject.GetComponent<VRRig>();
			if (!(component == null) && !component.sizeManager.touchingChangers.Contains(this))
			{
				component.sizeManager.touchingChangers.Add(this);
			}
		}
	}

	public void OnTriggerExit(Collider other)
	{
		if ((bool)other.GetComponent<SphereCollider>())
		{
			VRRig component = other.attachedRigidbody.gameObject.GetComponent<VRRig>();
			if (!(component == null) && component.sizeManager.touchingChangers.Contains(this))
			{
				component.sizeManager.touchingChangers.Remove(this);
			}
		}
	}
}
