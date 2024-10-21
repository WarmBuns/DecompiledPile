using EntityStates.MajorConstruct.Stance;
using RoR2;

namespace EntityStates.MajorConstruct;

public class SwitchStance : BaseState
{
	private const string stanceStateMachineName = "Stance";

	public override void OnEnter()
	{
		base.OnEnter();
		if (!base.isAuthority)
		{
			return;
		}
		EntityStateMachine entityStateMachine = EntityStateMachine.FindByCustomName(base.gameObject, "Stance");
		EntityState state = entityStateMachine.state;
		if (!(state is Raised))
		{
			if (state is Lowered)
			{
				entityStateMachine.SetNextState(new LoweredToRaised());
			}
		}
		else
		{
			entityStateMachine.SetNextState(new RaisedToLowered());
		}
		outer.SetNextStateToMain();
	}
}
