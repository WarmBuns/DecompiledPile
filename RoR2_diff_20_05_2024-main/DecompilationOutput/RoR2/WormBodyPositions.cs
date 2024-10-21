using System.Collections.Generic;
using UnityEngine;

namespace RoR2;

public class WormBodyPositions : MonoBehaviour
{
	private struct Keyframe
	{
		public Vector3 position;

		public Quaternion rotation;

		public Vector3 fromPreviousNormal;

		public float fromPreviousLength;
	}

	public Vector3 headVelocity = Vector3.zero;

	public Transform[] segments;

	public float segmentRadius = 1f;

	private List<Keyframe> positionHistory = new List<Keyframe>();

	private void Start()
	{
		positionHistory.Add(new Keyframe
		{
			rotation = segments[0].rotation,
			position = segments[0].position,
			fromPreviousNormal = Vector3.zero,
			fromPreviousLength = 0f
		});
	}

	private void FixedUpdate()
	{
		Vector3 position = segments[0].position;
		Vector3 vector = position - positionHistory[positionHistory.Count - 1].position;
		float magnitude = vector.magnitude;
		if (magnitude != 0f)
		{
			Quaternion rotation = segments[0].rotation;
			segments[0].up = -vector;
			Quaternion rotation2 = segments[0].rotation;
			segments[0].rotation = Quaternion.RotateTowards(rotation, rotation2, 360f * Time.fixedDeltaTime);
			positionHistory.Add(new Keyframe
			{
				rotation = segments[0].rotation,
				position = position,
				fromPreviousNormal = vector * (1f / magnitude),
				fromPreviousLength = magnitude
			});
		}
		float num = segmentRadius * 2f;
		float num2 = num;
		Vector3 a = position;
		int num3 = 1;
		for (int num4 = positionHistory.Count - 1; num4 >= 1; num4--)
		{
			Vector3 position2 = positionHistory[num4 - 1].position;
			float fromPreviousLength = positionHistory[num4].fromPreviousLength;
			if (num2 < fromPreviousLength)
			{
				float t = num2 / fromPreviousLength;
				segments[num3].position = Vector3.Lerp(a, position2, t);
				num3++;
				if (num3 >= segments.Length)
				{
					positionHistory.RemoveRange(0, num4 - 1);
					break;
				}
				num2 += num;
			}
			num2 -= fromPreviousLength;
			a = position2;
		}
		if (segments.Length <= 1)
		{
			return;
		}
		Quaternion rotation3 = segments[0].rotation;
		Vector3 vector2 = segments[0].position;
		Vector3 vector3 = segments[1].position;
		for (int i = 1; i < segments.Length - 1; i++)
		{
			Vector3 position3 = segments[i + 1].position;
			Vector3 vector4 = position3 - vector2;
			if (vector4 != Vector3.zero)
			{
				segments[i].rotation = rotation3;
				segments[i].up = vector4;
			}
			vector2 = vector3;
			vector3 = position3;
		}
	}
}
