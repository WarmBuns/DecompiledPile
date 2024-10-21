using RoR2;
using UnityEngine;

namespace EntityStates.ClayBruiserMonster;

public class SpawnState : BaseState
{
	public static float duration;

	public static string spawnSoundString;

	public static GameObject spawnEffectPrefab;

	public static string spawnEffectChildString;

	public static float startingPrintBias;

	public static float maxPrintBias;

	public static float printDuration;

	private static int SpawnStateHash = Animator.StringToHash("Spawn");

	private static int SpawnParamHash = Animator.StringToHash("Spawn.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		EffectManager.SimpleMuzzleFlash(spawnEffectPrefab, base.gameObject, spawnEffectChildString, transmit: false);
		Util.PlaySound(spawnSoundString, base.gameObject);
		PlayAnimation("Body", SpawnStateHash, SpawnParamHash, duration);
		PrintController printController = GetModelTransform().gameObject.AddComponent<PrintController>();
		printController.printTime = printDuration;
		printController.enabled = true;
		printController.startingPrintHeight = 0.3f;
		printController.maxPrintHeight = 0.3f;
		printController.startingPrintBias = startingPrintBias;
		printController.maxPrintBias = maxPrintBias;
		printController.disableWhenFinished = true;
		printController.printCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
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
