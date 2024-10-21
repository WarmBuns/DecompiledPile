using RoR2.Projectile;
using UnityEngine;

namespace RoR2;

public static class MissileUtils
{
	public static void FireMissile(Vector3 position, CharacterBody attackerBody, ProcChainMask procChainMask, GameObject victim, float missileDamage, bool isCrit, GameObject projectilePrefab, DamageColorIndex damageColorIndex, bool addMissileProc)
	{
		FireMissile(position, attackerBody, procChainMask, victim, missileDamage, isCrit, projectilePrefab, damageColorIndex, Vector3.up + Random.insideUnitSphere * 0.1f, 200f, addMissileProc);
	}

	public static void FireMissile(Vector3 position, CharacterBody attackerBody, ProcChainMask procChainMask, GameObject victim, float missileDamage, bool isCrit, GameObject projectilePrefab, DamageColorIndex damageColorIndex, Vector3 initialDirection, float force, bool addMissileProc)
	{
		int valueOrDefault = (attackerBody?.inventory?.GetItemCount(DLC1Content.Items.MoreMissile)).GetValueOrDefault();
		float num = Mathf.Max(1f, 1f + 0.5f * (float)(valueOrDefault - 1));
		InputBankTest component = attackerBody.GetComponent<InputBankTest>();
		ProcChainMask procChainMask2 = procChainMask;
		if (addMissileProc)
		{
			procChainMask2.AddProc(ProcType.Missile);
		}
		FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
		fireProjectileInfo.projectilePrefab = projectilePrefab;
		fireProjectileInfo.position = position;
		fireProjectileInfo.rotation = Util.QuaternionSafeLookRotation(initialDirection);
		fireProjectileInfo.procChainMask = procChainMask2;
		fireProjectileInfo.target = victim;
		fireProjectileInfo.owner = attackerBody.gameObject;
		fireProjectileInfo.damage = missileDamage * num;
		fireProjectileInfo.crit = isCrit;
		fireProjectileInfo.force = force;
		fireProjectileInfo.damageColorIndex = damageColorIndex;
		FireProjectileInfo fireProjectileInfo2 = fireProjectileInfo;
		ProjectileManager.instance.FireProjectile(fireProjectileInfo2);
		if (valueOrDefault > 0)
		{
			Vector3 axis = (component ? component.aimDirection : attackerBody.transform.position);
			FireProjectileInfo fireProjectileInfo3 = fireProjectileInfo2;
			fireProjectileInfo3.rotation = Util.QuaternionSafeLookRotation(Quaternion.AngleAxis(45f, axis) * initialDirection);
			ProjectileManager.instance.FireProjectile(fireProjectileInfo3);
			FireProjectileInfo fireProjectileInfo4 = fireProjectileInfo2;
			fireProjectileInfo4.rotation = Util.QuaternionSafeLookRotation(Quaternion.AngleAxis(-45f, axis) * initialDirection);
			ProjectileManager.instance.FireProjectile(fireProjectileInfo4);
		}
	}
}
