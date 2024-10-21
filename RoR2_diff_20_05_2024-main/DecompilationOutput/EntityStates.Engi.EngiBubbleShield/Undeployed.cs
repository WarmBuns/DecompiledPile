using RoR2;
using RoR2.Projectile;
using UnityEngine.Networking;

namespace EntityStates.Engi.EngiBubbleShield;

public class Undeployed : EntityState
{
	private ProjectileStickOnImpact projectileStickOnImpact;

	public override void OnEnter()
	{
		base.OnEnter();
		ProjectileController component = GetComponent<ProjectileController>();
		projectileStickOnImpact = GetComponent<ProjectileStickOnImpact>();
		if (!NetworkServer.active || !component.owner)
		{
			return;
		}
		CharacterBody component2 = component.owner.GetComponent<CharacterBody>();
		if ((bool)component2)
		{
			CharacterMaster master = component2.master;
			if ((bool)master)
			{
				master.AddDeployable(GetComponent<Deployable>(), DeployableSlot.EngiBubbleShield);
			}
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (projectileStickOnImpact.stuck && NetworkServer.active)
		{
			SetNextState();
		}
	}

	protected virtual void SetNextState()
	{
		outer.SetNextState(new Deployed());
	}
}
