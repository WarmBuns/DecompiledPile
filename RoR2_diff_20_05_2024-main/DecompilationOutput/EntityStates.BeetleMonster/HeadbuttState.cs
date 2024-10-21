using System;
using RoR2;
using UnityEngine;

namespace EntityStates.BeetleMonster;

public class HeadbuttState : BaseState
{
	public static float baseDuration = 3.5f;

	public static float damageCoefficient;

	public static float forceMagnitude = 16f;

	public static GameObject hitEffectPrefab;

	public static string attackSoundString;

	private OverlapAttack attack;

	private Animator modelAnimator;

	private RootMotionAccumulator rootMotionAccumulator;

	private float duration;

	public override void OnEnter()
	{
		base.OnEnter();
		rootMotionAccumulator = GetModelRootMotionAccumulator();
		modelAnimator = GetModelAnimator();
		duration = baseDuration / attackSpeedStat;
		attack = new OverlapAttack();
		attack.attacker = base.gameObject;
		attack.inflictor = base.gameObject;
		attack.teamIndex = TeamComponent.GetObjectTeam(attack.attacker);
		attack.damage = damageCoefficient * damageStat;
		attack.hitEffectPrefab = hitEffectPrefab;
		Transform modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			attack.hitBoxGroup = Array.Find(modelTransform.GetComponents<HitBoxGroup>(), (HitBoxGroup element) => element.groupName == "Headbutt");
		}
		Util.PlaySound(attackSoundString, base.gameObject);
		PlayCrossfade("Body", "Headbutt", "Headbutt.playbackRate", duration, 0.1f);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if ((bool)rootMotionAccumulator)
		{
			Vector3 vector = rootMotionAccumulator.ExtractRootMotion();
			if (vector != Vector3.zero && base.isAuthority && (bool)base.characterMotor)
			{
				base.characterMotor.rootMotion += vector;
			}
		}
		if (base.isAuthority)
		{
			attack.forceVector = (base.characterDirection ? (base.characterDirection.forward * forceMagnitude) : Vector3.zero);
			if ((bool)base.characterDirection && (bool)base.inputBank)
			{
				base.characterDirection.moveVector = base.inputBank.aimDirection;
			}
			if ((bool)modelAnimator && modelAnimator.GetFloat("Headbutt.hitBoxActive") > 0.5f)
			{
				attack.Fire();
			}
		}
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}
