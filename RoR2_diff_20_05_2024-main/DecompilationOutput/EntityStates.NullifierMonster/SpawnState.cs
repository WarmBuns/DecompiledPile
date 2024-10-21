using RoR2;
using UnityEngine;

namespace EntityStates.NullifierMonster;

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
		if ((bool)spawnEffectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(spawnEffectPrefab, base.gameObject, "PortalEffect", transmit: false);
		}
		Transform modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			modelTransform.GetComponent<PrintController>().enabled = true;
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
