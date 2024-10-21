using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.BrotherMonster;

public class FistSlam : BaseState
{
	public static float baseDuration = 3.5f;

	public static float damageCoefficient = 4f;

	public static float forceMagnitude = 16f;

	public static float upwardForce;

	public static float radius = 3f;

	public static string attackSoundString;

	public static string muzzleString;

	public static float healthCostFraction;

	public static GameObject chargeEffectPrefab;

	public static GameObject slamImpactEffect;

	public static GameObject waveProjectilePrefab;

	public static int waveProjectileCount;

	public static float waveProjectileDamageCoefficient;

	public static float waveProjectileForce;

	private BlastAttack attack;

	private Animator modelAnimator;

	private Transform modelTransform;

	private bool hasAttacked;

	private float duration;

	private GameObject chargeInstance;

	private static int BufferEmptyStateHash = Animator.StringToHash("BufferEmpty");

	public override void OnEnter()
	{
		base.OnEnter();
		modelAnimator = GetModelAnimator();
		modelTransform = GetModelTransform();
		duration = baseDuration / attackSpeedStat;
		Util.PlayAttackSpeedSound(attackSoundString, base.gameObject, attackSpeedStat);
		PlayCrossfade("FullBody Override", "FistSlam", "FistSlam.playbackRate", duration, 0.1f);
		if ((bool)base.characterDirection)
		{
			base.characterDirection.moveVector = base.characterDirection.forward;
		}
		if ((bool)modelTransform)
		{
			AimAnimator component = modelTransform.GetComponent<AimAnimator>();
			if ((bool)component)
			{
				component.enabled = true;
			}
		}
		Transform transform = FindModelChild("MuzzleRight");
		if ((bool)transform && (bool)chargeEffectPrefab)
		{
			chargeInstance = Object.Instantiate(chargeEffectPrefab, transform.position, transform.rotation);
			chargeInstance.transform.parent = transform;
			ScaleParticleSystemDuration component2 = chargeInstance.GetComponent<ScaleParticleSystemDuration>();
			if ((bool)component2)
			{
				component2.newDuration = duration / 2.8f;
			}
		}
	}

	public override void OnExit()
	{
		if ((bool)chargeInstance)
		{
			EntityState.Destroy(chargeInstance);
		}
		PlayAnimation("FullBody Override", BufferEmptyStateHash);
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if ((bool)modelAnimator && modelAnimator.GetFloat("fist.hitBoxActive") > 0.5f && !hasAttacked)
		{
			if ((bool)chargeInstance)
			{
				EntityState.Destroy(chargeInstance);
			}
			EffectManager.SimpleMuzzleFlash(slamImpactEffect, base.gameObject, muzzleString, transmit: false);
			if (NetworkServer.active && (bool)base.healthComponent)
			{
				DamageInfo damageInfo = new DamageInfo();
				damageInfo.damage = base.healthComponent.combinedHealth * healthCostFraction;
				damageInfo.position = base.characterBody.corePosition;
				damageInfo.force = Vector3.zero;
				damageInfo.damageColorIndex = DamageColorIndex.Default;
				damageInfo.crit = false;
				damageInfo.attacker = null;
				damageInfo.inflictor = null;
				damageInfo.damageType = DamageType.NonLethal | DamageType.BypassArmor;
				damageInfo.procCoefficient = 0f;
				damageInfo.procChainMask = default(ProcChainMask);
				base.healthComponent.TakeDamage(damageInfo);
			}
			if (base.isAuthority)
			{
				if ((bool)modelTransform)
				{
					Transform transform = FindModelChild(muzzleString);
					if ((bool)transform)
					{
						attack = new BlastAttack();
						attack.attacker = base.gameObject;
						attack.inflictor = base.gameObject;
						attack.teamIndex = TeamComponent.GetObjectTeam(base.gameObject);
						attack.baseDamage = damageStat * damageCoefficient;
						attack.baseForce = forceMagnitude;
						attack.position = transform.position;
						attack.radius = radius;
						attack.bonusForce = new Vector3(0f, upwardForce, 0f);
						attack.Fire();
					}
				}
				float num = 360f / (float)waveProjectileCount;
				Vector3 vector = Vector3.ProjectOnPlane(base.inputBank.aimDirection, Vector3.up);
				Vector3 footPosition = base.characterBody.footPosition;
				for (int i = 0; i < waveProjectileCount; i++)
				{
					Vector3 forward = Quaternion.AngleAxis(num * (float)i, Vector3.up) * vector;
					ProjectileManager.instance.FireProjectile(waveProjectilePrefab, footPosition, Util.QuaternionSafeLookRotation(forward), base.gameObject, base.characterBody.damage * waveProjectileDamageCoefficient, waveProjectileForce, Util.CheckRoll(base.characterBody.crit, base.characterBody.master));
				}
			}
			hasAttacked = true;
		}
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}
}
