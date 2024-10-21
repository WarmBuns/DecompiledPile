using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Engi.Mine;

public class BaseMineState : BaseState
{
	[SerializeField]
	public string enterSoundString;

	protected ProjectileStickOnImpact projectileStickOnImpact { get; private set; }

	protected EntityStateMachine armingStateMachine { get; private set; }

	protected virtual bool shouldStick => false;

	protected virtual bool shouldRevertToWaitForStickOnSurfaceLost => false;

	public override void OnEnter()
	{
		base.OnEnter();
		projectileStickOnImpact = GetComponent<ProjectileStickOnImpact>();
		armingStateMachine = EntityStateMachine.FindByCustomName(base.gameObject, "Arming");
		if (projectileStickOnImpact.enabled != shouldStick)
		{
			projectileStickOnImpact.enabled = shouldStick;
		}
		Util.PlaySound(enterSoundString, base.gameObject);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (NetworkServer.active && shouldRevertToWaitForStickOnSurfaceLost && !projectileStickOnImpact.stuck)
		{
			outer.SetNextState(new WaitForStick());
		}
	}
}
