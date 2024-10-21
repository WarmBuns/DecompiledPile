namespace EntityStates.VoidRaidCrab;

public class SpinBeamEnter : BaseSpinBeamAttackState
{
	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= base.duration && base.isAuthority)
		{
			outer.SetNextState(new SpinBeamWindUp());
		}
	}
}
