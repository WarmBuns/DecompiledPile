using RoR2;

namespace EntityStates.GameOver;

public class BaseGameOverControllerState : BaseState
{
	protected GameOverController gameOverController { get; private set; }

	protected GameEndingDef gameEnding { get; private set; }

	public override void OnEnter()
	{
		base.OnEnter();
		gameOverController = GetComponent<GameOverController>();
		gameEnding = gameOverController.runReport.gameEnding;
	}
}
