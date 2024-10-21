using System;
using UnityEngine;

namespace RoR2;

[Serializable]
public struct PitchYawPair
{
	public static readonly PitchYawPair zero = new PitchYawPair(0f, 0f);

	public float pitch;

	public float yaw;

	public PitchYawPair(float pitch, float yaw)
	{
		this.pitch = pitch;
		this.yaw = yaw;
	}

	public static PitchYawPair Lerp(PitchYawPair a, PitchYawPair b, float t)
	{
		float num = Mathf.LerpAngle(a.pitch, b.pitch, t);
		float num2 = Mathf.LerpAngle(a.yaw, b.yaw, t);
		return new PitchYawPair(num, num2);
	}

	public static PitchYawPair SmoothDamp(PitchYawPair current, PitchYawPair target, ref PitchYawPair velocity, float smoothTime, float maxSpeed)
	{
		float num = Mathf.SmoothDampAngle(current.pitch, target.pitch, ref velocity.pitch, smoothTime, maxSpeed);
		float num2 = Mathf.SmoothDampAngle(current.yaw, target.yaw, ref velocity.yaw, smoothTime, maxSpeed);
		return new PitchYawPair(num, num2);
	}
}
