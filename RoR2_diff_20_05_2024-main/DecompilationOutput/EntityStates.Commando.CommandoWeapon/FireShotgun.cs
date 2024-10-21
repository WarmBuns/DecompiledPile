using RoR2;
using UnityEngine;

namespace EntityStates.Commando.CommandoWeapon;

public class FireShotgun : BaseState
{
	public static GameObject effectPrefab;

	public static GameObject hitEffectPrefab;

	public static GameObject tracerEffectPrefab;

	public static float damageCoefficient;

	public static float force;

	public static int bulletCount;

	public static float baseMaxDuration = 2f;

	public static float baseMinDuration = 0.5f;

	public static string attackSoundString;

	public static float recoilAmplitude;

	public static float spreadBloomValue = 0.3f;

	private float maxDuration;

	private float minDuration;

	private static int FireShotgunStateHash = Animator.StringToHash("FireShotgun");

	private static int FireShotgunParamHash = Animator.StringToHash("FireShotgun.playbackRate");

	private bool buttonReleased;

	public override void OnEnter()
	{
		base.OnEnter();
		AddRecoil(-1f * recoilAmplitude, -2f * recoilAmplitude, -0.5f * recoilAmplitude, 0.5f * recoilAmplitude);
		maxDuration = baseMaxDuration / attackSpeedStat;
		minDuration = baseMinDuration / attackSpeedStat;
		Ray aimRay = GetAimRay();
		StartAimMode(aimRay);
		Util.PlaySound(attackSoundString, base.gameObject);
		PlayAnimation("Gesture, Additive", FireShotgunStateHash, FireShotgunParamHash, maxDuration * 1.1f);
		PlayAnimation("Gesture, Override", FireShotgunStateHash, FireShotgunParamHash, maxDuration * 1.1f);
		string muzzleName = "MuzzleShotgun";
		if ((bool)effectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(effectPrefab, base.gameObject, muzzleName, transmit: false);
		}
		if (base.isAuthority)
		{
			BulletAttack bulletAttack = new BulletAttack();
			bulletAttack.owner = base.gameObject;
			bulletAttack.weapon = base.gameObject;
			bulletAttack.origin = aimRay.origin;
			bulletAttack.aimVector = aimRay.direction;
			bulletAttack.minSpread = 0f;
			bulletAttack.maxSpread = base.characterBody.spreadBloomAngle;
			bulletAttack.bulletCount = ((bulletCount > 0) ? ((uint)bulletCount) : 0u);
			bulletAttack.procCoefficient = 1f / (float)bulletCount;
			bulletAttack.damage = damageCoefficient * damageStat / (float)bulletCount;
			bulletAttack.force = force;
			bulletAttack.falloffModel = BulletAttack.FalloffModel.DefaultBullet;
			bulletAttack.tracerEffectPrefab = tracerEffectPrefab;
			bulletAttack.muzzleName = muzzleName;
			bulletAttack.hitEffectPrefab = hitEffectPrefab;
			bulletAttack.isCrit = Util.CheckRoll(critStat, base.characterBody.master);
			bulletAttack.HitEffectNormal = false;
			bulletAttack.radius = 0f;
			bulletAttack.Fire();
		}
		base.characterBody.AddSpreadBloom(spreadBloomValue);
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		buttonReleased |= !base.inputBank.skill1.down;
		if (base.fixedAge >= maxDuration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		if (buttonReleased && base.fixedAge >= minDuration)
		{
			return InterruptPriority.Any;
		}
		return InterruptPriority.Skill;
	}
}
