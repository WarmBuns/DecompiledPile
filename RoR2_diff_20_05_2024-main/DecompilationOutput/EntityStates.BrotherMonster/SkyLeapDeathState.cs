using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.BrotherMonster;

public class SkyLeapDeathState : GenericCharacterDeath
{
	public static float baseDuration;

	private float duration;

	private static int SkyLeapStateHash = Animator.StringToHash("EnterSkyLeap");

	private static int BufferEmptyStateHash = Animator.StringToHash("BufferEmpty");

	private static int SkyLeapParamHash = Animator.StringToHash("SkyLeap.playbackRate");

	protected override bool shouldAutoDestroy => false;

	public override void OnEnter()
	{
		duration = baseDuration / attackSpeedStat;
		base.OnEnter();
	}

	protected override void PlayDeathAnimation(float crossfadeDuration = 0.1f)
	{
		PlayAnimation("Body", SkyLeapStateHash, SkyLeapParamHash, duration);
		PlayAnimation("FullBody Override", BufferEmptyStateHash);
		base.characterDirection.moveVector = base.characterDirection.forward;
		AimAnimator aimAnimator = GetAimAnimator();
		if ((bool)aimAnimator)
		{
			aimAnimator.enabled = true;
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (NetworkServer.active && base.fixedAge >= duration)
		{
			DestroyBodyAsapServer();
		}
	}
}
