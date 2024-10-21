using System;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.ImpMonster;

public class DoubleSlash : BaseState
{
	public static float baseDuration = 3.5f;

	public static float damageCoefficient = 4f;

	public static float procCoefficient;

	public static float selfForce;

	public static float forceMagnitude = 16f;

	public static GameObject hitEffectPrefab;

	public static GameObject swipeEffectPrefab;

	public static string enterSoundString;

	public static string slashSoundString;

	public static float walkSpeedPenaltyCoefficient;

	private OverlapAttack attack;

	private Animator modelAnimator;

	private float duration;

	private int slashCount;

	private Transform modelTransform;

	private static int DoubleSlashStateHash = Animator.StringToHash("DoubleSlash");

	private static int DoubleSlashParamHash = Animator.StringToHash("DoubleSlash.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		modelAnimator = GetModelAnimator();
		modelTransform = GetModelTransform();
		base.characterMotor.walkSpeedPenaltyCoefficient = walkSpeedPenaltyCoefficient;
		attack = new OverlapAttack();
		attack.attacker = base.gameObject;
		attack.inflictor = base.gameObject;
		attack.teamIndex = TeamComponent.GetObjectTeam(attack.attacker);
		attack.damage = damageCoefficient * damageStat;
		attack.hitEffectPrefab = hitEffectPrefab;
		attack.isCrit = Util.CheckRoll(critStat, base.characterBody.master);
		attack.procCoefficient = procCoefficient;
		attack.damageType = DamageType.BleedOnHit;
		Util.PlayAttackSpeedSound(enterSoundString, base.gameObject, attackSpeedStat);
		if ((bool)modelAnimator)
		{
			PlayAnimation("Gesture, Additive", DoubleSlashStateHash, DoubleSlashParamHash, duration);
			PlayAnimation("Gesture, Override", DoubleSlashStateHash, DoubleSlashParamHash, duration);
		}
		if ((bool)base.characterBody)
		{
			base.characterBody.SetAimTimer(duration + 2f);
		}
	}

	public override void OnExit()
	{
		base.characterMotor.walkSpeedPenaltyCoefficient = 1f;
		base.OnExit();
	}

	private void HandleSlash(string animatorParamName, string muzzleName, string hitBoxGroupName)
	{
		if (!(modelAnimator.GetFloat(animatorParamName) > 0.1f))
		{
			return;
		}
		Util.PlaySound(slashSoundString, base.gameObject);
		EffectManager.SimpleMuzzleFlash(swipeEffectPrefab, base.gameObject, muzzleName, transmit: true);
		slashCount++;
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
		attack.Fire();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (NetworkServer.active && (bool)modelAnimator)
		{
			switch (slashCount)
			{
			case 0:
				HandleSlash("HandR.hitBoxActive", "SwipeRight", "HandR");
				break;
			case 1:
				HandleSlash("HandL.hitBoxActive", "SwipeLeft", "HandL");
				break;
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
