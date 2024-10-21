using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.Drone.DroneWeapon;

public class FireMissileBarrage : BaseState
{
	public static GameObject effectPrefab;

	public static GameObject projectilePrefab;

	public static float damageCoefficient = 1f;

	public static float baseFireInterval = 0.1f;

	public static float minSpread = 0f;

	public static float maxSpread = 5f;

	public static int maxMissileCount;

	private float fireTimer;

	private float fireInterval;

	private Transform modelTransform;

	private AimAnimator aimAnimator;

	private int missileCount;

	private static int FireMissileStateHash = Animator.StringToHash("FireMissile");

	private void FireMissile(string targetMuzzle)
	{
		missileCount++;
		PlayAnimation("Gesture, Additive", FireMissileStateHash);
		Ray aimRay = GetAimRay();
		if ((bool)modelTransform)
		{
			ChildLocator component = modelTransform.GetComponent<ChildLocator>();
			if ((bool)component)
			{
				Transform transform = component.FindChild(targetMuzzle);
				if ((bool)transform)
				{
					aimRay.origin = transform.position;
				}
			}
		}
		if ((bool)effectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(effectPrefab, base.gameObject, targetMuzzle, transmit: false);
		}
		if ((bool)base.characterBody)
		{
			base.characterBody.SetAimTimer(2f);
		}
		if (base.isAuthority)
		{
			float x = Random.Range(minSpread, maxSpread);
			float z = Random.Range(0f, 360f);
			Vector3 up = Vector3.up;
			Vector3 axis = Vector3.Cross(up, aimRay.direction);
			Vector3 vector = Quaternion.Euler(0f, 0f, z) * (Quaternion.Euler(x, 0f, 0f) * Vector3.forward);
			float y = vector.y;
			vector.y = 0f;
			float angle = Mathf.Atan2(vector.z, vector.x) * 57.29578f - 90f;
			float angle2 = Mathf.Atan2(y, vector.magnitude) * 57.29578f;
			Vector3 forward = Quaternion.AngleAxis(angle, up) * (Quaternion.AngleAxis(angle2, axis) * aimRay.direction);
			ProjectileManager.instance.FireProjectile(projectilePrefab, aimRay.origin, Util.QuaternionSafeLookRotation(forward), base.gameObject, damageStat * damageCoefficient, 0f, Util.CheckRoll(critStat, base.characterBody.master));
		}
	}

	public override void OnEnter()
	{
		base.OnEnter();
		modelTransform = GetModelTransform();
		fireInterval = baseFireInterval / attackSpeedStat;
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		fireTimer -= GetDeltaTime();
		if (fireTimer <= 0f)
		{
			FireMissile("Muzzle");
			fireTimer += fireInterval;
		}
		if (missileCount >= maxMissileCount && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
