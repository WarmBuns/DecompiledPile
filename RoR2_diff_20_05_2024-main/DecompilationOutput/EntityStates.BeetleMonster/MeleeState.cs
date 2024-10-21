using System;
using RoR2;
using UnityEngine;

namespace EntityStates.BeetleMonster;

public class MeleeState : EntityState
{
	public static float duration = 3.5f;

	public static float damage = 10f;

	public static float forceMagnitude = 10f;

	private OverlapAttack attack;

	private Animator modelAnimator;

	public override void OnEnter()
	{
		base.OnEnter();
		modelAnimator = GetModelAnimator();
		attack = new OverlapAttack();
		attack.attacker = base.gameObject;
		attack.inflictor = base.gameObject;
		attack.teamIndex = TeamComponent.GetObjectTeam(attack.attacker);
		attack.damage = 10f;
		Transform modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			attack.hitBoxGroup = Array.Find(modelTransform.GetComponents<HitBoxGroup>(), (HitBoxGroup element) => element.groupName == "Melee1");
		}
		PlayCrossfade("Body", "Melee1", "Melee1.playbackRate", duration, 0.1f);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority)
		{
			attack.forceVector = (base.characterDirection ? (base.characterDirection.forward * forceMagnitude) : Vector3.zero);
			if ((bool)modelAnimator && modelAnimator.GetFloat("Melee1.hitBoxActive") > 0.5f)
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
