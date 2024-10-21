using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.Drone.DroneWeapon;

public class FireTwinRocket : BaseState
{
	public static GameObject projectilePrefab;

	public static GameObject muzzleEffectPrefab;

	public static float damageCoefficient;

	public static float force;

	public static float baseDuration = 2f;

	private ChildLocator childLocator;

	private float stopwatch;

	private float duration;

	public override void OnEnter()
	{
		base.OnEnter();
		stopwatch = 0f;
		duration = baseDuration / attackSpeedStat;
		GetAimRay();
		Transform modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			childLocator = modelTransform.GetComponent<ChildLocator>();
		}
		FireProjectile("GatLeft");
		FireProjectile("GatRight");
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	private void FireProjectile(string muzzleString)
	{
		GetAimRay();
		Transform transform = childLocator.FindChild(muzzleString);
		if ((bool)muzzleEffectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(muzzleEffectPrefab, base.gameObject, muzzleString, transmit: false);
		}
		if (!base.isAuthority || !(projectilePrefab != null))
		{
			return;
		}
		float maxDistance = 1000f;
		Ray aimRay = GetAimRay();
		Vector3 forward = aimRay.direction;
		Vector3 position = aimRay.origin;
		if ((bool)transform)
		{
			position = transform.position;
			if (Physics.Raycast(aimRay, out var hitInfo, maxDistance, (int)LayerIndex.world.mask | (int)LayerIndex.entityPrecise.mask))
			{
				forward = hitInfo.point - transform.position;
			}
		}
		ProjectileManager.instance.FireProjectile(projectilePrefab, position, Util.QuaternionSafeLookRotation(forward), base.gameObject, damageStat * damageCoefficient, force, Util.CheckRoll(critStat, base.characterBody.master));
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		stopwatch += GetDeltaTime();
		if (stopwatch >= duration / attackSpeedStat && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
