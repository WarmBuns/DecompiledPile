using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.Mage.Weapon;

public class FireIceOrb : BaseState
{
	public enum Gauntlet
	{
		Left,
		Right
	}

	public static GameObject projectilePrefab;

	public static GameObject effectPrefab;

	public static float baseDuration = 2f;

	public static float damageCoefficient = 1.2f;

	public static string attackString;

	private float duration;

	private bool hasFiredGauntlet;

	private string muzzleString;

	private Animator animator;

	public Gauntlet gauntlet;

	private static int FireGauntletLeftStateHash = Animator.StringToHash("FireGauntletLeft");

	private static int FireGauntletRightStateHash = Animator.StringToHash("FireGauntletRight");

	private static int HoldGauntletsUpStateHash = Animator.StringToHash("HoldGauntletsUp");

	private static int FireGauntletParamHash = Animator.StringToHash("FireGauntlet.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		switch (gauntlet)
		{
		case Gauntlet.Left:
			muzzleString = "MuzzleLeft";
			PlayAnimation("Gesture Left, Additive", FireGauntletLeftStateHash, FireGauntletParamHash, duration);
			break;
		case Gauntlet.Right:
			muzzleString = "MuzzleRight";
			PlayAnimation("Gesture Right, Additive", FireGauntletRightStateHash, FireGauntletParamHash, duration);
			break;
		}
		PlayAnimation("Gesture, Additive", HoldGauntletsUpStateHash, FireGauntletParamHash, duration);
		Util.PlaySound(attackString, base.gameObject);
		animator = GetModelAnimator();
		base.characterBody.SetAimTimer(2f);
		FireGauntlet();
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	private void FireGauntlet()
	{
		hasFiredGauntlet = true;
		Ray aimRay = GetAimRay();
		if ((bool)effectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(effectPrefab, base.gameObject, muzzleString, transmit: false);
		}
		if (base.isAuthority)
		{
			ProjectileManager.instance.FireProjectile(projectilePrefab, aimRay.origin, Util.QuaternionSafeLookRotation(aimRay.direction), base.gameObject, damageStat * damageCoefficient, 0f, Util.CheckRoll(critStat, base.characterBody.master));
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (animator.GetFloat("FireGauntlet.fire") > 0f && !hasFiredGauntlet)
		{
			FireGauntlet();
		}
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
