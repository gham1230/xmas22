using UnityEngine;

public class BubblerParticle : MonoBehaviour
{
	private AudioSource myAudioSource;

	private void Awake()
	{
		myAudioSource = base.gameObject.AddComponent(typeof(AudioSource)) as AudioSource;
	}

	private void Update()
	{
	}
}
