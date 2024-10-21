namespace EntityStates.Railgunner.Scope;

public class WindUpScopeLight : BaseWindUp
{
	protected override BaseActive GetNextState()
	{
		return new ActiveScopeLight();
	}
}
