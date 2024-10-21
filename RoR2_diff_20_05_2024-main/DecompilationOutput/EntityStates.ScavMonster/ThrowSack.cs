using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.ScavMonster;

public class ThrowSack : SackBaseState
{
	public static float baseDuration;

	public static string sound;

	public static GameObject effectPrefab;

	public static GameObject projectilePrefab;

	public static float damageCoefficient;

	public static float force;

	public static float minSpread;

	public static float maxSpread;

	public static string attackSoundString;

	public static float projectileVelocity;

	public static float minimumDistance;

	public static float timeToTarget = 3f;

	public static int projectileCount;

	private float duration;

	private static int ThrowSackStateHash = Animator.StringToHash("ThrowSack");

	private static int ThrowSackParamHash = Animator.StringToHash("ThrowSack.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		Util.PlayAttackSpeedSound(sound, base.gameObject, attackSpeedStat);
		PlayAnimation("Body", ThrowSackStateHash, ThrowSackParamHash, duration);
		if ((bool)effectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(effectPrefab, base.gameObject, SackBaseState.muzzleName, transmit: false);
		}
		Fire();
	}

	private void Fire()
	{
		Ray aimRay = GetAimRay();
		Ray ray = aimRay;
		Ray ray2 = aimRay;
		Vector3 point = aimRay.GetPoint(minimumDistance);
		bool flag = false;
		if (Util.CharacterRaycast(base.gameObject, ray, out var hitInfo, 500f, (int)LayerIndex.world.mask | (int)LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Ignore))
		{
			point = hitInfo.point;
			flag = true;
		}
		float magnitude = projectileVelocity;
		if (flag)
		{
			Vector3 vector = point - ray2.origin;
			Vector2 vector2 = new Vector2(vector.x, vector.z);
			float magnitude2 = vector2.magnitude;
			Vector2 vector3 = vector2 / magnitude2;
			if (magnitude2 < minimumDistance)
			{
				magnitude2 = minimumDistance;
			}
			float y = Trajectory.CalculateInitialYSpeed(timeToTarget, vector.y);
			float num = magnitude2 / timeToTarget;
			Vector3 direction = new Vector3(vector3.x * num, y, vector3.y * num);
			magnitude = direction.magnitude;
			ray2.direction = direction;
		}
		for (int i = 0; i < projectileCount; i++)
		{
			Quaternion rotation = Util.QuaternionSafeLookRotation(Util.ApplySpread(ray2.direction, minSpread, maxSpread, 1f, 1f));
			ProjectileManager.instance.FireProjectile(projectilePrefab, ray2.origin, rotation, base.gameObject, damageStat * damageCoefficient, 0f, Util.CheckRoll(critStat, base.characterBody.master), DamageColorIndex.Default, null, magnitude);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}
}
