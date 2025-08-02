using UnityEngine;
using easyInputs;

public class MonkeGrab : MonoBehaviour
{
	public Collider LeftHand;

	public Collider RightHand;

	public GameObject Grabbable;

	private bool _isGrabbing = false;

	private Transform _originalParent;

	private void Start()
	{
		_originalParent = Grabbable.transform.parent;
	}

	private void Update()
	{
		if (!_isGrabbing && (EasyInputs.GetTriggerButtonDown(EasyHand.RightHand) || EasyInputs.GetTriggerButtonDown(EasyHand.LeftHand)))
		{
			Collider collider = null;
			if (LeftHand.bounds.Intersects(Grabbable.GetComponent<Collider>().bounds))
			{
				collider = LeftHand;
			}
			else if (RightHand.bounds.Intersects(Grabbable.GetComponent<Collider>().bounds))
			{
				collider = RightHand;
			}
			if (collider != null)
			{
				_isGrabbing = true;
				Grabbable.transform.SetParent(collider.transform);
				Grabbable.GetComponent<Collider>().enabled = false;
			}
		}
		else if (_isGrabbing && (EasyInputs.GetTriggerButtonDown(EasyHand.RightHand) || EasyInputs.GetTriggerButtonDown(EasyHand.LeftHand)))
		{
			_isGrabbing = false;
			Grabbable.transform.SetParent(_originalParent);
			Grabbable.GetComponent<Collider>().enabled = true;
		}
	}
}
