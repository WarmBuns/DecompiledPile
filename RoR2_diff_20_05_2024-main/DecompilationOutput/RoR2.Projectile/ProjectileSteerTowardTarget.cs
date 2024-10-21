using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Projectile;

[RequireComponent(typeof(ProjectileTargetComponent))]
public class ProjectileSteerTowardTarget : MonoBehaviour
{
	[Tooltip("Constrains rotation to the Y axis only.")]
	public bool yAxisOnly;

	[Tooltip("How fast to rotate in degrees per second. Rotation is linear.")]
	public float rotationSpeed;

	private new Transform transform;

	private ProjectileTargetComponent targetComponent;

	private void Start()
	{
		if (!NetworkServer.active)
		{
			base.enabled = false;
			return;
		}
		transform = base.transform;
		targetComponent = GetComponent<ProjectileTargetComponent>();
	}

	private void FixedUpdate()
	{
		if ((bool)targetComponent.target)
		{
			Vector3 vector = targetComponent.target.transform.position - transform.position;
			if (yAxisOnly)
			{
				vector.y = 0f;
			}
			if (vector != Vector3.zero)
			{
				transform.forward = Vector3.RotateTowards(transform.forward, vector, rotationSpeed * (MathF.PI / 180f) * Time.fixedDeltaTime, 0f);
			}
		}
	}
}
