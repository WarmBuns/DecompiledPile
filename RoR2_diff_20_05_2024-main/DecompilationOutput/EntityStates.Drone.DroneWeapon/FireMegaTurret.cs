using RoR2;
using UnityEngine;

namespace EntityStates.Drone.DroneWeapon;

public class FireMegaTurret : BaseState
{
	public static GameObject effectPrefab;

	public static GameObject hitEffectPrefab;

	public static GameObject tracerEffectPrefab;

	public static string attackSoundString;

	public static float attackSoundPlaybackCoefficient;

	public static float damageCoefficient;

	public static float force;

	public static float minSpread;

	public static float maxSpread;

	public static int maxBulletCount;

	public static float baseTotalDuration;

	private Transform modelTransform;

	private ChildLocator childLocator;

	private float fireStopwatch;

	private float stopwatch;

	private float durationBetweenShots;

	private float totalDuration;

	private int bulletCount;

	private static int FireGatStateHash = Animator.StringToHash("FireGat");

	public override void OnEnter()
	{
		base.OnEnter();
		fireStopwatch = 0f;
		totalDuration = baseTotalDuration / attackSpeedStat;
		durationBetweenShots = totalDuration / (float)maxBulletCount;
		GetAimRay();
		Transform transform = GetModelTransform();
		if ((bool)transform)
		{
			childLocator = transform.GetComponent<ChildLocator>();
		}
	}

	private void FireBullet(string muzzleString)
	{
		Ray aimRay = GetAimRay();
		_ = aimRay.origin;
		Util.PlayAttackSpeedSound(attackSoundString, base.gameObject, attackSoundPlaybackCoefficient);
		PlayAnimation("Gesture, Additive", FireGatStateHash);
		if ((bool)childLocator)
		{
			Transform transform = childLocator.FindChild(muzzleString);
			if ((bool)transform)
			{
				_ = transform.position;
			}
		}
		if ((bool)effectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(effectPrefab, base.gameObject, muzzleString, transmit: false);
		}
		if (base.isAuthority)
		{
			BulletAttack bulletAttack = new BulletAttack();
			bulletAttack.owner = base.gameObject;
			bulletAttack.weapon = base.gameObject;
			bulletAttack.origin = aimRay.origin;
			bulletAttack.aimVector = aimRay.direction;
			bulletAttack.minSpread = minSpread;
			bulletAttack.maxSpread = maxSpread;
			bulletAttack.damage = damageCoefficient * damageStat;
			bulletAttack.force = force;
			bulletAttack.tracerEffectPrefab = tracerEffectPrefab;
			bulletAttack.muzzleName = muzzleString;
			bulletAttack.hitEffectPrefab = hitEffectPrefab;
			bulletAttack.isCrit = Util.CheckRoll(critStat, base.characterBody.master);
			bulletAttack.Fire();
		}
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		float deltaTime = GetDeltaTime();
		fireStopwatch += deltaTime;
		stopwatch += deltaTime;
		if (fireStopwatch >= durationBetweenShots)
		{
			bulletCount++;
			fireStopwatch -= durationBetweenShots;
			FireBullet((bulletCount % 2 == 0) ? "GatLeft" : "GatRight");
		}
		if (stopwatch >= totalDuration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
