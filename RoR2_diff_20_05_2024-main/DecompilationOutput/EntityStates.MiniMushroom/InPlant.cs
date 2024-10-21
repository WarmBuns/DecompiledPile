using RoR2;
using UnityEngine;

namespace EntityStates.MiniMushroom;

public class InPlant : BaseState
{
	public static GameObject burrowPrefab;

	public static float baseDuration;

	public static string burrowInSoundString;

	private float duration;

	private static int PlantStartStateHash = Animator.StringToHash("PlantStart");

	private static int EmptyStateHash = Animator.StringToHash("Empty");

	private static int PlantStartParamHash = Animator.StringToHash("PlantStart.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		Util.PlaySound(burrowInSoundString, base.gameObject);
		EffectManager.SimpleMuzzleFlash(burrowPrefab, base.gameObject, "BurrowCenter", transmit: false);
		PlayAnimation("Plant", PlantStartStateHash, PlantStartParamHash, duration);
	}

	public override void OnExit()
	{
		PlayAnimation("Plant", EmptyStateHash);
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && base.fixedAge >= duration)
		{
			outer.SetNextState(new Plant());
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
