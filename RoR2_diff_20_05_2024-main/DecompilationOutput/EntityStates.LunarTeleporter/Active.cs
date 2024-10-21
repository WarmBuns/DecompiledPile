using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.LunarTeleporter;

public class Active : LunarTeleporterBaseState
{
	private static int ActiveStateHash = Animator.StringToHash("Active");

	public override void OnEnter()
	{
		base.OnEnter();
		preferredInteractability = Interactability.Available;
		PlayAnimation("Base", ActiveStateHash);
		outer.mainStateType = new SerializableEntityStateType(typeof(ActiveToIdle));
		if (NetworkServer.active)
		{
			teleporterInteraction.sceneExitController.useRunNextStageScene = false;
		}
	}
}
