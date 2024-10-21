using System;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.LemurianMonster;

public class Bite : BaseState
{
	public static float baseDuration = 3.5f;

	public static float damageCoefficient = 4f;

	public static float forceMagnitude = 16f;

	public static float radius = 3f;

	public static GameObject hitEffectPrefab;

	public static GameObject biteEffectPrefab;

	public static string attackString;

	private OverlapAttack attack;

	private Animator modelAnimator;

	private float duration;

	private bool hasBit;

	private static int BiteStateHash = Animator.StringToHash("Bite");

	private static int BiteParamHash = Animator.StringToHash("Bite.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		modelAnimator = GetModelAnimator();
		Transform modelTransform = GetModelTransform();
		attack = new OverlapAttack();
		attack.attacker = base.gameObject;
		attack.inflictor = base.gameObject;
		attack.teamIndex = TeamComponent.GetObjectTeam(attack.attacker);
		attack.damage = damageCoefficient * damageStat;
		attack.hitEffectPrefab = hitEffectPrefab;
		attack.isCrit = Util.CheckRoll(critStat, base.characterBody.master);
		Util.PlayAttackSpeedSound(attackString, base.gameObject, attackSpeedStat);
		if ((bool)modelTransform)
		{
			attack.hitBoxGroup = Array.Find(modelTransform.GetComponents<HitBoxGroup>(), (HitBoxGroup element) => element.groupName == "Bite");
		}
		if ((bool)modelAnimator)
		{
			PlayAnimation("Gesture", BiteStateHash, BiteParamHash, duration);
		}
		if ((bool)base.characterBody)
		{
			base.characterBody.SetAimTimer(2f);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (NetworkServer.active && (bool)modelAnimator && modelAnimator.GetFloat("Bite.hitBoxActive") > 0.1f)
		{
			if (!hasBit)
			{
				EffectManager.SimpleMuzzleFlash(biteEffectPrefab, base.gameObject, "MuzzleMouth", transmit: true);
				hasBit = true;
			}
			attack.forceVector = base.transform.forward * forceMagnitude;
			attack.Fire();
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
