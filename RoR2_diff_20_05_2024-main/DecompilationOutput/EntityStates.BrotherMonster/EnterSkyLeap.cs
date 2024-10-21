using RoR2;
using UnityEngine;

namespace EntityStates.BrotherMonster;

public class EnterSkyLeap : BaseState
{
	public static float baseDuration;

	public static string soundString;

	private float duration;

	private static int EnterSkyLeapStateHash = Animator.StringToHash("EnterSkyLeap");

	private static int BufferEmptyStateHash = Animator.StringToHash("BufferEmpty");

	private static int SkyLeapParamHash = Animator.StringToHash("SkyLeap.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		Util.PlaySound(soundString, base.gameObject);
		PlayAnimation("Body", EnterSkyLeapStateHash, SkyLeapParamHash, duration);
		PlayAnimation("FullBody Override", BufferEmptyStateHash);
		base.characterDirection.moveVector = base.characterDirection.forward;
		base.characterBody.AddTimedBuff(RoR2Content.Buffs.ArmorBoost, baseDuration);
		AimAnimator aimAnimator = GetAimAnimator();
		if ((bool)aimAnimator)
		{
			aimAnimator.enabled = true;
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && base.fixedAge > duration)
		{
			outer.SetNextState(new HoldSkyLeap());
		}
	}
}
