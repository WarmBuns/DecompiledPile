using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.VagrantMonster.Weapon;

public class JellyBarrage : BaseState
{
	private float stopwatch;

	private float missileStopwatch;

	public static float baseDuration;

	public static string muzzleString;

	public static float missileSpawnFrequency;

	public static float missileSpawnDelay;

	public static float damageCoefficient;

	public static float maxSpread;

	public static GameObject projectilePrefab;

	public static GameObject muzzleflashPrefab;

	private ChildLocator childLocator;

	public override void OnEnter()
	{
		base.OnEnter();
		missileStopwatch -= missileSpawnDelay;
		if ((bool)base.sfxLocator && base.sfxLocator.barkSound != "")
		{
			Util.PlaySound(base.sfxLocator.barkSound, base.gameObject);
		}
		Transform modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			childLocator = modelTransform.GetComponent<ChildLocator>();
			if ((bool)childLocator)
			{
				_ = (bool)childLocator.FindChild(muzzleString);
			}
		}
	}

	private void FireBlob(Ray projectileRay, float bonusPitch, float bonusYaw)
	{
		projectileRay.direction = Util.ApplySpread(projectileRay.direction, 0f, maxSpread, 1f, 1f, bonusYaw, bonusPitch);
		ProjectileManager.instance.FireProjectile(projectilePrefab, projectileRay.origin, Util.QuaternionSafeLookRotation(projectileRay.direction), base.gameObject, damageStat * damageCoefficient, 0f, Util.CheckRoll(critStat, base.characterBody.master));
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		float deltaTime = GetDeltaTime();
		stopwatch += deltaTime;
		missileStopwatch += deltaTime;
		if (missileStopwatch >= 1f / missileSpawnFrequency)
		{
			missileStopwatch -= 1f / missileSpawnFrequency;
			Transform transform = childLocator.FindChild(muzzleString);
			if ((bool)transform)
			{
				Ray projectileRay = default(Ray);
				projectileRay.origin = transform.position;
				projectileRay.direction = GetAimRay().direction;
				float maxDistance = 1000f;
				if (Physics.Raycast(GetAimRay(), out var hitInfo, maxDistance, LayerIndex.world.mask))
				{
					projectileRay.direction = hitInfo.point - transform.position;
				}
				FireBlob(projectileRay, 0f, 0f);
			}
		}
		if (stopwatch >= baseDuration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}
}
