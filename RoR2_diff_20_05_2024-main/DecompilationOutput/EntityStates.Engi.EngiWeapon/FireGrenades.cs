using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.Engi.EngiWeapon;

public class FireGrenades : BaseState
{
	public static GameObject effectPrefab;

	public static GameObject projectilePrefab;

	public int grenadeCountMax = 3;

	public static float damageCoefficient;

	public static float fireDuration = 1f;

	public static float baseDuration = 2f;

	public static float arcAngle = 5f;

	public static float recoilAmplitude = 1f;

	public static string attackSoundString;

	public static float spreadBloomValue = 0.3f;

	private Ray projectileRay;

	private Transform modelTransform;

	private float duration;

	private float fireTimer;

	private int grenadeCount;

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
			float x = Random.Range(0f, base.characterBody.spreadBloomAngle);
			float z = Random.Range(0f, 360f);
			Vector3 up = Vector3.up;
			Vector3 axis = Vector3.Cross(up, projectileRay.direction);
			Vector3 vector = Quaternion.Euler(0f, 0f, z) * (Quaternion.Euler(x, 0f, 0f) * Vector3.forward);
			float y = vector.y;
			vector.y = 0f;
			float angle = Mathf.Atan2(vector.z, vector.x) * 57.29578f - 90f;
			float angle2 = Mathf.Atan2(y, vector.magnitude) * 57.29578f + arcAngle;
			Vector3 forward = Quaternion.AngleAxis(angle, up) * (Quaternion.AngleAxis(angle2, axis) * projectileRay.direction);
			ProjectileManager.instance.FireProjectile(projectilePrefab, projectileRay.origin, Util.QuaternionSafeLookRotation(forward), base.gameObject, damageStat * damageCoefficient, 0f, Util.CheckRoll(critStat, base.characterBody.master));
		}
		base.characterBody.AddSpreadBloom(spreadBloomValue);
	}

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		modelTransform = GetModelTransform();
		StartAimMode();
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		fireTimer -= GetDeltaTime();
		float num = fireDuration / attackSpeedStat / (float)grenadeCountMax;
		if (fireTimer <= 0f && grenadeCount < grenadeCountMax)
		{
			fireTimer += num;
			if (grenadeCount % 2 == 0)
			{
				FireGrenade("MuzzleLeft");
				PlayCrossfade("Gesture Left Cannon, Additive", "FireGrenadeLeft", 0.1f);
			}
			else
			{
				FireGrenade("MuzzleRight");
				PlayCrossfade("Gesture Right Cannon, Additive", "FireGrenadeRight", 0.1f);
			}
			grenadeCount++;
		}
		if (base.isAuthority && base.fixedAge >= duration)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
