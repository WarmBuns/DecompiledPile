using RoR2;
using UnityEngine;

namespace EntityStates.GravekeeperBoss;

public class SpawnState : BaseState
{
	public static float duration = 4f;

	public static string spawnSoundString;

	public static GameObject spawnEffectPrefab;

	private static int SpawnStateHash = Animator.StringToHash("Spawn");

	private static int SpawnParamHash = Animator.StringToHash("Spawn.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation("Body", SpawnStateHash, SpawnParamHash, duration);
		Util.PlaySound(spawnSoundString, base.gameObject);
		EffectManager.SimpleMuzzleFlash(spawnEffectPrefab, base.gameObject, "Root", transmit: false);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Death;
	}
}
