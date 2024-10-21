using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.Huntress.HuntressWeapon;

public class FireArrow : BaseState
{
	public static GameObject effectPrefab;

	public static GameObject hitEffectPrefab;

	public static GameObject projectilePrefab;

	public static int arrowCountMax = 1;

	public float damageCoefficient;

	public static float fireDuration = 1f;

	public static float baseDuration = 2f;

	public static float arcAngle = 5f;

	public static float recoilAmplitude = 1f;

	public static string attackSoundString;

	public static float spreadBloomValue = 0.3f;

	public static float smallHopStrength;

	private Ray aimRay;

	private Transform modelTransform;

	private float duration;

	private float fireTimer;

	private int grenadeCount;

	private static int fireArrowHash = Animator.StringToHash("FireArrow");

	private static int fireArrowParamHash = Animator.StringToHash("FireArrow.playbackRate");

	private void FireGrenade(string targetMuzzle)
	{
		Util.PlaySound(attackSoundString, base.gameObject);
		aimRay = GetAimRay();
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
			Vector3 axis = Vector3.Cross(up, aimRay.direction);
			Vector3 vector = Quaternion.Euler(0f, 0f, z) * (Quaternion.Euler(x, 0f, 0f) * Vector3.forward);
			float y = vector.y;
			vector.y = 0f;
			float angle = Mathf.Atan2(vector.z, vector.x) * 57.29578f - 90f;
			float angle2 = Mathf.Atan2(y, vector.magnitude) * 57.29578f + arcAngle;
			Vector3 forward = Quaternion.AngleAxis(angle, up) * (Quaternion.AngleAxis(angle2, axis) * aimRay.direction);
			ProjectileManager.instance.FireProjectile(projectilePrefab, aimRay.origin, Util.QuaternionSafeLookRotation(forward), base.gameObject, damageStat * damageCoefficient, 0f, Util.CheckRoll(critStat, base.characterBody.master));
		}
		base.characterBody.AddSpreadBloom(spreadBloomValue);
	}

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		modelTransform = GetModelTransform();
		if ((bool)base.characterMotor && smallHopStrength != 0f)
		{
			base.characterMotor.velocity.y = smallHopStrength;
		}
		if ((bool)base.characterBody)
		{
			base.characterBody.SetAimTimer(2f);
		}
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority)
		{
			fireTimer -= GetDeltaTime();
			float num = fireDuration / attackSpeedStat / (float)arrowCountMax;
			if (fireTimer <= 0f && grenadeCount < arrowCountMax)
			{
				PlayAnimation("Gesture, Additive", fireArrowHash, fireArrowParamHash, duration - num);
				PlayAnimation("Gesture, Override", fireArrowHash, fireArrowParamHash, duration - num);
				FireGrenade("Muzzle");
				fireTimer += num;
				grenadeCount++;
			}
			if (base.fixedAge >= duration)
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
