using RoR2;
using UnityEngine;

namespace EntityStates.Toolbot;

public class AimStunDrone : AimGrenade
{
	public static string enterSoundString;

	public static string exitSoundString;

	private static int PrepBombStateHash = Animator.StringToHash("PrepBomb");

	private static int PutAwayGunStateHash = Animator.StringToHash("PutAwayGun");

	private static int PrepBombParamHash = Animator.StringToHash("PrepBomb.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		Util.PlaySound(enterSoundString, base.gameObject);
		PlayAnimation("Gesture, Additive", PrepBombStateHash, PrepBombParamHash, minimumDuration);
		PlayAnimation("Stance, Override", PutAwayGunStateHash);
	}

	public override void OnExit()
	{
		base.OnExit();
		outer.SetNextState(new RecoverAimStunDrone());
		Util.PlaySound(exitSoundString, base.gameObject);
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}
