using RoR2;
using UnityEngine;

namespace EntityStates.MiniMushroom;

public class SpawnState : BaseState
{
	public static GameObject burrowPrefab;

	public static float duration = 4f;

	public static string spawnSoundString;

	private static int SpawnStateHash = Animator.StringToHash("Spawn");

	private static int SpawnParamHash = Animator.StringToHash("Spawn.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		Util.PlaySound(spawnSoundString, base.gameObject);
		PlayAnimation("Body", SpawnStateHash, SpawnParamHash, duration);
		EffectManager.SimpleMuzzleFlash(burrowPrefab, base.gameObject, "BurrowCenter", transmit: false);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && base.fixedAge >= duration)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Death;
	}
}
