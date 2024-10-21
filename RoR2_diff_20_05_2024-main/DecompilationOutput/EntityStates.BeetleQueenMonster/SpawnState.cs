using RoR2;
using UnityEngine;

namespace EntityStates.BeetleQueenMonster;

public class SpawnState : BaseState
{
	public static float duration = 4f;

	public static GameObject burrowPrefab;

	public static string spawnSoundString;

	private static int SpawnStateHash = Animator.StringToHash("Spawn");

	private static int SpawnParamHash = Animator.StringToHash("Spawn.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		Util.PlaySound(spawnSoundString, base.gameObject);
		GetModelTransform().GetComponent<ChildLocator>();
		PlayAnimation("Body", SpawnStateHash, SpawnParamHash, duration);
		string muzzleName = "BurrowCenter";
		EffectManager.SimpleMuzzleFlash(burrowPrefab, base.gameObject, muzzleName, transmit: false);
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
