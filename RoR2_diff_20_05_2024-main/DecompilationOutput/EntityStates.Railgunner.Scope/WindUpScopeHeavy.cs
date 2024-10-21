namespace EntityStates.Railgunner.Scope;

public class WindUpScopeHeavy : BaseWindUp
{
	protected override BaseActive GetNextState()
	{
		return new ActiveScopeHeavy();
	}
}
