using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.ClayBoss.ClayBossWeapon;

public class FireBombardment : BaseState
{
	public static GameObject effectPrefab;

	public static GameObject projectilePrefab;

	public int grenadeCountMax = 3;

	public static float damageCoefficient;

	public static float baseTimeBetweenShots = 1f;

	public static float cooldownDuration = 2f;

	public static float arcAngle = 5f;

	public static float recoilAmplitude = 1f;

	public static string shootSoundString;

	public static float spreadBloomValue = 0.3f;

	private Ray aimRay;

	private Transform modelTransform;

	private float duration;

	private float fireTimer;

	private int grenadeCount;

	private float timeBetweenShots;

	private void FireGrenade(string targetMuzzle)
	{
		PlayCrossfade("Gesture, Bombardment", "FireBombardment", 0.1f);
		Util.PlaySound(shootSoundString, base.gameObject);
		aimRay = GetAimRay();
		Vector3 vector = aimRay.origin;
		if ((bool)modelTransform)
		{
			ChildLocator component = modelTransform.GetComponent<ChildLocator>();
			if ((bool)component)
			{
				Transform transform = component.FindChild(targetMuzzle);
				if ((bool)transform)
				{
					vector = transform.position;
				}
			}
		}
		AddRecoil(-1f * recoilAmplitude, -2f * recoilAmplitude, -1f * recoilAmplitude, 1f * recoilAmplitude);
		if ((bool)effectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(effectPrefab, base.gameObject, targetMuzzle, transmit: true);
		}
		if (base.isAuthority)
		{
			float num = -1f;
			if (Util.CharacterRaycast(base.gameObject, aimRay, out var hitInfo, float.PositiveInfinity, (int)LayerIndex.world.mask | (int)LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Ignore))
			{
				Vector3 point = hitInfo.point;
				float velocity = projectilePrefab.GetComponent<ProjectileSimple>().velocity;
				Vector3 vector2 = point - vector;
				Vector2 vector3 = new Vector2(vector2.x, vector2.z);
				float magnitude = vector3.magnitude;
				float y = Trajectory.CalculateInitialYSpeed(magnitude / velocity, vector2.y);
				Vector3 vector4 = new Vector3(vector3.x / magnitude * velocity, y, vector3.y / magnitude * velocity);
				num = vector4.magnitude;
				aimRay.direction = vector4 / num;
			}
			float x = Random.Range(0f, base.characterBody.spreadBloomAngle);
			float z = Random.Range(0f, 360f);
			Vector3 up = Vector3.up;
			Vector3 axis = Vector3.Cross(up, aimRay.direction);
			Vector3 vector5 = Quaternion.Euler(0f, 0f, z) * (Quaternion.Euler(x, 0f, 0f) * Vector3.forward);
			float y2 = vector5.y;
			vector5.y = 0f;
			float angle = Mathf.Atan2(vector5.z, vector5.x) * 57.29578f - 90f;
			float angle2 = Mathf.Atan2(y2, vector5.magnitude) * 57.29578f;
			Vector3 forward = Quaternion.AngleAxis(angle, up) * (Quaternion.AngleAxis(angle2, axis) * aimRay.direction);
			ProjectileManager.instance.FireProjectile(projectilePrefab, vector, Util.QuaternionSafeLookRotation(forward), base.gameObject, damageStat * damageCoefficient, 0f, Util.CheckRoll(critStat, base.characterBody.master), DamageColorIndex.Default, null, num);
		}
		base.characterBody.AddSpreadBloom(spreadBloomValue);
	}

	public override void OnEnter()
	{
		base.OnEnter();
		timeBetweenShots = baseTimeBetweenShots / attackSpeedStat;
		duration = (baseTimeBetweenShots * (float)grenadeCount + cooldownDuration) / attackSpeedStat;
		PlayCrossfade("Gesture, Additive", "BeginBombardment", 0.1f);
		modelTransform = GetModelTransform();
		if ((bool)base.characterBody)
		{
			base.characterBody.SetAimTimer(duration);
		}
	}

	public override void OnExit()
	{
		PlayCrossfade("Gesture, Additive", "EndBombardment", 0.1f);
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority)
		{
			fireTimer -= GetDeltaTime();
			if (fireTimer <= 0f && grenadeCount < grenadeCountMax)
			{
				fireTimer += timeBetweenShots;
				FireGrenade("Muzzle");
				grenadeCount++;
			}
			if (base.fixedAge >= duration && base.isAuthority)
			{
				outer.SetNextStateToMain();
			}
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
