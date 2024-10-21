using RoR2;
using UnityEngine;

namespace EntityStates.ParentMonster;

public class SpawnState : EntityState
{
	public static float duration = 2f;

	public static string spawnSoundString;

	public static GameObject spawnEffectPrefab;

	private ParentEnergyFXController FXController;

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
			EffectData effectData = new EffectData
			{
				origin = base.modelLocator.modelTransform.GetComponent<ChildLocator>().FindChild("SpawnEffectOrigin").position
			};
			EffectManager.SpawnEffect(spawnEffectPrefab, effectData, transmit: true);
		}
		PrintController component = base.modelLocator.modelTransform.gameObject.GetComponent<PrintController>();
		component.enabled = false;
		component.printTime = duration;
		component.startingPrintHeight = 4f;
		component.maxPrintHeight = -1f;
		component.startingPrintBias = 2f;
		component.maxPrintBias = 0.95f;
		component.disableWhenFinished = true;
		component.printCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
		component.enabled = true;
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
