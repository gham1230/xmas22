using UnityEngine;

[ExecuteInEditMode]
public class GorillaIK : MonoBehaviour
{
	public Transform headBone;

	public Transform leftUpperArm;

	public Transform leftLowerArm;

	public Transform leftHand;

	public Transform rightUpperArm;

	public Transform rightLowerArm;

	public Transform rightHand;

	public Transform targetLeft;

	public Transform targetRight;

	public Transform targetHead;

	public Quaternion initialUpperLeft;

	public Quaternion initialLowerLeft;

	public Quaternion initialUpperRight;

	public Quaternion initialLowerRight;

	public Quaternion newRotationUpper;

	public Quaternion newRotationLower;

	private float dU;

	private float dL;

	private float dMax;

	public bool testInEditor;

	public bool reset;

	public bool testDefineRot;

	public bool moveOnce;

	public float eps;

	public float upperArmAngle;

	public float elbowAngle;

	private void Awake()
	{
		if (Application.isPlaying && !testInEditor)
		{
			dU = (leftUpperArm.position - leftLowerArm.position).magnitude;
			dL = (leftLowerArm.position - leftHand.position).magnitude;
			dMax = dU + dL - eps;
			initialUpperLeft = leftUpperArm.localRotation;
			initialLowerLeft = leftLowerArm.localRotation;
			initialUpperRight = rightUpperArm.localRotation;
			initialLowerRight = rightLowerArm.localRotation;
		}
	}

	private void LateUpdate()
	{
		if ((Application.isPlaying && !testInEditor) || (Application.isEditor && testInEditor))
		{
			dU = (leftUpperArm.position - leftLowerArm.position).magnitude;
			dL = (leftLowerArm.position - leftHand.position).magnitude;
			dMax = dU + dL - eps;
			ArmIK(ref leftUpperArm, ref leftLowerArm, ref leftHand, initialUpperLeft, initialLowerLeft, targetLeft);
			ArmIK(ref rightUpperArm, ref rightLowerArm, ref rightHand, initialUpperRight, initialLowerRight, targetRight);
			headBone.rotation = targetHead.rotation;
		}
	}

	private void ArmIK(ref Transform upperArm, ref Transform lowerArm, ref Transform hand, Quaternion initRotUpper, Quaternion initRotLower, Transform target)
	{
		upperArm.localRotation = initRotUpper;
		lowerArm.localRotation = initRotLower;
		float num = Mathf.Clamp((target.position - upperArm.position).magnitude, eps, dMax);
		float num2 = Mathf.Acos(Mathf.Clamp(Vector3.Dot((hand.position - upperArm.position).normalized, (lowerArm.position - upperArm.position).normalized), -1f, 1f));
		float num3 = Mathf.Acos(Mathf.Clamp(Vector3.Dot((upperArm.position - lowerArm.position).normalized, (hand.position - lowerArm.position).normalized), -1f, 1f));
		float num4 = Mathf.Acos(Mathf.Clamp(Vector3.Dot((hand.position - upperArm.position).normalized, (target.position - upperArm.position).normalized), -1f, 1f));
		float num5 = Mathf.Acos(Mathf.Clamp((dL * dL - dU * dU - num * num) / (-2f * dU * num), -1f, 1f));
		float num6 = Mathf.Acos(Mathf.Clamp((num * num - dU * dU - dL * dL) / (-2f * dU * dL), -1f, 1f));
		Vector3 normalized = Vector3.Cross(hand.position - upperArm.position, lowerArm.position - upperArm.position).normalized;
		Vector3 normalized2 = Vector3.Cross(hand.position - upperArm.position, target.position - upperArm.position).normalized;
		Quaternion quaternion = Quaternion.AngleAxis((num5 - num2) * 57.29578f, Quaternion.Inverse(upperArm.rotation) * normalized);
		Quaternion quaternion2 = Quaternion.AngleAxis((num6 - num3) * 57.29578f, Quaternion.Inverse(lowerArm.rotation) * normalized);
		Quaternion quaternion3 = Quaternion.AngleAxis(num4 * 57.29578f, Quaternion.Inverse(upperArm.rotation) * normalized2);
		newRotationUpper = upperArm.localRotation * quaternion3 * quaternion;
		newRotationLower = lowerArm.localRotation * quaternion2;
		upperArm.localRotation = newRotationUpper;
		lowerArm.localRotation = newRotationLower;
		hand.rotation = target.rotation;
	}
}
