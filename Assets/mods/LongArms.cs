using UnityEngine;

public class LongArms : MonoBehaviour
{
	[SerializeField]
	private GameObject gorillaRig;

	private void OnTriggerEnter(Collider other)
	{
		gorillaRig.transform.localScale *= 1.35f;
	}
}
