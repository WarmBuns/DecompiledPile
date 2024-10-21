using UnityEngine.Networking;

namespace EntityStates.GameOver;

public class ShowReport : BaseGameOverControllerState
{
	public override void OnEnter()
	{
		base.OnEnter();
		if (NetworkClient.active)
		{
			base.gameOverController.shouldDisplayGameEndReportPanels = true;
		}
	}

	public override void OnExit()
	{
		base.OnExit();
	}
}
