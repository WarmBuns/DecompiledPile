using RoR2;
using UnityEngine.Networking;

namespace EntityStates.Interactables.MSObelisk;

public class TransitionToNextStage : BaseState
{
	public static float duration;

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (NetworkServer.active && base.fixedAge >= duration)
		{
			Stage.instance.BeginAdvanceStage(SceneCatalog.GetSceneDefFromSceneName("limbo"));
			outer.SetNextState(new Idle());
		}
	}
}
