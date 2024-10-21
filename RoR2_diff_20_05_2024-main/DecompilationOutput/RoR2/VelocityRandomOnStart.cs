using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class VelocityRandomOnStart : MonoBehaviour
{
	public enum DirectionMode
	{
		Sphere,
		Hemisphere,
		Cone
	}

	public float minSpeed;

	public float maxSpeed;

	public Vector3 baseDirection = Vector3.up;

	public bool localDirection;

	public DirectionMode directionMode;

	public float coneAngle = 30f;

	[Tooltip("Minimum angular speed in degrees/second.")]
	public float minAngularSpeed;

	[Tooltip("Maximum angular speed in degrees/second.")]
	public float maxAngularSpeed;

	private void Start()
	{
		if (!NetworkServer.active)
		{
			return;
		}
		Rigidbody component = GetComponent<Rigidbody>();
		if (!component)
		{
			return;
		}
		float num = ((minSpeed != maxSpeed) ? UnityEngine.Random.Range(minSpeed, maxSpeed) : minSpeed);
		if (num != 0f)
		{
			Vector3 vector = Vector3.zero;
			Vector3 vector2 = (localDirection ? (base.transform.rotation * baseDirection) : baseDirection);
			switch (directionMode)
			{
			case DirectionMode.Sphere:
				vector = UnityEngine.Random.onUnitSphere;
				break;
			case DirectionMode.Hemisphere:
				vector = UnityEngine.Random.onUnitSphere;
				if (Vector3.Dot(vector, vector2) < 0f)
				{
					vector = -vector;
				}
				break;
			case DirectionMode.Cone:
				vector = Util.ApplySpread(vector2, 0f, coneAngle, 1f, 1f);
				break;
			}
			component.velocity = vector * num;
		}
		float num2 = ((minAngularSpeed != maxAngularSpeed) ? UnityEngine.Random.Range(minAngularSpeed, maxAngularSpeed) : minAngularSpeed);
		if (num2 != 0f)
		{
			component.angularVelocity = UnityEngine.Random.onUnitSphere * (num2 * (MathF.PI / 180f));
		}
	}
}
