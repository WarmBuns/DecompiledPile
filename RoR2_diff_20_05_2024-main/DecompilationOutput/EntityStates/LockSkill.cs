namespace EntityStates;

public class LockSkill : BaseState
{
	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Death;
	}
}
