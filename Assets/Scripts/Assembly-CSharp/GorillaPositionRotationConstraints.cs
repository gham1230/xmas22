using UnityEngine;

public class GorillaPositionRotationConstraints : MonoBehaviour
{
	public GorillaPosRotConstraint[] constraints;

	protected void OnEnable()
	{
		GorillaPositionRotationConstraintManager.Register(this);
	}

	protected void OnDisable()
	{
		GorillaPositionRotationConstraintManager.Unregister(this);
	}
}
