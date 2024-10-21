using RoR2.Projectile;
using UnityEngine;

namespace RoR2.Items;

public class SummonedEchoBodyBehavior : BaseItemBodyBehavior
{
	private float fireTimer;

	private float fireInterval = 3f;

	private float damageCoefficient = 3f;

	private static ItemDef GetItemDef()
	{
		return RoR2Content.Items.SummonedEcho;
	}

	private void FixedUpdate()
	{
		fireTimer -= Time.fixedDeltaTime;
		if (fireTimer <= 0f)
		{
			fireTimer = fireInterval;
			FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
			fireProjectileInfo.crit = false;
			fireProjectileInfo.damage = base.body.damage * damageCoefficient;
			fireProjectileInfo.damageColorIndex = DamageColorIndex.Default;
			fireProjectileInfo.damageTypeOverride = DamageType.SlowOnHit;
			fireProjectileInfo.owner = base.body.gameObject;
			fireProjectileInfo.position = base.body.aimOrigin;
			fireProjectileInfo.rotation = Quaternion.LookRotation(Vector3.up);
			fireProjectileInfo.procChainMask = default(ProcChainMask);
			fireProjectileInfo.projectilePrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/EchoHunterProjectile");
			fireProjectileInfo.force = 400f;
			fireProjectileInfo.target = null;
			FireProjectileInfo fireProjectileInfo2 = fireProjectileInfo;
			ProjectileManager.instance.FireProjectile(fireProjectileInfo2);
		}
	}
}
