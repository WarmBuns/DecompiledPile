using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.GlobalSkills.WoundSlash;

public class WoundSlash : BaseSkillState
{
	public static float blastRadius;

	public static float blastDamageCoefficient;

	public static float blastForce;

	public static float blastProcCoefficient;

	public static GameObject blastImpactEffectPrefab;

	public static GameObject slashEffectPrefab;

	public static float slashEffectOffset;

	public static float baseDuration;

	public static string soundString;

	public static float shortHopVelocity;

	private float duration => baseDuration / attackSpeedStat;

	public override void OnEnter()
	{
		base.OnEnter();
		Ray aimRay = GetAimRay();
		Vector3 vector = base.characterBody.corePosition + aimRay.direction * slashEffectOffset;
		Util.PlaySound(soundString, base.gameObject);
		EffectData effectData = new EffectData
		{
			origin = vector,
			rotation = Util.QuaternionSafeLookRotation(aimRay.direction)
		};
		EffectManager.SpawnEffect(slashEffectPrefab, effectData, transmit: true);
		if (NetworkServer.active)
		{
			BlastAttack blastAttack = new BlastAttack();
			blastAttack.attacker = base.gameObject;
			blastAttack.baseDamage = blastDamageCoefficient * damageStat;
			blastAttack.baseForce = blastForce;
			blastAttack.attackerFiltering = AttackerFiltering.NeverHitSelf;
			blastAttack.crit = RollCrit();
			blastAttack.damageType = DamageType.Generic;
			blastAttack.falloffModel = BlastAttack.FalloffModel.None;
			blastAttack.inflictor = base.gameObject;
			blastAttack.position = vector;
			blastAttack.procChainMask = default(ProcChainMask);
			blastAttack.procCoefficient = blastProcCoefficient;
			blastAttack.radius = blastRadius;
			blastAttack.teamIndex = base.teamComponent.teamIndex;
			blastAttack.impactEffect = EffectCatalog.FindEffectIndexFromPrefab(blastImpactEffectPrefab);
			blastAttack.Fire();
		}
		if (base.isAuthority && (bool)base.characterMotor)
		{
			base.characterMotor.velocity.y = Mathf.Max(base.characterMotor.velocity.y, shortHopVelocity);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Pain;
	}
}
