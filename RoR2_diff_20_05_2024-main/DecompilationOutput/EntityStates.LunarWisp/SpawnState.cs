using RoR2;
using UnityEngine;

namespace EntityStates.LunarWisp;

public class SpawnState : BaseState
{
	public static float duration;

	public static string spawnSoundString;

	public static float spawnEffectsDelay;

	private bool spawnEffectsTriggered;

	public static GameObject spawnEffectPrefab;

	public static string spawnEffectMuzzleName;

	public static float printDuration;

	private float mPrintDuration;

	private LunarWispFXController FXController;

	private static int SpawnStateHash = Animator.StringToHash("Spawn");

	public override void OnEnter()
	{
		base.OnEnter();
		FXController = base.characterBody.GetComponent<LunarWispFXController>();
		FXController.TurnOffFX();
		mPrintDuration = printDuration;
		Util.PlaySound(spawnSoundString, base.gameObject);
		PlayAnimation("Body", SpawnStateHash);
		PrintController component = GetModelTransform().gameObject.GetComponent<PrintController>();
		component.enabled = false;
		component.printTime = mPrintDuration;
		component.startingPrintHeight = 25f;
		component.maxPrintHeight = 3.15f;
		component.startingPrintBias = 4f;
		component.maxPrintBias = 1.4f;
		component.disableWhenFinished = true;
		component.printCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
		component.enabled = true;
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= spawnEffectsDelay && !spawnEffectsTriggered)
		{
			spawnEffectsTriggered = true;
			EffectManager.SimpleMuzzleFlash(spawnEffectPrefab, base.gameObject, spawnEffectMuzzleName, transmit: false);
			if ((bool)FXController)
			{
				FXController.TurnOnFX();
			}
		}
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
