using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Toolbot;

public class DroneProjectileHover : BaseState
{
	[SerializeField]
	public float duration;

	[SerializeField]
	public int pulseCount = 3;

	[SerializeField]
	public float pulseRadius = 7f;

	protected TeamFilter teamFilter;

	protected float interval;

	protected int pulses;

	public override void OnEnter()
	{
		base.OnEnter();
		if ((bool)base.rigidbody)
		{
			base.rigidbody.velocity = Vector3.zero;
			base.rigidbody.useGravity = false;
		}
		if (NetworkServer.active && (bool)base.projectileController)
		{
			teamFilter = base.projectileController.teamFilter;
		}
		interval = duration / (float)(pulseCount + 1);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (NetworkServer.active)
		{
			if (base.age >= duration)
			{
				EntityState.Destroy(base.gameObject);
			}
			else if (base.age >= interval * (float)(pulses + 1))
			{
				pulses++;
				Pulse();
			}
		}
	}

	protected virtual void Pulse()
	{
	}
}
