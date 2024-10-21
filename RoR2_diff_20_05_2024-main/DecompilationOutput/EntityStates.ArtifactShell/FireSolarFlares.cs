using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.ArtifactShell;

public class FireSolarFlares : BaseState
{
	public static GameObject projectilePrefab;

	public static int minimumProjectileCount;

	public static int maximumProjectileCount;

	public static float arc;

	public static float baseDuration;

	public static float radius;

	public static float projectileDamageCoefficient;

	public static float projectileForce;

	public static float projectileFuse;

	public static float projectileSpeed;

	private float duration;

	private int projectileCount;

	private int projectilesFired;

	private Quaternion currentRotation;

	private Quaternion deltaRotation;

	public override void OnEnter()
	{
		base.OnEnter();
		if ((bool)base.healthComponent)
		{
			projectileCount = (int)Util.Remap(base.healthComponent.combinedHealthFraction, 0f, 1f, maximumProjectileCount, minimumProjectileCount);
		}
		if (NetworkServer.active)
		{
			Vector3 aimDirection = base.inputBank.aimDirection;
			currentRotation = Quaternion.LookRotation(aimDirection);
			_ = Run.FixedTimeStamp.now.t * 2f % 1f;
			Vector3 rhs = currentRotation * Vector3.forward;
			Vector3 lhs = currentRotation * Vector3.right;
			_ = currentRotation * Vector3.up;
			deltaRotation = Quaternion.AngleAxis(arc / (float)projectileCount, Vector3.Cross(lhs, rhs));
		}
		duration = baseDuration / attackSpeedStat;
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (NetworkServer.active)
		{
			float num = duration / (float)projectileCount;
			if (base.fixedAge >= (float)projectilesFired * num)
			{
				projectilesFired++;
				FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
				fireProjectileInfo.owner = base.gameObject;
				fireProjectileInfo.position = base.transform.position + currentRotation * Vector3.forward * radius;
				fireProjectileInfo.rotation = currentRotation;
				fireProjectileInfo.projectilePrefab = projectilePrefab;
				fireProjectileInfo.fuseOverride = projectileFuse;
				fireProjectileInfo.useFuseOverride = true;
				fireProjectileInfo.speedOverride = projectileSpeed;
				fireProjectileInfo.useSpeedOverride = true;
				fireProjectileInfo.damage = damageStat * projectileDamageCoefficient;
				fireProjectileInfo.force = projectileForce;
				ProjectileManager.instance.FireProjectile(fireProjectileInfo);
				currentRotation *= deltaRotation;
			}
			if (base.fixedAge >= duration)
			{
				outer.SetNextStateToMain();
			}
		}
	}
}
