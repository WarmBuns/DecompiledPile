using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Treebot.Weapon;

public class FireMortar2 : BaseState
{
	public static float baseDuration;

	public static GameObject projectilePrefab;

	public static string fireSound;

	public static float maxDistance;

	public static float damageCoefficient;

	public static float force;

	public static string muzzleName;

	public static GameObject muzzleEffect;

	public static float healthCostFraction;

	private float duration;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		EffectManager.SimpleMuzzleFlash(muzzleEffect, base.gameObject, muzzleName, transmit: false);
		Util.PlaySound(fireSound, base.gameObject);
		PlayCrossfade("Gesture, Additive", "FireBomb", 0.1f);
		if (base.isAuthority)
		{
			Fire();
		}
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
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && base.fixedAge >= duration)
		{
			outer.SetNextStateToMain();
		}
	}

	private void Fire()
	{
		RaycastHit hitInfo;
		Vector3 position = ((!base.inputBank.GetAimRaycast(maxDistance, out hitInfo)) ? base.inputBank.GetAimRay().GetPoint(maxDistance) : hitInfo.point);
		FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
		fireProjectileInfo.projectilePrefab = projectilePrefab;
		fireProjectileInfo.position = position;
		fireProjectileInfo.rotation = Quaternion.identity;
		fireProjectileInfo.owner = base.gameObject;
		fireProjectileInfo.damage = damageCoefficient * damageStat;
		fireProjectileInfo.force = force;
		fireProjectileInfo.crit = RollCrit();
		fireProjectileInfo.damageColorIndex = DamageColorIndex.Default;
		fireProjectileInfo.target = null;
		fireProjectileInfo.speedOverride = 0f;
		fireProjectileInfo.fuseOverride = -1f;
		FireProjectileInfo fireProjectileInfo2 = fireProjectileInfo;
		ProjectileManager.instance.FireProjectile(fireProjectileInfo2);
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}
