namespace EntityStates.VoidRaidCrab.Leg;

public class Idle : BaseLegState
{
	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.legController.mainBodyHasEffectiveAuthority && !base.legController.DoesJointExist())
		{
			outer.SetNextState(new JointBroken());
		}
	}
}
