using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.TitanMonster;

public class FireGoldMegaLaser : FireMegaLaser
{
	public static GameObject projectilePrefab;

	public static float projectileFireFrequency;

	public static float projectileDamageCoefficient;

	public static float projectileMinSpread;

	public static float projectileMaxSpread;

	private float projectileStopwatch;

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (!base.isAuthority)
		{
			return;
		}
		projectileStopwatch += GetDeltaTime() * attackSpeedStat;
		if (projectileStopwatch >= 1f / projectileFireFrequency)
		{
			Ray ray = GetAimRay();
			if ((bool)muzzleTransform)
			{
				ray.origin = muzzleTransform.transform.position;
			}
			ray.direction = Util.ApplySpread(ray.direction, projectileMinSpread, projectileMaxSpread, 1f, 1f);
			projectileStopwatch -= 1f / projectileFireFrequency;
			ProjectileManager.instance.FireProjectile(projectilePrefab, ray.origin, Util.QuaternionSafeLookRotation(ray.direction), base.gameObject, damageStat * FireMegaLaser.damageCoefficient, 0f, Util.CheckRoll(critStat, base.characterBody.master));
		}
	}
}
