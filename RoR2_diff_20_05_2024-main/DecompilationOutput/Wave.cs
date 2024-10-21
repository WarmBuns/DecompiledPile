using System;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public struct Wave
{
	public float amplitude;

	public float frequency;

	[FormerlySerializedAs("phase")]
	public float cycleOffset;

	public float period
	{
		get
		{
			return 1f / frequency;
		}
		set
		{
			frequency = 1f / value;
		}
	}

	public float Evaluate(float t)
	{
		return Mathf.Sin(MathF.PI * 2f * (frequency * t + cycleOffset)) * amplitude;
	}
}
