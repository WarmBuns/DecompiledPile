using RoR2;
using UnityEngine;

namespace EntityStates.ClayGrenadier;

public class SpawnState : BaseState
{
	[SerializeField]
	public float duration;

	[SerializeField]
	public string spawnSoundString;

	[SerializeField]
	public GameObject spawnEffectPrefab;

	[SerializeField]
	public string spawnEffectChildString;

	[SerializeField]
	public float startingPrintBias;

	[SerializeField]
	public float maxPrintBias;

	[SerializeField]
	public float printDuration;

	[SerializeField]
	public float startingPrintHeight = 0.3f;

	[SerializeField]
	public float maxPrintHeight = 0.3f;

	[SerializeField]
	public string animationLayerName = "Body";

	[SerializeField]
	public string animationStateName = "Spawn";

	[SerializeField]
	public string playbackRateParam = "Spawn.playbackRate";

	public override void OnEnter()
	{
		base.OnEnter();
		EffectManager.SimpleMuzzleFlash(spawnEffectPrefab, base.gameObject, spawnEffectChildString, transmit: false);
		Util.PlaySound(spawnSoundString, base.gameObject);
		PlayAnimation(animationLayerName, animationStateName, playbackRateParam, duration);
		PrintController printController = GetModelTransform().gameObject.AddComponent<PrintController>();
		printController.printTime = printDuration;
		printController.enabled = true;
		printController.startingPrintHeight = startingPrintHeight;
		printController.maxPrintHeight = maxPrintHeight;
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
