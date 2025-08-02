using UnityEngine;

public class ButtonPress : MonoBehaviour
{
	public bool Button;

	public GameObject button;

	public GameObject Mod3;

	public GameObject Mod2;

	public GameObject Mod;

	public Material PressedMaterial;

	public Material UnPressMaterial;

	private Renderer buttonRenderer;

	private void Start()
	{
		buttonRenderer = button.GetComponent<Renderer>();
	}

	private void OnTriggerEnter(Collider other)
	{
		Button = !Button;
		if (Button)
		{
			buttonRenderer.material = PressedMaterial;
			Mod.SetActive(value: true);
			Mod2.SetActive(value: false);
			Mod3.SetActive(value: false);
		}
		else
		{
			buttonRenderer.material = UnPressMaterial;
			Mod.SetActive(value: false);
		}
	}
}
