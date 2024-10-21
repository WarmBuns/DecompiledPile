using RoR2;
using UnityEngine.Networking;

namespace EntityStates.GameOver;

public class RoR2MainEndingPlayCutscene : BaseGameOverControllerState
{
	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (NetworkServer.active && (!OutroCutsceneController.instance || OutroCutsceneController.instance.cutsceneIsFinished))
		{
			outer.SetNextStateToMain();
		}
	}

	public override void OnExit()
	{
		if ((bool)OutroCutsceneController.instance && (bool)OutroCutsceneController.instance.playableDirector)
		{
			OutroCutsceneController.instance.playableDirector.time = OutroCutsceneController.instance.playableDirector.duration;
		}
		base.OnExit();
	}
}
