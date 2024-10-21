using RoR2;
using UnityEngine;

namespace EntityStates.BrotherMonster;

public class SprintBash : BasicMeleeAttack
{
	public static float durationBeforePriorityReduces;

	protected override void PlayAnimation()
	{
		PlayCrossfade("FullBody Override", "SprintBash", "SprintBash.playbackRate", duration, 0.05f);
	}

	public override void OnEnter()
	{
		base.OnEnter();
		AimAnimator aimAnimator = GetAimAnimator();
		if ((bool)aimAnimator)
		{
			aimAnimator.enabled = true;
		}
		if ((bool)base.characterDirection)
		{
			base.characterDirection.forward = base.inputBank.aimDirection;
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && (bool)base.inputBank && (bool)base.skillLocator && base.skillLocator.utility.IsReady() && base.inputBank.skill3.justPressed)
		{
			base.skillLocator.utility.ExecuteIfReady();
		}
	}

	public override void OnExit()
	{
		Transform transform = FindModelChild("SpinnyFX");
		if ((bool)transform)
		{
			transform.gameObject.SetActive(value: false);
		}
		PlayCrossfade("FullBody Override", "BufferEmpty", 0.1f);
		base.OnExit();
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		if (!(base.fixedAge > durationBeforePriorityReduces))
		{
			return InterruptPriority.PrioritySkill;
		}
		return InterruptPriority.Skill;
	}
}
