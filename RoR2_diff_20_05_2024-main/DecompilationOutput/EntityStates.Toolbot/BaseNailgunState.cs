using RoR2;
using UnityEngine;

namespace EntityStates.Toolbot;

public class BaseNailgunState : BaseToolbotPrimarySkillState
{
	public static float damageCoefficient = 0.1f;

	public static float procCoefficient = 1f;

	public static float force = 100f;

	public static float maxDistance = 50f;

	public new static string muzzleName;

	public static GameObject hitEffectPrefab;

	public static float spreadPitchScale = 0.5f;

	public static float spreadYawScale = 1f;

	public static GameObject tracerEffectPrefab;

	public static string fireSoundString;

	public static GameObject muzzleFlashPrefab;

	public static float spreadBloomValue = 0.2f;

	protected float duration;

	protected int fireNumber;

	private static int FireNailgunStateHash = Animator.StringToHash("FireNailgun");

	private static int isFiringNailgunStateHash = Animator.StringToHash("isFiringNailgun");

	private bool _animateNailgunFiring;

	public override string baseMuzzleName => muzzleName;

	protected bool animateNailgunFiring
	{
		get
		{
			return _animateNailgunFiring;
		}
		set
		{
			if (_animateNailgunFiring != value)
			{
				_animateNailgunFiring = value;
				GetModelAnimator().SetBool(isFiringNailgunStateHash, value);
			}
		}
	}

	protected virtual float GetBaseDuration()
	{
		return 0f;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		PullCurrentStats();
	}

	protected void PullCurrentStats()
	{
		attackSpeedStat = base.characterBody.attackSpeed;
		critStat = base.characterBody.crit;
		duration = GetBaseDuration() / attackSpeedStat;
	}

	protected void FireBullet(Ray aimRay, int bulletCount, float spreadPitchScale, float spreadYawScale)
	{
		fireNumber++;
		StartAimMode(aimRay, 3f);
		if (base.isAuthority)
		{
			BulletAttack bulletAttack = new BulletAttack();
			bulletAttack.aimVector = aimRay.direction;
			bulletAttack.origin = aimRay.origin;
			bulletAttack.owner = base.gameObject;
			bulletAttack.weapon = base.gameObject;
			bulletAttack.bulletCount = (uint)bulletCount;
			bulletAttack.damage = damageStat * damageCoefficient;
			bulletAttack.damageColorIndex = DamageColorIndex.Default;
			bulletAttack.damageType = DamageType.Generic;
			bulletAttack.falloffModel = BulletAttack.FalloffModel.DefaultBullet;
			bulletAttack.force = force;
			bulletAttack.HitEffectNormal = false;
			bulletAttack.procChainMask = default(ProcChainMask);
			bulletAttack.procCoefficient = procCoefficient;
			bulletAttack.maxDistance = maxDistance;
			bulletAttack.radius = 0f;
			bulletAttack.isCrit = RollCrit();
			bulletAttack.muzzleName = ((IToolbotPrimarySkillState)this).muzzleName;
			bulletAttack.minSpread = 0f;
			bulletAttack.hitEffectPrefab = hitEffectPrefab;
			bulletAttack.maxSpread = base.characterBody.spreadBloomAngle;
			bulletAttack.smartCollision = false;
			bulletAttack.sniper = false;
			bulletAttack.spreadPitchScale = spreadPitchScale * spreadPitchScale;
			bulletAttack.spreadYawScale = spreadYawScale * spreadYawScale;
			bulletAttack.tracerEffectPrefab = tracerEffectPrefab;
			bulletAttack.allowTrajectoryAimAssist = false;
			bulletAttack.Fire();
		}
		if ((bool)base.characterBody)
		{
			base.characterBody.AddSpreadBloom(spreadBloomValue);
		}
		Util.PlaySound(fireSoundString, base.gameObject);
		EffectManager.SimpleMuzzleFlash(muzzleFlashPrefab, base.gameObject, muzzleName, transmit: false);
		if (!base.isInDualWield)
		{
			PlayAnimation("Gesture, Additive", FireNailgunStateHash);
		}
		else
		{
			BaseToolbotPrimarySkillStateMethods.PlayGenericFireAnim(this, base.gameObject, base.skillLocator, 0.2f);
		}
	}
}
