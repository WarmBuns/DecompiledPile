namespace EntityStates.Captain.Weapon;

public class CallAirstrikeAltEnter : BaseSkillState
{
	public override void OnEnter()
	{
		base.OnEnter();
		if (base.isAuthority)
		{
			outer.SetNextState(new CallAirstrikeAlt());
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}
