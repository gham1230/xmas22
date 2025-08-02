using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class ThermalManager : MonoBehaviour
{
	public static readonly List<ThermalSourceVolume> sources = new List<ThermalSourceVolume>(256);

	public static readonly List<ThermalReceiver> receivers = new List<ThermalReceiver>(256);

	[NonSerialized]
	public static ThermalManager instance;

	private const float kMinCelsius = -100f;

	protected void OnEnable()
	{
		if (instance != null)
		{
			Debug.LogError("ThermalManager already exists!");
		}
		else
		{
			instance = this;
		}
	}

	protected void LateUpdate()
	{
		float deltaTime = Time.deltaTime;
		for (int i = 0; i < receivers.Count; i++)
		{
			ThermalReceiver thermalReceiver = receivers[i];
			float num = 20f;
			for (int j = 0; j < sources.Count; j++)
			{
				ThermalSourceVolume thermalSourceVolume = sources[j];
				float num2 = Vector3.Distance(thermalSourceVolume.transform.position, thermalReceiver.transform.position);
				float num3 = 1f - Mathf.InverseLerp(thermalSourceVolume.innerRadius, thermalSourceVolume.outerRadius, num2 - thermalReceiver.radius);
				num += thermalSourceVolume.celsius * num3;
			}
			thermalReceiver.celsius = Mathf.Lerp(thermalReceiver.celsius, num, deltaTime * thermalReceiver.conductivity);
		}
	}

	public static void Register(ThermalSourceVolume source)
	{
		sources.Add(source);
	}

	public static void Unregister(ThermalSourceVolume source)
	{
		sources.Remove(source);
	}

	public static void Register(ThermalReceiver receiver)
	{
		receivers.Add(receiver);
	}

	public static void Unregister(ThermalReceiver receiver)
	{
		receivers.Remove(receiver);
	}
}
