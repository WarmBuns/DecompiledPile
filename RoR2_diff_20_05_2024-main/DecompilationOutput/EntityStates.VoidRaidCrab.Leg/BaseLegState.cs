using RoR2.VoidRaidCrab;

namespace EntityStates.VoidRaidCrab.Leg;

public class BaseLegState : EntityState
{
	protected LegController legController { get; private set; }

	public override void OnEnter()
	{
		base.OnEnter();
		legController = legController ?? GetComponent<LegController>();
	}

	public override void ModifyNextState(EntityState nextState)
	{
		base.ModifyNextState(nextState);
		if (nextState is BaseLegState baseLegState)
		{
			baseLegState.legController = legController;
		}
	}
}
