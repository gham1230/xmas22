using UnityEngine;

public class LongArmsDisable : MonoBehaviour
{
	[SerializeField]
	private GameObject gorillaRig;

	private void OnTriggerEnter(Collider other)
	{
		gorillaRig.transform.localScale = Vector3.one;
	}
}
