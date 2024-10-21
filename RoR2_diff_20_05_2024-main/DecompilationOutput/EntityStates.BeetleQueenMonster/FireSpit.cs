using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.BeetleQueenMonster;

public class FireSpit : BaseState
{
	public static GameObject projectilePrefab;

	public static GameObject effectPrefab;

	public static float baseDuration = 2f;

	public static float damageCoefficient = 1.2f;

	public static float force = 20f;

	public static int projectileCount = 3;

	public static float yawSpread = 5f;

	public static float minSpread = 0f;

	public static float maxSpread = 5f;

	public static float arcAngle = 5f;

	public static float projectileHSpeed = 50f;

	private Ray aimRay;

	private float duration;

	public override void OnEnter()
	{
		base.OnEnter();
		string muzzleName = "Mouth";
		duration = baseDuration / attackSpeedStat;
		if ((bool)effectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(effectPrefab, base.gameObject, muzzleName, transmit: false);
		}
		PlayCrossfade("Gesture", "FireSpit", "FireSpit.playbackRate", duration, 0.1f);
		aimRay = GetAimRay();
		float magnitude = projectileHSpeed;
		Ray ray = aimRay;
		ray.origin = aimRay.GetPoint(6f);
		if (Util.CharacterRaycast(base.gameObject, ray, out var hitInfo, float.PositiveInfinity, (int)LayerIndex.world.mask | (int)LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Ignore))
		{
			float num = magnitude;
			Vector3 vector = hitInfo.point - aimRay.origin;
			Vector2 vector2 = new Vector2(vector.x, vector.z);
			float magnitude2 = vector2.magnitude;
			float y = Trajectory.CalculateInitialYSpeed(magnitude2 / num, vector.y);
			Vector3 vector3 = new Vector3(vector2.x / magnitude2 * num, y, vector2.y / magnitude2 * num);
			magnitude = vector3.magnitude;
			aimRay.direction = vector3 / magnitude;
		}
		EffectManager.SimpleMuzzleFlash(effectPrefab, base.gameObject, muzzleName, transmit: false);
		if (base.isAuthority)
		{
			for (int i = 0; i < projectileCount; i++)
			{
				FireBlob(aimRay, 0f, ((float)projectileCount / 2f - (float)i) * yawSpread, magnitude);
			}
		}
	}

	private void FireBlob(Ray aimRay, float bonusPitch, float bonusYaw, float speed)
	{
		Vector3 forward = Util.ApplySpread(aimRay.direction, minSpread, maxSpread, 1f, 1f, bonusYaw, bonusPitch);
		ProjectileManager.instance.FireProjectile(projectilePrefab, aimRay.origin, Util.QuaternionSafeLookRotation(forward), base.gameObject, damageStat * damageCoefficient, 0f, Util.CheckRoll(critStat, base.characterBody.master), DamageColorIndex.Default, null, speed);
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
