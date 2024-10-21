using RoR2.Projectile;

namespace EntityStates.Engi.MineDeployer;

public class WaitForStick : BaseMineDeployerState
{
	private ProjectileStickOnImpact projectileStickOnImpact;

	public override void OnEnter()
	{
		base.OnEnter();
		projectileStickOnImpact = GetComponent<ProjectileStickOnImpact>();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && projectileStickOnImpact.stuck)
		{
			outer.SetNextState(new FireMine());
		}
	}
}
