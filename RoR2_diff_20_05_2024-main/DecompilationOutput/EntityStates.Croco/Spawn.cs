using RoR2;
using UnityEngine;

namespace EntityStates.Croco;

public class Spawn : BaseState
{
	public static float minimumSleepDuration;

	public static GameObject spawnEffectPrefab;

	private Animator modelAnimator;

	private static int SleepLoopStateHash = Animator.StringToHash("SleepLoop");

	public override void OnEnter()
	{
		base.OnEnter();
		base.modelLocator.normalizeToFloor = true;
		modelAnimator = GetModelAnimator();
		if ((bool)modelAnimator)
		{
			modelAnimator.SetFloat(AnimationParameters.aimWeight, 0f);
		}
		PlayAnimation("Body", SleepLoopStateHash);
		EffectManager.SpawnEffect(spawnEffectPrefab, new EffectData
		{
			origin = base.characterBody.footPosition
		}, transmit: false);
	}

	public override void OnExit()
	{
		if ((bool)modelAnimator)
		{
			modelAnimator.SetFloat(AnimationParameters.aimWeight, 1f);
		}
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && base.fixedAge >= minimumSleepDuration && (base.inputBank.moveVector.sqrMagnitude >= Mathf.Epsilon || base.inputBank.CheckAnyButtonDown()))
		{
			outer.SetNextState(new WakeUp());
		}
	}
}
