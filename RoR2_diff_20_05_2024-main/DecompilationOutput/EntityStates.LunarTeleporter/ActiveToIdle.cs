using RoR2;
using UnityEngine;

namespace EntityStates.LunarTeleporter;

public class ActiveToIdle : LunarTeleporterBaseState
{
	public static float duration;

	private static int ActiveToIdleStateHash = Animator.StringToHash("ActiveToIdle");

	private static int playbackRateParamHash = Animator.StringToHash("playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation("Base", ActiveToIdleStateHash, playbackRateParamHash, duration);
		preferredInteractability = Interactability.Disabled;
		Util.PlaySound("Play_boss_spawn_rumble", base.gameObject);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge > duration)
		{
			outer.SetNextState(new Idle());
		}
	}

	public override void OnExit()
	{
		Chat.SendBroadcastChat(new Chat.SimpleChatMessage
		{
			baseToken = "LUNAR_TELEPORTER_IDLE"
		});
		base.OnExit();
	}
}
