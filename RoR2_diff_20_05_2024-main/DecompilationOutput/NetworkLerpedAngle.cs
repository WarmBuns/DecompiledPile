using UnityEngine;

public struct NetworkLerpedAngle
{
	private const float interpDuration = 0.05f;

	private const float invInterpDuration = 20f;

	public float authoritativeValue;

	private float interpStartValue;

	private float interpStartTime;

	public float GetCurrentValue(bool hasAuthority)
	{
		if (hasAuthority)
		{
			return authoritativeValue;
		}
		float t = Mathf.Clamp01((Time.time - interpStartTime) * 20f);
		return Mathf.LerpAngle(interpStartValue, authoritativeValue, t);
	}

	public void SetAuthoritativeValue(float newValue, bool hasAuthority)
	{
		interpStartValue = GetCurrentValue(hasAuthority);
		interpStartTime = Time.time;
		authoritativeValue = newValue;
	}
}
