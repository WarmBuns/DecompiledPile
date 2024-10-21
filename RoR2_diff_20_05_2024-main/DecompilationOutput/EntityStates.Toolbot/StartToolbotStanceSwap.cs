using System.Linq;
using RoR2;

namespace EntityStates.Toolbot;

public class StartToolbotStanceSwap : BaseState
{
	private EntityStateMachine stanceStateMachine;

	public override void OnEnter()
	{
		base.OnEnter();
		if (base.isAuthority)
		{
			stanceStateMachine = base.gameObject.GetComponents<EntityStateMachine>().FirstOrDefault((EntityStateMachine c) => c.customName == "Stance");
			if (stanceStateMachine?.state is ToolbotStanceBase toolbotStanceBase && toolbotStanceBase.swapStateType != null)
			{
				stanceStateMachine.SetNextState(new ToolbotStanceSwap
				{
					previousStanceState = toolbotStanceBase.GetType(),
					nextStanceState = toolbotStanceBase.swapStateType
				});
			}
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		outer.SetNextStateToMain();
	}
}
