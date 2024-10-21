using UnityEngine;

public struct NetworkLerpedAngleNew
{
	private struct InterpPoint
	{
		public float time;

		public float value;
	}

	private const float interpDelay = 0.1f;

	private InterpPoint lowInterpPoint;

	private InterpPoint highInterpPoint;

	private InterpPoint newestInterpPoint;

	private float inverseLowHighTimespan;

	public void SetValueImmediate(float value)
	{
		newestInterpPoint.time = Time.time;
		newestInterpPoint.value = value;
		highInterpPoint = newestInterpPoint;
		lowInterpPoint = newestInterpPoint;
		inverseLowHighTimespan = 0f;
	}

	public float GetCurrentValue(bool hasAuthority)
	{
		if (hasAuthority)
		{
			return newestInterpPoint.value;
		}
		float num = Time.time - 0.1f;
		if (num > highInterpPoint.time)
		{
			lowInterpPoint = highInterpPoint;
			highInterpPoint = newestInterpPoint;
			float num2 = highInterpPoint.time - lowInterpPoint.time;
			inverseLowHighTimespan = ((num2 == 0f) ? 0f : (1f / num2));
		}
		float t = Mathf.Clamp01((num - lowInterpPoint.time) * inverseLowHighTimespan);
		return Mathf.LerpAngle(lowInterpPoint.value, highInterpPoint.value, t);
	}

	public float GetAuthoritativeValue()
	{
		return newestInterpPoint.value;
	}

	public void PushValue(float value)
	{
		if (newestInterpPoint.value != value)
		{
			newestInterpPoint.time = Time.time;
			newestInterpPoint.value = value;
		}
	}
}
