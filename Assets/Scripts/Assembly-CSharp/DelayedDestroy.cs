using System.Collections;
using UnityEngine;

public class DelayedDestroy : MonoBehaviour
{
	[Tooltip("Return to the object pool after this many seconds.")]
	public float destroyDelay;

	private float timeToDie = -1f;

	private IEnumerator DelayedDestroyCoroutine()
	{
		yield return new WaitForSeconds(destroyDelay);
	}

	protected void OnEnable()
	{
		if (!(ObjectPools.instance == null) && ObjectPools.instance.initialized)
		{
			timeToDie = Time.time + destroyDelay;
		}
	}

	protected void LateUpdate()
	{
		if (Time.time > timeToDie)
		{
			ObjectPools.instance.Destroy(base.gameObject);
		}
	}
}
