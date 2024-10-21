using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.Chef;

public class Glaze : BaseState
{
	public static GameObject effectPrefab;

	public static GameObject projectilePrefab;

	public static int grenadeCountMax = 3;

	public static float damageCoefficient;

	public static float fireDuration = 3f;

	public static float baseDuration = 2f;

	public static float arcAngle = 7f;

	public static float recoilAmplitude = 1f;

	public static string attackSoundString;

	public static float spreadBloomValue = 0.3f;

	public static float minimumTimeBetweenShots = 0.025f;

	public static float maximumTimeBetweenShots = 0.1f;

	public static float xDeviationSpread = 1f;

	private Ray projectileRay;

	private Transform modelTransform;

	private float duration;

	private float fireTimer;

	private int grenadeCount;

	private int muzzleStringEndNum = 1;

	private void FireGrenade(string targetMuzzle)
	{
		Util.PlaySound(attackSoundString, base.gameObject);
		projectileRay = GetAimRay();
		if ((bool)modelTransform)
		{
			ChildLocator component = modelTransform.GetComponent<ChildLocator>();
			if ((bool)component)
			{
				Transform transform = component.FindChild(targetMuzzle);
				if ((bool)transform)
				{
					projectileRay.origin = transform.position;
				}
			}
		}
		AddRecoil(-1f * recoilAmplitude, -2f * recoilAmplitude, -1f * recoilAmplitude, 1f * recoilAmplitude);
		if ((bool)effectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(effectPrefab, base.gameObject, targetMuzzle, transmit: false);
		}
		if (base.isAuthority)
		{
			float x = Random.Range(0f, base.characterBody.spreadBloomAngle + xDeviationSpread);
			float z = Random.Range(0f, 360f);
			Vector3 up = Vector3.up;
			Vector3 axis = Vector3.Cross(up, projectileRay.direction);
			Vector3 vector = Quaternion.Euler(0f, 0f, z) * (Quaternion.Euler(x, 0f, 0f) * Vector3.forward);
			float y = vector.y;
			vector.y = 0f;
			float angle = Mathf.Atan2(vector.z, vector.x) * 57.29578f - 90f;
			float angle2 = Mathf.Atan2(y, vector.magnitude) * 57.29578f + arcAngle;
			Vector3 forward = Quaternion.AngleAxis(angle, up) * (Quaternion.AngleAxis(angle2, axis) * projectileRay.direction);
			FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
			fireProjectileInfo.projectilePrefab = projectilePrefab;
			fireProjectileInfo.position = projectileRay.origin;
			fireProjectileInfo.rotation = Util.QuaternionSafeLookRotation(forward);
			fireProjectileInfo.owner = base.gameObject;
			fireProjectileInfo.damage = damageStat * damageCoefficient;
			fireProjectileInfo.force = 0f;
			fireProjectileInfo.crit = Util.CheckRoll(critStat, base.characterBody.master);
			fireProjectileInfo.damageTypeOverride = DamageType.WeakOnHit;
			ProjectileManager.instance.FireProjectile(fireProjectileInfo);
		}
		base.characterBody.AddSpreadBloom(spreadBloomValue);
	}

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		modelTransform = GetModelTransform();
		PlayAnimation("Gesture, Override", "ChefGlazeStart", "ChefGlazeStart.playbackRate", duration);
		StartAimMode();
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration)
		{
			fireTimer -= GetDeltaTime();
			float num = Random.Range(minimumTimeBetweenShots, maximumTimeBetweenShots) + fireDuration / attackSpeedStat / (float)grenadeCountMax;
			if (fireTimer <= 0f && grenadeCount < grenadeCountMax)
			{
				fireTimer += num;
				arcAngle = Random.Range(-6, -12);
				PlayCrossfade("Gesture, Additive", "ChefGlazeRecoil", 0.2f);
				FireGrenade("MuzzleGlaze" + muzzleStringEndNum);
				muzzleStringEndNum++;
				if (muzzleStringEndNum > 5)
				{
					muzzleStringEndNum = 1;
				}
				grenadeCount++;
			}
		}
		if (base.isAuthority && grenadeCount >= grenadeCountMax)
		{
			PlayCrossfade("Gesture, Override", "ChefGlazeExit", 0.2f);
			PlayCrossfade("Gesture, Additive", "ChefGlazeExit", 0.2f);
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
