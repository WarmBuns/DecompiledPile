namespace EntityStates.VoidRaidCrab.Leg;

public class JointBroken : BaseLegState
{
	public override void OnEnter()
	{
		base.OnEnter();
		base.legController.shouldRetract = true;
	}

	public override void OnExit()
	{
		base.OnExit();
		base.legController.shouldRetract = false;
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.legController.mainBodyHasEffectiveAuthority && base.legController.DoesJointExist())
		{
			outer.SetNextState(new Idle());
		}
	}
}
