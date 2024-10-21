using UnityEngine;

namespace EntityStates.Engi.SpiderMine;

public class WaitForStick : BaseSpiderMineState
{
	private static int IdleStateHash = Animator.StringToHash("Idle");

	protected override bool shouldStick => true;

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation("Base", IdleStateHash);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && base.projectileStickOnImpact.stuck)
		{
			outer.SetNextState(new Burrow());
		}
	}
}
