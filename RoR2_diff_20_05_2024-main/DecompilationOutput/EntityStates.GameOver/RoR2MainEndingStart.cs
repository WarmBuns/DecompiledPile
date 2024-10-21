using UnityEngine.Networking;

namespace EntityStates.GameOver;

public class RoR2MainEndingStart : BaseGameOverControllerState
{
	public static float duration = 6f;

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (NetworkServer.active && base.fixedAge >= duration)
		{
			outer.SetNextState(new RoR2MainEndingSetSceneAndWaitForPlayers());
		}
	}

	public override void OnEnter()
	{
		base.OnEnter();
	}

	public override void OnExit()
	{
		base.OnExit();
	}
}
