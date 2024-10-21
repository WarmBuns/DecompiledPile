using UnityEngine.Networking;

namespace EntityStates.Engi.Mine;

public class WaitForStick : BaseMineState
{
	protected override bool shouldStick => true;

	protected override bool shouldRevertToWaitForStickOnSurfaceLost => false;

	public override void OnEnter()
	{
		base.OnEnter();
		if (NetworkServer.active)
		{
			base.armingStateMachine.SetNextState(new MineArmingUnarmed());
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (NetworkServer.active && base.projectileStickOnImpact.stuck)
		{
			outer.SetNextState(new Arm());
		}
	}
}
