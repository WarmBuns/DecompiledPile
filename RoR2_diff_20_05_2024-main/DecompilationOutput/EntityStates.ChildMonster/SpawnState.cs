using RoR2;
using UnityEngine;

namespace EntityStates.ChildMonster;

public class SpawnState : EntityState
{
	public static float duration = 1f;

	public static string spawnSoundString;

	public static GameObject spawnEffectPrefab;

	private ParentEnergyFXController FXController;

	public override void OnEnter()
	{
		base.OnEnter();
		GetModelAnimator();
		PlayAnimation("Gesture, Override", "FrolicEnter", "FrolicEnter.playbackRate", duration);
		Util.PlaySound(spawnSoundString, base.gameObject);
		if ((bool)spawnEffectPrefab)
		{
			EffectManager.SpawnEffect(spawnEffectPrefab, new EffectData
			{
				origin = base.gameObject.transform.position
			}, transmit: true);
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
