using RoR2.Projectile;
using UnityEngine.Networking;

namespace EntityStates.Engi.Mine;

public class WaitForTarget : BaseMineState
{
	private ProjectileSphereTargetFinder targetFinder;

	private ProjectileTargetComponent projectileTargetComponent;

	private ProjectileImpactExplosion projectileImpactExplosion;

	protected override bool shouldStick => true;

	public override void OnEnter()
	{
		base.OnEnter();
		projectileTargetComponent = GetComponent<ProjectileTargetComponent>();
		targetFinder = GetComponent<ProjectileSphereTargetFinder>();
		if (NetworkServer.active)
		{
			targetFinder.enabled = true;
			base.armingStateMachine.SetNextState(new MineArmingWeak());
		}
	}

	public override void OnExit()
	{
		if ((bool)targetFinder)
		{
			targetFinder.enabled = false;
		}
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (NetworkServer.active && (bool)targetFinder)
		{
			if ((bool)projectileTargetComponent.target)
			{
				outer.SetNextState(new PreDetonate());
			}
			if (base.armingStateMachine?.state is BaseMineArmingState baseMineArmingState)
			{
				targetFinder.enabled = baseMineArmingState.triggerRadius != 0f;
				targetFinder.lookRange = baseMineArmingState.triggerRadius;
			}
		}
	}
}
