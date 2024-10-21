using EntityStates;
using UnityEngine.Networking;

namespace RoR2;

public class GameEndingController : NetworkBehaviour
{
	private class GameEndingControllerBaseState : BaseState
	{
		protected GameEndingController gameEndingController { get; private set; }

		public override void OnEnter()
		{
			base.OnEnter();
			gameEndingController = GetComponent<GameEndingController>();
		}
	}

	private class EndingCutsceneState : GameEndingControllerBaseState
	{
	}

	private class CreditsState : GameEndingControllerBaseState
	{
	}

	private class PostGameReportState : GameEndingControllerBaseState
	{
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		bool result = default(bool);
		return result;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
	}

	public override void PreStartClient()
	{
	}
}
