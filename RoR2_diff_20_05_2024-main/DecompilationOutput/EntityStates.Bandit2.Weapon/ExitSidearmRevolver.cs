namespace EntityStates.Bandit2.Weapon;

public class ExitSidearmRevolver : BaseSidearmState
{
	public override string exitAnimationStateName => "SideToMain";

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge > duration)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Any;
	}
}
