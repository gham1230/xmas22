using UnityEngine;

public class CornOnCobCosmetic : MonoBehaviour
{
	[Tooltip("The corn will start popping based on the temperature from this ThermalReciever.")]
	public ThermalReceiver thermalReceiver;

	[Tooltip("The particle system that will be emitted when the heat source is hot enough.")]
	public ParticleSystem particleSys;

	[Tooltip("The curve that determines how many particles will be emitted based on the heat source's temperature.\n\nThe x-axis is the heat source's temperature and the y-axis is the number of particles to emit.")]
	public AnimationCurve particleEmissionCurve;

	public SoundBankPlayer soundBankPlayer;

	private ParticleSystem.EmissionModule emissionModule;

	private float maxBurstProbability;

	private int previousParticleCount;

	protected void Awake()
	{
		emissionModule = particleSys.emission;
		maxBurstProbability = ((emissionModule.burstCount > 0) ? emissionModule.GetBurst(0).probability : 0.2f);
	}

	protected void LateUpdate()
	{
		for (int i = 0; i < emissionModule.burstCount; i++)
		{
			ParticleSystem.Burst burst = emissionModule.GetBurst(i);
			burst.probability = maxBurstProbability * particleEmissionCurve.Evaluate(thermalReceiver.celsius);
			emissionModule.SetBurst(i, burst);
		}
		int particleCount = particleSys.particleCount;
		if (particleCount > previousParticleCount)
		{
			soundBankPlayer.Play();
		}
		previousParticleCount = particleCount;
	}
}
