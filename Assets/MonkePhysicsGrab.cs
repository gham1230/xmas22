using UnityEngine;
using easyInputs;

namespace MyNamespace
{
	public class MonkePhysicsGrab : MonoBehaviour
	{
		[Header("script made by The Kachow DO NOT STEAL")]
		[Header("Give credits to The Kachow")]
		public Collider LeftHand;

		public Collider RightHand;

		public GameObject Grabbable;

		public bool UseNozzleAndLineRenderer = true;

		public Transform Nozzle;

		public float ThrowForce = 10f;

		public LineRenderer LineRenderer;

		private bool _isGrabbing = false;

		private Transform _originalParent;

		private Rigidbody _grabbableRigidbody;

		private void Start()
		{
			_originalParent = Grabbable.transform.parent;
			_grabbableRigidbody = Grabbable.GetComponent<Rigidbody>();
		}

		private void Update()
		{
			if (!_isGrabbing && (EasyInputs.GetGripButtonDown(EasyHand.RightHand) || EasyInputs.GetGripButtonDown(EasyHand.LeftHand)))
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
					Grabbable.transform.localPosition = Vector3.zero;
					Grabbable.GetComponent<Collider>().enabled = false;
					_grabbableRigidbody.isKinematic = true;
				}
			}
			else if (_isGrabbing && (EasyInputs.GetTriggerButtonDown(EasyHand.RightHand) || EasyInputs.GetTriggerButtonDown(EasyHand.LeftHand)))
			{
				_isGrabbing = false;
				Grabbable.transform.SetParent(_originalParent);
				Grabbable.GetComponent<Collider>().enabled = true;
				_grabbableRigidbody.isKinematic = false;
				if (UseNozzleAndLineRenderer)
				{
					Vector3 forward = Nozzle.transform.forward;
					_grabbableRigidbody.AddForce(forward * ThrowForce, ForceMode.Impulse);
					if (LineRenderer != null)
					{
						LineRenderer.enabled = false;
					}
				}
			}
			else if (_isGrabbing)
			{
				Grabbable.transform.localPosition = Vector3.zero;
				Grabbable.transform.localRotation = Quaternion.identity;
			}
		}
	}
}
