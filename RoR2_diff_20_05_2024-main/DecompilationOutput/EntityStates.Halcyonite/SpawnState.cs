using RoR2;
using UnityEngine;

namespace EntityStates.Halcyonite;

public class SpawnState : EntityState
{
	public static float duration = 2f;

	public static float startingPrintHeight;

	public static float maxPrintHeight;

	public static float startingPrintBias;

	public static float maxPrintBias;

	public static string spawnSoundString;

	public override void OnEnter()
	{
		base.OnEnter();
		base.gameObject.GetComponent<CharacterBody>();
		Util.PlaySound(spawnSoundString, base.gameObject);
		PlayAnimation("Body", "Spawn", "Spawn.playbackRate", duration);
		PrintController printController = GetModelTransform().gameObject.AddComponent<PrintController>();
		printController.printTime = duration;
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
