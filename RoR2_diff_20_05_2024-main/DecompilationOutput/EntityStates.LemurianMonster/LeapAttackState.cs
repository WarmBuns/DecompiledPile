using System;
using RoR2;
using UnityEngine;

namespace EntityStates.LemurianMonster;

public class LeapAttackState : BaseState
{
	public static float baseDuration = 3.5f;

	public static float damage = 10f;

	public static float forceMagnitude = 16f;

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
		attack.damage = damage;
		Transform modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			attack.hitBoxGroup = Array.Find(modelTransform.GetComponents<HitBoxGroup>(), (HitBoxGroup element) => element.groupName == "LeapAttack");
		}
		PlayCrossfade("Body", "LeapAttack", "LeapAttack.playbackRate", duration, 0.1f);
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
			if ((bool)modelAnimator && modelAnimator.GetFloat("LeapAttack.hitBoxActive") > 0.5f)
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
