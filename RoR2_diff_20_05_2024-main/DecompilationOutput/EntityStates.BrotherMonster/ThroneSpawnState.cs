using RoR2;
using UnityEngine;

namespace EntityStates.BrotherMonster;

public class ThroneSpawnState : BaseState
{
	public static GameObject spawnEffectPrefab;

	public static string muzzleString;

	public static float initialDelay;

	public static float duration;

	private bool hasPlayedAnimation;

	private static int ThroneStateHash = Animator.StringToHash("Throne");

	private static int ThroneToIdleStateHash = Animator.StringToHash("ThroneToIdle");

	private static int BufferEmptyStateHash = Animator.StringToHash("BufferEmpty");

	private static int ThroneToIdleParamHash = Animator.StringToHash("ThroneToIdle.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation("Body", ThroneStateHash);
		PlayAnimation("FullBody Override", BufferEmptyStateHash);
		EffectManager.SimpleMuzzleFlash(spawnEffectPrefab, base.gameObject, muzzleString, transmit: false);
	}

	private void PlayAnimation()
	{
		hasPlayedAnimation = true;
		PlayAnimation("Body", ThroneToIdleStateHash, ThroneToIdleParamHash, duration);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge > initialDelay && !hasPlayedAnimation)
		{
			PlayAnimation();
		}
		if (base.fixedAge > initialDelay + duration)
		{
			outer.SetNextStateToMain();
		}
	}
}
