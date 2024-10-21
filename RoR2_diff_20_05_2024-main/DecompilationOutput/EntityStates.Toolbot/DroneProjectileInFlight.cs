using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Toolbot;

public class DroneProjectileInFlight : BaseState
{
	private ProjectileImpactEventCaller impactEventCaller;

	private ProjectileSimple projectileSimple;

	private ProjectileFuse projectileFuse;

	public override void OnEnter()
	{
		base.OnEnter();
		if (NetworkServer.active)
		{
			impactEventCaller = GetComponent<ProjectileImpactEventCaller>();
			if ((bool)impactEventCaller)
			{
				impactEventCaller.impactEvent.AddListener(OnImpact);
			}
			projectileSimple = GetComponent<ProjectileSimple>();
			projectileFuse = GetComponent<ProjectileFuse>();
			if ((bool)projectileFuse)
			{
				projectileFuse.onFuse.AddListener(OnFuse);
			}
		}
	}

	public override void OnExit()
	{
		if ((bool)impactEventCaller)
		{
			impactEventCaller.impactEvent.RemoveListener(OnImpact);
		}
		if ((bool)projectileFuse)
		{
			projectileFuse.onFuse.RemoveListener(OnFuse);
		}
		base.OnEnter();
	}

	private void OnImpact(ProjectileImpactInfo projectileImpactInfo)
	{
		Advance();
	}

	private void OnFuse()
	{
		Advance();
	}

	private void Advance()
	{
		if (NetworkServer.active)
		{
			if ((bool)projectileSimple)
			{
				projectileSimple.velocity = 0f;
				projectileSimple.enabled = false;
			}
			if ((bool)base.rigidbody)
			{
				base.rigidbody.velocity = new Vector3(0f, Trajectory.CalculateInitialYSpeedForFlightDuration(DroneProjectilePrepHover.duration), 0f);
			}
		}
		if (base.isAuthority)
		{
			outer.SetNextState(new DroneProjectilePrepHover());
		}
	}
}
