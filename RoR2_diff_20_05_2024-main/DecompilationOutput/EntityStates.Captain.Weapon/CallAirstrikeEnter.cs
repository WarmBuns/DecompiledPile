namespace EntityStates.Captain.Weapon;

public class CallAirstrikeEnter : BaseSkillState
{
	public override void OnEnter()
	{
		base.OnEnter();
		if (base.isAuthority)
		{
			switch (base.activatorSkillSlot.stock)
			{
			case 2:
				outer.SetNextState(new CallAirstrike1());
				break;
			case 1:
				outer.SetNextState(new CallAirstrike2());
				break;
			case 0:
				outer.SetNextState(new CallAirstrike3());
				break;
			default:
				outer.SetNextState(new CallAirstrike1());
				break;
			}
		}
	}
}
