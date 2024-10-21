using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.GameOver;

public class VoidEndingSetSceneAndWaitForPlayers : BaseGameOverControllerState
{
	[SerializeField]
	public SceneDef desiredSceneDef;

	public override void OnEnter()
	{
		base.OnEnter();
		FadeToBlackManager.ForceFullBlack();
		FadeToBlackManager.fadeCount++;
		if (NetworkServer.active)
		{
			Run.instance.AdvanceStage(desiredSceneDef);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (NetworkServer.active && NetworkUser.AllParticipatingNetworkUsersReady() && SceneCatalog.mostRecentSceneDef == desiredSceneDef)
		{
			outer.SetNextState(new VoidEndingPlayCutscene());
		}
	}

	public override void OnExit()
	{
		FadeToBlackManager.fadeCount--;
		base.OnExit();
	}
}
