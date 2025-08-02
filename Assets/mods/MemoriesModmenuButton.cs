using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MemoriesModmenuButton : GorillaPressableButton
{
	public List<GameObject> Disable;

	public List<GameObject> Enable;

	public float buttonFadeTime = 0.25f;

	public override void ButtonActivation()
	{
		foreach (GameObject item in Disable)
		{
			item.SetActive(value: false);
		}
		foreach (GameObject item2 in Enable)
		{
			item2.SetActive(value: true);
		}
		base.ButtonActivation();
		StartCoroutine(ButtonColorUpdate());
	}

	private IEnumerator ButtonColorUpdate()
	{
		buttonRenderer.material = pressedMaterial;
		yield return new WaitForSeconds(buttonFadeTime);
		buttonRenderer.material = unpressedMaterial;
	}
}
