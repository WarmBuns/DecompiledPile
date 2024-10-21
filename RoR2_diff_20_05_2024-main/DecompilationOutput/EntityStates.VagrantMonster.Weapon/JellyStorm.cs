using System;
using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.VagrantMonster.Weapon;

public class JellyStorm : BaseState
{
	private float stopwatch;

	private float missileStopwatch;

	public static float stormDuration;

	public static float stormToIdleTransitionDuration;

	public static string stormPointChildString;

	public static float missileSpawnFrequency;

	public static float missileSpawnDelay;

	public static int missileTurretCount;

	public static float missileTurretYawFrequency;

	public static float missileTurretPitchFrequency;

	public static float missileTurretPitchMagnitude;

	public static float missileSpeed;

	public static float damageCoefficient;

	public static GameObject projectilePrefab;

	public static GameObject effectPrefab;

	private bool beginExitTransition;

	private ChildLocator childLocator;

	private static int StormEnterStateHash = Animator.StringToHash("StormEnter");

	public override void OnEnter()
	{
		base.OnEnter();
		missileStopwatch -= missileSpawnDelay;
		if ((bool)base.sfxLocator && base.sfxLocator.barkSound != "")
		{
			Util.PlaySound(base.sfxLocator.barkSound, base.gameObject);
		}
		PlayAnimation("Gesture", StormEnterStateHash);
		Transform modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			childLocator = modelTransform.GetComponent<ChildLocator>();
			if ((bool)childLocator)
			{
				_ = (bool)childLocator.FindChild(stormPointChildString);
			}
		}
	}

	private void FireBlob(Ray aimRay, float bonusPitch, float bonusYaw, float speed)
	{
		Vector3 forward = Util.ApplySpread(aimRay.direction, 0f, 0f, 1f, 1f, bonusYaw, bonusPitch);
		ProjectileManager.instance.FireProjectile(projectilePrefab, aimRay.origin, Util.QuaternionSafeLookRotation(forward), base.gameObject, damageStat * damageCoefficient, 0f, Util.CheckRoll(critStat, base.characterBody.master), DamageColorIndex.Default, null, speed);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		float deltaTime = GetDeltaTime();
		stopwatch += deltaTime;
		missileStopwatch += deltaTime;
		if (missileStopwatch >= 1f / missileSpawnFrequency && !beginExitTransition)
		{
			missileStopwatch -= 1f / missileSpawnFrequency;
			Transform transform = childLocator.FindChild(stormPointChildString);
			if ((bool)transform)
			{
				for (int i = 0; i < missileTurretCount; i++)
				{
					float bonusYaw = 360f / (float)missileTurretCount * (float)i + 360f * missileTurretYawFrequency * stopwatch;
					float bonusPitch = Mathf.Sin(MathF.PI * 2f * missileTurretPitchFrequency * stopwatch) * missileTurretPitchMagnitude;
					Ray aimRay = default(Ray);
					aimRay.origin = transform.position;
					aimRay.direction = transform.transform.forward;
					FireBlob(aimRay, bonusPitch, bonusYaw, missileSpeed);
				}
			}
		}
		if (stopwatch >= stormDuration - stormToIdleTransitionDuration && !beginExitTransition)
		{
			beginExitTransition = true;
			PlayCrossfade("Gesture", "StormExit", "StormExit.playbackRate", stormToIdleTransitionDuration, 0.5f);
		}
		if (stopwatch >= stormDuration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}
}
