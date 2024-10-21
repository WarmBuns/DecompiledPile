using RoR2;
using UnityEngine;

namespace EntityStates.Assassin2;

public class SpawnState : BaseState
{
	private float stopwatch;

	public static float animDuration = 1.5f;

	public static float duration = 4f;

	public static string spawnSoundString;

	public static GameObject spawnEffectPrefab;

	private static int SpawnStateHash = Animator.StringToHash("Spawn");

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation("Body", SpawnStateHash);
		Util.PlaySound(spawnSoundString, base.gameObject);
		if ((bool)spawnEffectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(spawnEffectPrefab, base.gameObject, "Base", transmit: false);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		stopwatch += GetDeltaTime();
		if (stopwatch >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Death;
	}
}
