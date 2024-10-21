using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.Pot.PotWeapon;

public class FireCannon : BaseState
{
	public static GameObject effectPrefab;

	public static GameObject hitEffectPrefab;

	public static GameObject projectilePrefab;

	public static float selfForce = 1000f;

	public static int grenadeCountMax = 3;

	public static float damageCoefficient;

	public static float fireDuration = 1f;

	public static float baseDuration = 2f;

	public static float minSpread = 0f;

	public static float maxSpread = 5f;

	public static float arcAngle = 5f;

	private Ray aimRay;

	private Transform modelTransform;

	private float duration;

	private float fireTimer;

	private int grenadeCount;

	private void FireBullet(string targetMuzzle)
	{
		aimRay = GetAimRay();
		if ((bool)modelTransform)
		{
			ChildLocator component = modelTransform.GetComponent<ChildLocator>();
			if ((bool)component)
			{
				Transform transform = component.FindChild(targetMuzzle);
				if ((bool)transform)
				{
					base.rigidbody.AddForceAtPosition(transform.forward * selfForce, transform.position, ForceMode.Impulse);
				}
			}
		}
		if ((bool)effectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(effectPrefab, base.gameObject, targetMuzzle, transmit: false);
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
			float angle2 = Mathf.Atan2(y, vector.magnitude) * 57.29578f + arcAngle;
			Vector3 forward = Quaternion.AngleAxis(angle, up) * (Quaternion.AngleAxis(angle2, axis) * aimRay.direction);
			ProjectileManager.instance.FireProjectile(projectilePrefab, aimRay.origin, Util.QuaternionSafeLookRotation(forward), base.gameObject, damageStat * damageCoefficient, 0f, Util.CheckRoll(critStat, base.characterBody.master));
		}
	}

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		modelTransform = GetModelTransform();
		aimRay = GetAimRay();
		StartAimMode(aimRay);
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (!base.isAuthority)
		{
			return;
		}
		fireTimer -= GetDeltaTime();
		float num = fireDuration / attackSpeedStat / (float)grenadeCountMax;
		if (fireTimer <= 0f && grenadeCount < grenadeCountMax)
		{
			fireTimer += num;
			if (grenadeCount % 2 == 0)
			{
				FireBullet("MuzzleLeft");
				PlayCrossfade("Gesture, Left Cannon", "FireGrenadeLeft", 0.1f);
			}
			else
			{
				FireBullet("MuzzleRight");
				PlayCrossfade("Gesture, Right Cannon", "FireGrenadeRight", 0.1f);
			}
			grenadeCount++;
		}
		if (base.fixedAge >= duration)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
