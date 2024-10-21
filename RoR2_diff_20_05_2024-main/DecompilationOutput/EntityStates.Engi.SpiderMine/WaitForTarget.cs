using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Engi.SpiderMine;

public class WaitForTarget : BaseSpiderMineState
{
	private static int ArmedStateHash = Animator.StringToHash("Armed");

	protected ProjectileSphereTargetFinder targetFinder { get; private set; }

	protected override bool shouldStick => true;

	public override void OnEnter()
	{
		base.OnEnter();
		if (NetworkServer.active)
		{
			targetFinder = GetComponent<ProjectileSphereTargetFinder>();
			targetFinder.enabled = true;
		}
		PlayAnimation("Base", ArmedStateHash);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority)
		{
			EntityState entityState = null;
			if (!base.projectileStickOnImpact.stuck)
			{
				entityState = new WaitForStick();
			}
			else if ((bool)base.projectileTargetComponent.target)
			{
				entityState = new Unburrow();
			}
			if (entityState != null)
			{
				outer.SetNextState(entityState);
			}
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
}
