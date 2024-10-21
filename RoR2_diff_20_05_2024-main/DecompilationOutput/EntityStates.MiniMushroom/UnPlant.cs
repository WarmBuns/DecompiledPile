using RoR2;
using UnityEngine;

namespace EntityStates.MiniMushroom;

public class UnPlant : BaseState
{
	public static GameObject plantEffectPrefab;

	public static float baseDuration;

	public static string UnplantOutSoundString;

	private float stopwatch;

	private Transform modelTransform;

	private ChildLocator childLocator;

	private float duration;

	private static int PlantEndStateHash = Animator.StringToHash("PlantEnd");

	private static int PlantEndParamHash = Animator.StringToHash("PlantEnd.playbackRate");

	private static int EmptyStateHash = Animator.StringToHash("Empty");

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		EffectManager.SimpleMuzzleFlash(plantEffectPrefab, base.gameObject, "BurrowCenter", transmit: false);
		Util.PlaySound(UnplantOutSoundString, base.gameObject);
		PlayAnimation("Plant", PlantEndStateHash, PlantEndParamHash, duration);
	}

	public override void OnExit()
	{
		PlayAnimation("Plant, Additive", EmptyStateHash);
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
