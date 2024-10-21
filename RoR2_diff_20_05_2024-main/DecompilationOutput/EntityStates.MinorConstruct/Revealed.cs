namespace EntityStates.MinorConstruct;

public class Revealed : BaseHideState
{
	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.characterBody.outOfCombat && base.characterBody.outOfDanger)
		{
			outer.SetNextState(new Hidden());
		}
	}
}
