using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.GravekeeperMonster.Weapon;

public class GravekeeperBarrage : BaseState
{
	private float stopwatch;

	private float missileStopwatch;

	public static float baseDuration;

	public static string muzzleString;

	public static float missileSpawnFrequency;

	public static float missileSpawnDelay;

	public static float missileForce;

	public static float damageCoefficient;

	public static float maxSpread;

	public static GameObject projectilePrefab;

	public static GameObject muzzleflashPrefab;

	public static string jarEffectChildLocatorString;

	public static string jarOpenSoundString;

	public static string jarCloseSoundString;

	public static GameObject jarOpenEffectPrefab;

	public static GameObject jarCloseEffectPrefab;

	private ChildLocator childLocator;

	private static int BeginGravekeeperBarrageStateHash = Animator.StringToHash("BeginGravekeeperBarrage");

	private static int EndGravekeeperBarrageStateHash = Animator.StringToHash("EndGravekeeperBarrage");

	public override void OnEnter()
	{
		base.OnEnter();
		missileStopwatch -= missileSpawnDelay;
		Transform modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			childLocator = modelTransform.GetComponent<ChildLocator>();
			if ((bool)childLocator)
			{
				childLocator.FindChild("JarEffectLoop").gameObject.SetActive(value: true);
			}
		}
		PlayAnimation("Jar, Override", BeginGravekeeperBarrageStateHash);
		EffectManager.SimpleMuzzleFlash(jarOpenEffectPrefab, base.gameObject, jarEffectChildLocatorString, transmit: false);
		Util.PlaySound(jarOpenSoundString, base.gameObject);
		base.characterBody.SetAimTimer(baseDuration + 2f);
	}

	private void FireBlob(Ray projectileRay, float bonusPitch, float bonusYaw)
	{
		projectileRay.direction = Util.ApplySpread(projectileRay.direction, 0f, maxSpread, 1f, 1f, bonusYaw, bonusPitch);
		EffectManager.SimpleMuzzleFlash(muzzleflashPrefab, base.gameObject, muzzleString, transmit: false);
		if (NetworkServer.active)
		{
			ProjectileManager.instance.FireProjectile(projectilePrefab, projectileRay.origin, Util.QuaternionSafeLookRotation(projectileRay.direction), base.gameObject, damageStat * damageCoefficient, missileForce, Util.CheckRoll(critStat, base.characterBody.master));
		}
	}

	public override void OnExit()
	{
		PlayCrossfade("Jar, Override", EndGravekeeperBarrageStateHash, 0.06f);
		EffectManager.SimpleMuzzleFlash(jarCloseEffectPrefab, base.gameObject, jarEffectChildLocatorString, transmit: false);
		Util.PlaySound(jarCloseSoundString, base.gameObject);
		if ((bool)childLocator)
		{
			childLocator.FindChild("JarEffectLoop").gameObject.SetActive(value: false);
		}
		base.OnExit();
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
