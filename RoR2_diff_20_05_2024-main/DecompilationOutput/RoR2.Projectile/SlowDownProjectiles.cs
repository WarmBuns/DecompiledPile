using System.Collections.Generic;
using UnityEngine;

namespace RoR2.Projectile;

[RequireComponent(typeof(Collider))]
public class SlowDownProjectiles : MonoBehaviour
{
	private struct SlowDownProjectileInfo
	{
		public Rigidbody rb;

		public Vector3 previousVelocity;
	}

	public TeamFilter teamFilter;

	public float slowDownCoefficient;

	private List<SlowDownProjectileInfo> slowDownProjectileInfos;

	private void Start()
	{
		slowDownProjectileInfos = new List<SlowDownProjectileInfo>();
	}

	private void Update()
	{
	}

	private void OnTriggerEnter(Collider other)
	{
		TeamFilter component = other.GetComponent<TeamFilter>();
		Rigidbody component2 = other.GetComponent<Rigidbody>();
		if ((bool)component2 && component.teamIndex != teamFilter.teamIndex)
		{
			slowDownProjectileInfos.Add(new SlowDownProjectileInfo
			{
				rb = component2,
				previousVelocity = component2.velocity
			});
		}
	}

	private void OnTriggerExit(Collider other)
	{
		TeamFilter component = other.GetComponent<TeamFilter>();
		Rigidbody component2 = other.GetComponent<Rigidbody>();
		if ((bool)component2 && component.teamIndex != teamFilter.teamIndex)
		{
			RemoveFromSlowDownProjectileInfos(component2);
		}
	}

	private void RemoveFromSlowDownProjectileInfos(Rigidbody rb)
	{
		for (int i = 0; i < slowDownProjectileInfos.Count; i++)
		{
			if (slowDownProjectileInfos[i].rb == rb)
			{
				slowDownProjectileInfos.RemoveAt(i);
				break;
			}
		}
	}

	private void FixedUpdate()
	{
		for (int i = 0; i < slowDownProjectileInfos.Count; i++)
		{
			SlowDownProjectileInfo value = slowDownProjectileInfos[i];
			Rigidbody rb = value.rb;
			Vector3 previousVelocity = value.previousVelocity;
			if ((bool)rb)
			{
				rb.MovePosition(rb.position - Vector3.Lerp(previousVelocity, Vector3.zero, slowDownCoefficient) * Time.fixedDeltaTime);
				value.previousVelocity = rb.velocity;
				slowDownProjectileInfos[i] = value;
			}
			else
			{
				RemoveFromSlowDownProjectileInfos(rb);
			}
		}
	}
}
