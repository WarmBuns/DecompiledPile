using UnityEngine;

public struct NetworkLerpedVector3
{
	private struct InterpPoint
	{
		public float time;

		public Vector3 value;
	}

	public float interpDelay;

	private InterpPoint lowInterpPoint;

	private InterpPoint highInterpPoint;

	private InterpPoint newestInterpPoint;

	private float inverseLowHighTimespan;

	public void SetValueImmediate(Vector3 value)
	{
		newestInterpPoint.time = Time.time;
		newestInterpPoint.value = value;
		highInterpPoint = newestInterpPoint;
		lowInterpPoint = newestInterpPoint;
		inverseLowHighTimespan = 0f;
	}

	public Vector3 GetCurrentValue(bool hasAuthority)
	{
		if (hasAuthority)
		{
			return newestInterpPoint.value;
		}
		float num = Time.time - interpDelay;
		if (num > highInterpPoint.time)
		{
			lowInterpPoint = highInterpPoint;
			highInterpPoint = newestInterpPoint;
			float num2 = highInterpPoint.time - lowInterpPoint.time;
			inverseLowHighTimespan = ((num2 == 0f) ? 0f : (1f / num2));
		}
		float t = (num - lowInterpPoint.time) * inverseLowHighTimespan;
		return Vector3.Lerp(lowInterpPoint.value, highInterpPoint.value, t);
	}

	public Vector3 GetAuthoritativeValue()
	{
		return newestInterpPoint.value;
	}

	public void PushValue(Vector3 value)
	{
		if (newestInterpPoint.value != value)
		{
			newestInterpPoint.time = Time.time;
			newestInterpPoint.value = value;
		}
	}
}
