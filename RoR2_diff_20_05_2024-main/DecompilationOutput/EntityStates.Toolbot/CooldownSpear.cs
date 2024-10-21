using RoR2;
using UnityEngine;

namespace EntityStates.Toolbot;

public class CooldownSpear : BaseToolbotPrimarySkillState
{
	public static float baseDuration;

	public static string enterSoundString;

	private float duration;

	private uint soundID;

	private static int CooldownSpearStateHash = Animator.StringToHash("CooldownSpear");

	private static int CooldownSpearParamHash = Animator.StringToHash("CooldownSpear.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		if (!base.isInDualWield)
		{
			PlayAnimation("Gesture, Additive", CooldownSpearStateHash, CooldownSpearParamHash, duration);
		}
		soundID = Util.PlaySound(enterSoundString, base.gameObject);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration)
		{
			outer.SetNextStateToMain();
		}
	}

	public override void OnExit()
	{
		AkSoundEngine.StopPlayingID(soundID);
		base.OnExit();
	}

	public override void Update()
	{
		base.Update();
		base.characterBody.SetSpreadBloom(1f - base.age / duration, canOnlyIncreaseBloom: false);
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
