using RoR2;
using UnityEngine;

namespace EntityStates.LunarGolem;

public class Shell : BaseState
{
	public static float baseDuration;

	public static float buffDuration;

	public static float preShieldAnimDuration;

	public static GameObject preShieldEffect;

	public static string preShieldSoundString;

	public static string shieldActivateSoundString;

	private bool readyToActivate;

	private float duration;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		Util.PlaySound(preShieldSoundString, base.gameObject);
		PlayCrossfade("Gesture, Additive", "PreShield", 0.2f);
		EffectManager.SimpleMuzzleFlash(preShieldEffect, base.gameObject, "Center", transmit: false);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= preShieldAnimDuration && !readyToActivate)
		{
			readyToActivate = true;
			Util.PlaySound(shieldActivateSoundString, base.gameObject);
			base.characterBody.AddTimedBuff(RoR2Content.Buffs.LunarShell, buffDuration);
		}
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override void OnExit()
	{
		base.OnExit();
	}
}
