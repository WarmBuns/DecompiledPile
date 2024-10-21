using RoR2;
using UnityEngine;

namespace EntityStates.NewtMonster;

public class SpawnState : EntityState
{
	public static float duration = 2f;

	public static string spawnSoundString;

	public static GameObject spawnEffectPrefab;

	private static int SpawnStateHash = Animator.StringToHash("Spawn");

	private static int SpawnParamHash = Animator.StringToHash("Spawn.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		GetModelAnimator();
		Util.PlaySound(spawnSoundString, base.gameObject);
		PlayAnimation("Body", SpawnStateHash, SpawnParamHash, duration);
		if ((bool)spawnEffectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(spawnEffectPrefab, base.gameObject, "SpawnEffectOrigin", transmit: false);
		}
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
