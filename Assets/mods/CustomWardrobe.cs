using UnityEngine;

public class CustomWardrobe : MonoBehaviour
{
	public bool IsCustom;

	public GameObject CustomButton;

	public GameObject Weardrobe;

	public GameObject CustomWeardrobe;

	public Material PressedMaterial;

	public Material UnPressMaterial;

	private Renderer buttonRenderer;

	private void Start()
	{
		buttonRenderer = CustomButton.GetComponent<Renderer>();
	}

	private void OnTriggerEnter(Collider other)
	{
		IsCustom = !IsCustom;
		if (IsCustom)
		{
			buttonRenderer.material = PressedMaterial;
			CustomWeardrobe.SetActive(value: true);
			Weardrobe.SetActive(value: false);
		}
		else
		{
			buttonRenderer.material = UnPressMaterial;
			CustomWeardrobe.SetActive(value: false);
			Weardrobe.SetActive(value: true);
		}
	}
}
