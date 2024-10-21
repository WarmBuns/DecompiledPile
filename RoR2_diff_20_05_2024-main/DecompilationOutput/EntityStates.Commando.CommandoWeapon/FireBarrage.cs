using RoR2;
using UnityEngine;

namespace EntityStates.Commando.CommandoWeapon;

public class FireBarrage : BaseState
{
	public static GameObject effectPrefab;

	public static GameObject hitEffectPrefab;

	public static GameObject tracerEffectPrefab;

	public static float damageCoefficient;

	public static float force;

	public static float minSpread;

	public static float maxSpread;

	public static float baseDurationBetweenShots = 1f;

	public static float totalDuration = 2f;

	public static float bulletRadius = 1.5f;

	public static int baseBulletCount = 1;

	public static string fireBarrageSoundString;

	public static float recoilAmplitude;

	public static float spreadBloomValue;

	private int totalBulletsFired;

	private int bulletCount;

	public float stopwatchBetweenShots;

	private Animator modelAnimator;

	private Transform modelTransform;

	private float duration;

	private float durationBetweenShots;

	private static int FireBarrageStateHash = Animator.StringToHash("FireBarrage");

	private static int FireBarrageParamHash = Animator.StringToHash("FireBarrage.playbackRate");

	private static int FirePistolRightStateHash = Animator.StringToHash("FirePistol, Right");

	public override void OnEnter()
	{
		base.OnEnter();
		base.characterBody.SetSpreadBloom(0.2f, canOnlyIncreaseBloom: false);
		duration = totalDuration;
		durationBetweenShots = baseDurationBetweenShots / attackSpeedStat;
		bulletCount = (int)((float)baseBulletCount * attackSpeedStat);
		modelAnimator = GetModelAnimator();
		modelTransform = GetModelTransform();
		PlayCrossfade("Gesture, Additive", FireBarrageStateHash, FireBarrageParamHash, duration, 0.2f);
		PlayCrossfade("Gesture, Override", FireBarrageStateHash, FireBarrageParamHash, duration, 0.2f);
		if ((bool)base.characterBody)
		{
			base.characterBody.SetAimTimer(2f);
		}
		FireBullet();
	}

	private void FireBullet()
	{
		Ray aimRay = GetAimRay();
		string muzzleName = "MuzzleRight";
		if ((bool)modelAnimator)
		{
			if ((bool)effectPrefab)
			{
				EffectManager.SimpleMuzzleFlash(effectPrefab, base.gameObject, muzzleName, transmit: false);
			}
			PlayAnimation("Gesture Additive, Right", FirePistolRightStateHash);
		}
		AddRecoil(-0.8f * recoilAmplitude, -1f * recoilAmplitude, -0.1f * recoilAmplitude, 0.15f * recoilAmplitude);
		if (base.isAuthority)
		{
			BulletAttack bulletAttack = new BulletAttack();
			bulletAttack.owner = base.gameObject;
			bulletAttack.weapon = base.gameObject;
			bulletAttack.origin = aimRay.origin;
			bulletAttack.aimVector = aimRay.direction;
			bulletAttack.minSpread = minSpread;
			bulletAttack.maxSpread = maxSpread;
			bulletAttack.bulletCount = 1u;
			bulletAttack.damage = damageCoefficient * damageStat;
			bulletAttack.force = force;
			bulletAttack.tracerEffectPrefab = tracerEffectPrefab;
			bulletAttack.muzzleName = muzzleName;
			bulletAttack.hitEffectPrefab = hitEffectPrefab;
			bulletAttack.isCrit = Util.CheckRoll(critStat, base.characterBody.master);
			bulletAttack.radius = bulletRadius;
			bulletAttack.smartCollision = true;
			bulletAttack.damageType = DamageType.Stun1s;
			bulletAttack.Fire();
		}
		base.characterBody.AddSpreadBloom(spreadBloomValue);
		totalBulletsFired++;
		Util.PlaySound(fireBarrageSoundString, base.gameObject);
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		stopwatchBetweenShots += GetDeltaTime();
		if (stopwatchBetweenShots >= durationBetweenShots && totalBulletsFired < bulletCount)
		{
			stopwatchBetweenShots -= durationBetweenShots;
			FireBullet();
		}
		if (base.fixedAge >= duration && totalBulletsFired == bulletCount && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
