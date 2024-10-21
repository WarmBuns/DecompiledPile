using RoR2;
using UnityEngine;

namespace EntityStates.Commando.CommandoWeapon;

public class PrepBarrage : BaseState
{
	public static float baseDuration = 3f;

	public static string prepBarrageSoundString;

	private float duration;

	private Animator modelAnimator;

	private static int PrepBarrageStateHash = Animator.StringToHash("PrepBarrage");

	private static int PrepBarrageParamHash = Animator.StringToHash("PrepBarrage.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		modelAnimator = GetModelAnimator();
		if ((bool)modelAnimator)
		{
			PlayAnimation("Gesture", PrepBarrageStateHash, PrepBarrageParamHash, duration);
		}
		Util.PlaySound(prepBarrageSoundString, base.gameObject);
		if ((bool)base.characterBody)
		{
			base.characterBody.SetAimTimer(duration);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration && base.isAuthority)
		{
			FireBarrage nextState = new FireBarrage();
			outer.SetNextState(nextState);
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
