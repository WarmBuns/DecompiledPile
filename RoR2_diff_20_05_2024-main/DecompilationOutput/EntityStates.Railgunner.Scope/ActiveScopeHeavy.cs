namespace EntityStates.Railgunner.Scope;

public class ActiveScopeHeavy : BaseActive
{
	protected override BaseWindDown GetNextState()
	{
		return new WindDownScopeHeavy();
	}
}
