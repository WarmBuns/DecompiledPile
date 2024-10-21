using RoR2;
using UnityEngine;

namespace EntityStates;

public class AnimatedJump : BaseState
{
	private float duration;

	private bool hasInputJump;

	public override void OnEnter()
	{
		base.OnEnter();
		Animator modelAnimator = GetModelAnimator();
		if ((bool)modelAnimator)
		{
			int layerIndex = modelAnimator.GetLayerIndex("Body");
			modelAnimator.CrossFadeInFixedTime("AnimatedJump", 0.25f);
			modelAnimator.Update(0f);
			duration = modelAnimator.GetNextAnimatorStateInfo(layerIndex).length;
			AimAnimator component = modelAnimator.GetComponent<AimAnimator>();
			if ((bool)component)
			{
				component.enabled = true;
			}
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration / 2f && base.isAuthority && !hasInputJump)
		{
			hasInputJump = true;
			base.characterMotor.moveDirection = base.inputBank.moveVector;
			GenericCharacterMain.ApplyJumpVelocity(base.characterMotor, base.characterBody, 1f, 1f);
		}
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}
}
