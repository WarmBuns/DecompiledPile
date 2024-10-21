using System.Runtime.InteropServices;
using UnityEngine;

namespace RoR2;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct Trajectory
{
	private static float defaultGravity => Physics.gravity.y;

	public static float CalculateApex(float initialSpeed)
	{
		return CalculateApex(initialSpeed, defaultGravity);
	}

	public static float CalculateApex(float initialSpeed, float gravity)
	{
		return initialSpeed * initialSpeed / (2f * (0f - gravity));
	}

	public static float CalculateGroundSpeed(float time, float distance)
	{
		return distance / time;
	}

	public static float CalculateGroundTravelTime(float hSpeed, float hDistance)
	{
		return hDistance / hSpeed;
	}

	public static float CalculateInitialYSpeed(float timeToTarget, float destinationYOffset)
	{
		return CalculateInitialYSpeed(timeToTarget, destinationYOffset, defaultGravity);
	}

	public static float CalculateInitialYSpeed(float timeToTarget, float destinationYOffset, float gravity)
	{
		return (destinationYOffset + 0.5f * (0f - gravity) * timeToTarget * timeToTarget) / timeToTarget;
	}

	public static float CalculateInitialYSpeedForHeight(float height)
	{
		return CalculateInitialYSpeedForHeight(height, defaultGravity);
	}

	public static float CalculateInitialYSpeedForHeight(float height, float gravity)
	{
		return Mathf.Sqrt(height * (2f * (0f - gravity)));
	}

	public static Vector3 CalculatePositionAtTime(Vector3 origin, Vector3 initialVelocity, float t)
	{
		return CalculatePositionAtTime(origin, initialVelocity, t, defaultGravity);
	}

	public static Vector3 CalculatePositionAtTime(Vector3 origin, Vector3 initialVelocity, float t, float gravity)
	{
		Vector3 result = origin + initialVelocity * t;
		result.y += 0.5f * gravity * t * t;
		return result;
	}

	public static float CalculatePositionYAtTime(float originY, float initialVelocityY, float t)
	{
		return CalculatePositionYAtTime(originY, initialVelocityY, t, defaultGravity);
	}

	public static float CalculatePositionYAtTime(float originY, float initialVelocityY, float t, float gravity)
	{
		return originY + initialVelocityY * t + 0.5f * gravity * t * t;
	}

	public static float CalculateInitialYSpeedForFlightDuration(float duration)
	{
		return CalculateInitialYSpeedForFlightDuration(duration, defaultGravity);
	}

	public static float CalculateInitialYSpeedForFlightDuration(float duration, float gravity)
	{
		return duration * gravity * -0.5f;
	}

	public static float CalculateFlightDuration(float vSpeed)
	{
		return CalculateFlightDuration(vSpeed, defaultGravity);
	}

	public static float CalculateFlightDuration(float vSpeed, float gravity)
	{
		return 2f * vSpeed / (0f - gravity);
	}

	public static float CalculateFlightDuration(float originY, float endY, float vSpeed)
	{
		return CalculateFlightDuration(originY, endY, vSpeed, defaultGravity);
	}

	public static float CalculateFlightDuration(float originY, float endY, float vSpeed, float gravity)
	{
		float num = endY - originY;
		float num2 = Mathf.Sqrt(vSpeed * vSpeed - 4f * (0.5f * gravity) * (0f - num));
		return (0f - vSpeed - num2) / gravity;
	}

	public static float CalculateGroundSpeedToClearDistance(float vSpeed, float distance)
	{
		return CalculateGroundSpeedToClearDistance(vSpeed, distance, defaultGravity);
	}

	public static float CalculateGroundSpeedToClearDistance(float vSpeed, float distance, float gravity)
	{
		return distance / CalculateFlightDuration(vSpeed, gravity);
	}
}
