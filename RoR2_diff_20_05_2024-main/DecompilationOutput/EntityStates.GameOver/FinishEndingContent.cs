using UnityEngine.Networking;

namespace EntityStates.GameOver;

public class FinishEndingContent : BaseGameOverControllerState
{
	public override void OnEnter()
	{
		base.OnEnter();
		if (NetworkServer.active)
		{
			outer.SetNextState(base.gameEnding.showCredits ? ((BaseGameOverControllerState)new ShowCredits()) : ((BaseGameOverControllerState)new ShowReport()));
		}
	}
}
