using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.LunarWisp;

public class SeekingBomb : BaseState
{
	public static float baseDuration = 3f;

	public static GameObject chargingEffectPrefab;

	public static GameObject projectilePrefab;

	public static string spinUpSoundString;

	public static string fireBombSoundString;

	public static string spinDownSoundString;

	public static float bombDamageCoefficient;

	public static float bombForce;

	public static string muzzleName;

	public float novaRadius;

	private float duration;

	public static float spinUpDuration;

	private bool upToSpeed;

	private GameObject chargeEffectInstance;

	private uint chargeLoopSoundID;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = (baseDuration + spinUpDuration) / attackSpeedStat;
		chargeLoopSoundID = Util.PlaySound(spinUpSoundString, base.gameObject);
		PlayCrossfade("Gesture", "BombStart", 0.2f);
		if ((bool)base.characterBody)
		{
			base.characterBody.SetAimTimer(duration);
		}
	}

	public override void OnExit()
	{
		base.OnExit();
		AkSoundEngine.StopPlayingID(chargeLoopSoundID);
		if ((bool)chargeEffectInstance)
		{
			EntityState.Destroy(chargeEffectInstance);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= spinUpDuration && !upToSpeed)
		{
			upToSpeed = true;
			Transform modelTransform = GetModelTransform();
			if ((bool)modelTransform)
			{
				ChildLocator component = modelTransform.GetComponent<ChildLocator>();
				if ((bool)component)
				{
					Transform transform = component.FindChild(muzzleName);
					if ((bool)transform && (bool)chargingEffectPrefab)
					{
						chargeEffectInstance = Object.Instantiate(chargingEffectPrefab, transform.position, transform.rotation);
						chargeEffectInstance.transform.parent = transform;
						chargeEffectInstance.GetComponent<ScaleParticleSystemDuration>().newDuration = duration;
					}
				}
			}
		}
		if (base.fixedAge >= duration && base.isAuthority)
		{
			FireBomb();
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}

	private void FireBomb()
	{
		Util.PlaySound(fireBombSoundString, base.gameObject);
		Ray aimRay = GetAimRay();
		Transform modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			ChildLocator component = modelTransform.GetComponent<ChildLocator>();
			if ((bool)component)
			{
				aimRay.origin = component.FindChild(muzzleName).transform.position;
			}
		}
		if (base.isAuthority)
		{
			ProjectileManager.instance.FireProjectile(projectilePrefab, aimRay.origin, Util.QuaternionSafeLookRotation(aimRay.direction), base.gameObject, damageStat * bombDamageCoefficient, bombForce, Util.CheckRoll(critStat, base.characterBody.master));
		}
		Util.PlaySound(spinDownSoundString, base.gameObject);
		PlayCrossfade("Gesture", "BombStop", 0.2f);
	}
}
