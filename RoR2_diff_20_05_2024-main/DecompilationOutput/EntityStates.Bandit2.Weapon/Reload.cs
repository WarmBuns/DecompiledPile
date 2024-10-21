using RoR2;
using UnityEngine;

namespace EntityStates.Bandit2.Weapon;

public class Reload : BaseState
{
	public static float enterSoundPitch;

	public static float exitSoundPitch;

	public static string enterSoundString;

	public static string exitSoundString;

	public static GameObject reloadEffectPrefab;

	public static string reloadEffectMuzzleString;

	public static float baseDuration;

	private bool hasGivenStock;

	private static int ReloadSimpleStateHash = Animator.StringToHash("ReloadSimple");

	private static int ReloadStateHash = Animator.StringToHash("Reload");

	private static int ReloadParamHash = Animator.StringToHash("Reload.playbackRate");

	private float duration => baseDuration / attackSpeedStat;

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation("Gesture, Additive", (base.characterBody.isSprinting && (bool)base.characterMotor && base.characterMotor.isGrounded) ? ReloadSimpleStateHash : ReloadStateHash, ReloadParamHash, duration);
		Util.PlayAttackSpeedSound(enterSoundString, base.gameObject, enterSoundPitch);
		EffectManager.SimpleMuzzleFlash(reloadEffectPrefab, base.gameObject, reloadEffectMuzzleString, transmit: false);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration / 2f)
		{
			GiveStock();
		}
		if (base.isAuthority && base.fixedAge >= duration)
		{
			if (base.skillLocator.primary.stock < base.skillLocator.primary.maxStock)
			{
				outer.SetNextState(new Reload());
				return;
			}
			Util.PlayAttackSpeedSound(exitSoundString, base.gameObject, exitSoundPitch);
			outer.SetNextStateToMain();
		}
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	private void GiveStock()
	{
		if (!hasGivenStock)
		{
			base.skillLocator.primary.AddOneStock();
			hasGivenStock = true;
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
