using EntityStates.GreaterWispMonster;

namespace EntityStates.ArchWispMonster;

public class ChargeCannons : EntityStates.GreaterWispMonster.ChargeCannons
{
	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration && base.isAuthority)
		{
			FireCannons nextState = new FireCannons();
			outer.SetNextState(nextState);
		}
	}
}
