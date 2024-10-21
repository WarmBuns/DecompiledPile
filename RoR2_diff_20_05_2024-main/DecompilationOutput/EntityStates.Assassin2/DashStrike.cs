using System;
using RoR2;
using RoR2.CharacterAI;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Assassin2;

public class DashStrike : BaseState
{
	public static float dashDuration = 1f;

	public static float slashDuration = 1.667f;

	public static float damageCoefficient = 4f;

	public static float procCoefficient;

	public static float selfForce;

	public static float forceMagnitude = 16f;

	public static float maxDashSpeedCoefficient = 4f;

	public static float maxSlashDistance = 20f;

	public static GameObject hitEffectPrefab;

	public static GameObject swipeEffectPrefab;

	public static string enterSoundString;

	public static string slashSoundString;

	private Vector3 targetMoveVector;

	private Vector3 targetMoveVectorVelocity;

	private OverlapAttack attack;

	private Animator modelAnimator;

	private float duration;

	private int slashCount;

	private Transform modelTransform;

	private bool dashComplete;

	private int handParamHash;

	private int swordParamHash;

	private float calculatedDashSpeed;

	private bool startedSlash;

	private static int DashStateHash = Animator.StringToHash("Dash");

	private static int SwordStrikeStateHash = Animator.StringToHash("SwordStrike");

	private static int forwardSpeedParamHash = Animator.StringToHash("forwardSpeed");

	public override void OnEnter()
	{
		base.OnEnter();
		duration = (dashDuration + slashDuration) / attackSpeedStat;
		modelAnimator = GetModelAnimator();
		modelTransform = GetModelTransform();
		base.characterMotor.velocity = Vector3.zero;
		float sqrMagnitude = (base.characterBody.master.GetComponent<BaseAI>().currentEnemy.characterBody.corePosition - base.characterBody.corePosition).sqrMagnitude;
		calculatedDashSpeed = Util.Remap(sqrMagnitude, 0f, maxSlashDistance * maxSlashDistance, 0f, maxDashSpeedCoefficient);
		attack = new OverlapAttack();
		attack.attacker = base.gameObject;
		attack.inflictor = base.gameObject;
		attack.teamIndex = TeamComponent.GetObjectTeam(attack.attacker);
		attack.damage = damageCoefficient * damageStat;
		attack.hitEffectPrefab = hitEffectPrefab;
		attack.isCrit = Util.CheckRoll(critStat, base.characterBody.master);
		attack.procCoefficient = procCoefficient;
		attack.damageType = DamageType.Generic;
		Util.PlayAttackSpeedSound(enterSoundString, base.gameObject, attackSpeedStat);
		if ((bool)modelAnimator)
		{
			PlayAnimation("Gesture", DashStateHash);
			handParamHash = Animator.StringToHash("HandStrike");
			swordParamHash = Animator.StringToHash("SwordStrike");
		}
	}

	public override void OnExit()
	{
		base.characterMotor.walkSpeedPenaltyCoefficient = 1f;
		base.OnExit();
	}

	private void HandleSlash(int animatorParamHash, string muzzleName, string hitBoxGroupName)
	{
		bool flag = false;
		if (modelAnimator.GetFloat(animatorParamHash) > 0.1f)
		{
			if (!startedSlash)
			{
				Util.PlaySound(slashSoundString, base.gameObject);
				EffectManager.SimpleMuzzleFlash(swipeEffectPrefab, base.gameObject, muzzleName, transmit: true);
				startedSlash = true;
			}
			if ((bool)modelTransform)
			{
				attack.hitBoxGroup = Array.Find(modelTransform.GetComponents<HitBoxGroup>(), (HitBoxGroup element) => element.groupName == hitBoxGroupName);
			}
			if ((bool)base.healthComponent)
			{
				base.healthComponent.TakeDamageForce(base.characterDirection.forward * selfForce, alwaysApply: true);
			}
			attack.ResetIgnoredHealthComponents();
			if ((bool)base.characterDirection)
			{
				attack.forceVector = base.characterDirection.forward * forceMagnitude;
			}
			flag = attack.Fire();
		}
		else if (startedSlash || flag)
		{
			slashCount++;
			startedSlash = false;
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (!dashComplete)
		{
			targetMoveVector = Vector3.ProjectOnPlane(Vector3.SmoothDamp(targetMoveVector, base.inputBank.aimDirection, ref targetMoveVectorVelocity, 0f, 0f), Vector3.up).normalized;
			base.characterDirection.moveVector = targetMoveVector;
			Vector3 forward = base.characterDirection.forward;
			float value = moveSpeedStat * calculatedDashSpeed;
			base.characterMotor.moveDirection = forward * calculatedDashSpeed;
			modelAnimator.SetFloat(forwardSpeedParamHash, value);
		}
		if (NetworkServer.active)
		{
			if (base.fixedAge > dashDuration && !dashComplete)
			{
				PlayAnimation("Gesture", SwordStrikeStateHash);
				dashComplete = true;
			}
			if ((bool)modelAnimator)
			{
				switch (slashCount)
				{
				case 0:
					HandleSlash(handParamHash, "ShurikenTag", "ShurikenHitbox");
					break;
				case 1:
					HandleSlash(swordParamHash, "Sword", "SwordHitbox");
					break;
				}
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
