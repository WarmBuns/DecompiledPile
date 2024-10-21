using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.NullifierMonster;

public class FirePortalBomb : BaseState
{
	public static GameObject portalBombProjectileEffect;

	public static GameObject muzzleflashEffectPrefab;

	public static string muzzleString;

	public static int portalBombCount;

	public static float baseDuration;

	public static float maxDistance;

	public static float damageCoefficient;

	public static float procCoefficient;

	public static float randomRadius;

	public static float force;

	public static float minimumDistanceBetweenBombs;

	public Quaternion? startRotation;

	public Quaternion? endRotation;

	private float duration;

	private int bombsFired;

	private float fireTimer;

	private float fireInterval;

	private Vector3 lastBombPosition;

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		StartAimMode(4f);
		if (base.isAuthority)
		{
			fireInterval = duration / (float)portalBombCount;
			fireTimer = 0f;
		}
	}

	private void FireBomb(Ray fireRay)
	{
		if (Physics.Raycast(fireRay, out var hitInfo, maxDistance, LayerIndex.world.mask))
		{
			Vector3 point = hitInfo.point;
			Vector3 vector = point - lastBombPosition;
			if (bombsFired > 0 && vector.sqrMagnitude < minimumDistanceBetweenBombs * minimumDistanceBetweenBombs)
			{
				point += vector.normalized * minimumDistanceBetweenBombs;
			}
			FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
			fireProjectileInfo.projectilePrefab = portalBombProjectileEffect;
			fireProjectileInfo.position = point;
			fireProjectileInfo.rotation = Quaternion.identity;
			fireProjectileInfo.owner = base.gameObject;
			fireProjectileInfo.damage = damageStat * damageCoefficient;
			fireProjectileInfo.force = force;
			fireProjectileInfo.crit = base.characterBody.RollCrit();
			ProjectileManager.instance.FireProjectile(fireProjectileInfo);
			lastBombPosition = point;
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (!base.isAuthority)
		{
			return;
		}
		fireTimer -= GetDeltaTime();
		if (fireTimer <= 0f)
		{
			fireTimer += fireInterval;
			if (startRotation.HasValue && endRotation.HasValue)
			{
				float num = 1f / ((float)portalBombCount - 1f);
				float t = (float)bombsFired * num;
				Ray aimRay = GetAimRay();
				Quaternion quaternion = Quaternion.Slerp(startRotation.Value, endRotation.Value, t);
				aimRay.direction = quaternion * Vector3.forward;
				FireBomb(aimRay);
				EffectManager.SimpleMuzzleFlash(muzzleflashEffectPrefab, base.gameObject, muzzleString, transmit: true);
			}
			bombsFired++;
		}
		if (base.fixedAge >= duration)
		{
			outer.SetNextStateToMain();
		}
	}
}
