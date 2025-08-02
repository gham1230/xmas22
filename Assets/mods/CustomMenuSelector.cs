using UnityEngine;
using UnityEngine.UI;

public class CustomMenuSelector : MonoBehaviour
{
	public bool IsEnabled;

	public GameObject CustomButton;

	public GameObject ModMenuEnable;

	public GameObject ModMenuDisable1;

	public GameObject ModMenuDisable2;

	public Text ButtonText;

	public GameObject ModMenuDisable3;

	public Material PressedMaterial;

	public Material UnPressMaterial;

	private Renderer buttonRenderer;

	private void Start()
	{
		buttonRenderer = CustomButton.GetComponent<Renderer>();
	}

	private void OnTriggerEnter(Collider other)
	{
		IsEnabled = !IsEnabled;
		if (IsEnabled)
		{
			buttonRenderer.material = PressedMaterial;
			ModMenuEnable.SetActive(value: true);
			ModMenuDisable1.SetActive(value: false);
			ModMenuDisable2.SetActive(value: false);
			ModMenuDisable3.SetActive(value: false);
			ButtonText.text = "TAKE OFF";
		}
		else
		{
			buttonRenderer.material = UnPressMaterial;
			ModMenuEnable.SetActive(value: false);
			ButtonText.text = "PUT ON";
		}
	}
}
