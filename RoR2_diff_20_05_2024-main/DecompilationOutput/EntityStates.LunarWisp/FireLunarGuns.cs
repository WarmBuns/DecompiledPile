using RoR2;
using UnityEngine;

namespace EntityStates.LunarWisp;

public class FireLunarGuns : BaseState
{
	public static GameObject muzzleVfxPrefab;

	public static float baseDuration;

	private float duration;

	public static float baseFireInterval;

	public static int baseBulletCount;

	public static float baseDamagePerSecondCoefficient;

	public static float baseForcePerSecond;

	public static float baseProcCoefficientPerSecond;

	public static float bulletMinSpread;

	public static float bulletMaxSpread;

	public static GameObject bulletTracerEffectPrefab;

	public static GameObject bulletHitEffectPrefab;

	public static bool bulletHitEffectNormal;

	public static float bulletMaxDistance;

	public static string fireSound;

	public static string windLoopSound;

	public static string windDownSound;

	public static string shootLoopSound;

	private uint windLoopSoundID;

	private uint shootLoopSoundID;

	private Transform muzzleTransform;

	public Transform muzzleTransformOne;

	public Transform muzzleTransformTwo;

	[SerializeField]
	public string muzzleNameOne;

	[SerializeField]
	public string muzzleNameTwo;

	private GameObject muzzleVFXInstanceOne;

	private GameObject muzzleVFXInstanceTwo;

	private float fireTimer;

	private float baseFireRate;

	private float baseBulletsPerSecond;

	private Run.FixedTimeStamp critEndTime;

	private Run.FixedTimeStamp lastCritCheck;

	protected ref InputBankTest.ButtonState skillButtonState => ref base.inputBank.skill1;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		muzzleTransformOne = FindModelChild(muzzleNameOne);
		muzzleTransformTwo = FindModelChild(muzzleNameTwo);
		if ((bool)muzzleTransformOne && (bool)muzzleTransformTwo && (bool)muzzleVfxPrefab)
		{
			muzzleVFXInstanceOne = Object.Instantiate(muzzleVfxPrefab, muzzleTransformOne.position, muzzleTransformOne.rotation);
			muzzleVFXInstanceOne.transform.parent = muzzleTransformOne;
			muzzleVFXInstanceTwo = Object.Instantiate(muzzleVfxPrefab, muzzleTransformTwo.position, muzzleTransformTwo.rotation);
			muzzleVFXInstanceTwo.transform.parent = muzzleTransformTwo;
		}
		baseFireRate = 1f / baseFireInterval;
		baseBulletsPerSecond = (float)baseBulletCount * baseFireRate;
		critEndTime = Run.FixedTimeStamp.negativeInfinity;
		lastCritCheck = Run.FixedTimeStamp.negativeInfinity;
		windLoopSoundID = Util.PlaySound(windLoopSound, base.gameObject);
		shootLoopSoundID = Util.PlaySound(shootLoopSound, base.gameObject);
	}

	private void UpdateCrits()
	{
		if (lastCritCheck.timeSince >= 1f)
		{
			lastCritCheck = Run.FixedTimeStamp.now;
			if (RollCrit())
			{
				critEndTime = Run.FixedTimeStamp.now + 2f;
			}
		}
	}

	public override void OnExit()
	{
		Util.PlaySound(windDownSound, base.gameObject);
		if ((bool)muzzleVFXInstanceOne)
		{
			EntityState.Destroy(muzzleVFXInstanceOne.gameObject);
			muzzleVFXInstanceOne = null;
		}
		if ((bool)muzzleVFXInstanceTwo)
		{
			EntityState.Destroy(muzzleVFXInstanceTwo.gameObject);
			muzzleVFXInstanceTwo = null;
		}
		AkSoundEngine.StopPlayingID(windLoopSoundID);
		AkSoundEngine.StopPlayingID(shootLoopSoundID);
		PlayCrossfade("Gesture", "MinigunSpinDown", 0.2f);
		base.OnExit();
	}

	private void OnFireShared()
	{
		Util.PlaySound(fireSound, base.gameObject);
		if (base.isAuthority)
		{
			OnFireAuthority();
		}
	}

	private void OnFireAuthority()
	{
		UpdateCrits();
		bool isCrit = !critEndTime.hasPassed;
		float damage = baseDamagePerSecondCoefficient / baseBulletsPerSecond * damageStat;
		float force = baseForcePerSecond / baseBulletsPerSecond;
		float procCoefficient = baseProcCoefficientPerSecond / baseBulletsPerSecond;
		StartAimMode(0.5f);
		Ray aimRay = GetAimRay();
		BulletAttack bulletAttack = new BulletAttack();
		bulletAttack.bulletCount = (uint)baseBulletCount / 2u;
		bulletAttack.aimVector = aimRay.direction;
		bulletAttack.origin = aimRay.origin;
		bulletAttack.damage = damage;
		bulletAttack.damageColorIndex = DamageColorIndex.Default;
		bulletAttack.damageType = DamageType.Generic;
		bulletAttack.falloffModel = BulletAttack.FalloffModel.None;
		bulletAttack.maxDistance = bulletMaxDistance;
		bulletAttack.force = force;
		bulletAttack.hitMask = LayerIndex.CommonMasks.bullet;
		bulletAttack.minSpread = bulletMinSpread;
		bulletAttack.maxSpread = bulletMaxSpread;
		bulletAttack.isCrit = isCrit;
		bulletAttack.owner = base.gameObject;
		bulletAttack.muzzleName = muzzleNameOne;
		bulletAttack.smartCollision = false;
		bulletAttack.procChainMask = default(ProcChainMask);
		bulletAttack.procCoefficient = procCoefficient;
		bulletAttack.radius = 0f;
		bulletAttack.sniper = false;
		bulletAttack.stopperMask = LayerIndex.CommonMasks.bullet;
		bulletAttack.weapon = null;
		bulletAttack.tracerEffectPrefab = bulletTracerEffectPrefab;
		bulletAttack.spreadPitchScale = 1f;
		bulletAttack.spreadYawScale = 1f;
		bulletAttack.queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
		bulletAttack.hitEffectPrefab = bulletHitEffectPrefab;
		bulletAttack.HitEffectNormal = bulletHitEffectNormal;
		bulletAttack.Fire();
		BulletAttack bulletAttack2 = new BulletAttack();
		bulletAttack2.bulletCount = (uint)baseBulletCount / 2u;
		bulletAttack2.aimVector = aimRay.direction;
		bulletAttack2.origin = aimRay.origin;
		bulletAttack2.damage = damage;
		bulletAttack2.damageColorIndex = DamageColorIndex.Default;
		bulletAttack2.damageType = DamageType.Generic;
		bulletAttack2.falloffModel = BulletAttack.FalloffModel.None;
		bulletAttack2.maxDistance = bulletMaxDistance;
		bulletAttack2.force = force;
		bulletAttack2.hitMask = LayerIndex.CommonMasks.bullet;
		bulletAttack2.minSpread = bulletMinSpread;
		bulletAttack2.maxSpread = bulletMaxSpread;
		bulletAttack2.isCrit = isCrit;
		bulletAttack2.owner = base.gameObject;
		bulletAttack2.muzzleName = muzzleNameTwo;
		bulletAttack2.smartCollision = false;
		bulletAttack2.procChainMask = default(ProcChainMask);
		bulletAttack2.procCoefficient = procCoefficient;
		bulletAttack2.radius = 0f;
		bulletAttack2.sniper = false;
		bulletAttack2.stopperMask = LayerIndex.CommonMasks.bullet;
		bulletAttack2.weapon = null;
		bulletAttack2.tracerEffectPrefab = bulletTracerEffectPrefab;
		bulletAttack2.spreadPitchScale = 1f;
		bulletAttack2.spreadYawScale = 1f;
		bulletAttack2.queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
		bulletAttack2.hitEffectPrefab = bulletHitEffectPrefab;
		bulletAttack2.HitEffectNormal = bulletHitEffectNormal;
		bulletAttack2.Fire();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		fireTimer -= GetDeltaTime();
		if (fireTimer <= 0f)
		{
			float num = baseFireInterval / attackSpeedStat;
			fireTimer += num;
			OnFireShared();
		}
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}
