using System;
using UnityEngine;

public class Oscillator : MonoBehaviour
{
	public enum WaveTypeEnum
	{
		Sine = 0,
		Square = 1,
		Triangle = 2
	}

	public WaveTypeEnum WaveType;

	private Vector3 m_initCenter;

	public bool UseCenter;

	public Vector3 Center;

	public Vector3 Radius;

	public Vector3 Frequency;

	public Vector3 Phase;

	public void Init(Vector3 center, Vector3 radius, Vector3 frequency, Vector3 startPhase)
	{
		Center = center;
		Radius = radius;
		Frequency = frequency;
		Phase = startPhase;
	}

	private float SampleWave(float phase)
	{
		switch (WaveType)
		{
		case WaveTypeEnum.Sine:
			return Mathf.Sin(phase);
		case WaveTypeEnum.Square:
			phase = Mathf.Repeat(phase, (float)Math.PI * 2f);
			if (!(phase < (float)Math.PI))
			{
				return -1f;
			}
			return 1f;
		case WaveTypeEnum.Triangle:
			phase = Mathf.Repeat(phase, (float)Math.PI * 2f);
			if (phase < (float)Math.PI / 2f)
			{
				return phase / ((float)Math.PI / 2f);
			}
			if (phase < (float)Math.PI)
			{
				return 1f - (phase - (float)Math.PI / 2f) / ((float)Math.PI / 2f);
			}
			if (phase < 4.712389f)
			{
				return ((float)Math.PI - phase) / ((float)Math.PI / 2f);
			}
			return (phase - 4.712389f) / ((float)Math.PI / 2f) - 1f;
		default:
			return 0f;
		}
	}

	public void OnEnable()
	{
		m_initCenter = base.transform.position;
	}

	public void Update()
	{
		Phase += Frequency * 2f * (float)Math.PI * Time.deltaTime;
		Vector3 position = (UseCenter ? Center : m_initCenter);
		position.x += Radius.x * SampleWave(Phase.x);
		position.y += Radius.y * SampleWave(Phase.y);
		position.z += Radius.z * SampleWave(Phase.z);
		base.transform.position = position;
	}
}
