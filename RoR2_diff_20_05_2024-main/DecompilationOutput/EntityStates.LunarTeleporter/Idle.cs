using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.LunarTeleporter;

public class Idle : LunarTeleporterBaseState
{
	private static int IdleStateHash = Animator.StringToHash("Idle");

	public override void OnEnter()
	{
		base.OnEnter();
		preferredInteractability = Interactability.Available;
		PlayAnimation("Base", IdleStateHash);
		outer.mainStateType = new SerializableEntityStateType(typeof(IdleToActive));
		if (NetworkServer.active)
		{
			teleporterInteraction.sceneExitController.useRunNextStageScene = true;
		}
	}
}
