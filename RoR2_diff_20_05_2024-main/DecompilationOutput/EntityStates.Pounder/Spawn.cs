using RoR2;
using UnityEngine;

namespace EntityStates.Pounder;

public class Spawn : BaseState
{
	public static GameObject spawnPrefab;

	public static float baseDuration;

	public static string spawnSoundString;

	private float duration;

	private static int SpawnStateHash = Animator.StringToHash("Spawn");

	private static int SpawnParamHash = Animator.StringToHash("Spawn.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		Util.PlaySound(spawnSoundString, base.gameObject);
		EffectManager.SimpleMuzzleFlash(spawnPrefab, base.gameObject, "Feet", transmit: false);
		PlayAnimation("Base", SpawnStateHash, SpawnParamHash, duration);
	}

	public override void OnExit()
	{
		base.OnExit();
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
		return InterruptPriority.Skill;
	}
}
