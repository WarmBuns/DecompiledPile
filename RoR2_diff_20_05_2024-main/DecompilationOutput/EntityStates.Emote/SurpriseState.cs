using UnityEngine;

namespace EntityStates.Emote;

public class SurpriseState : EntityState
{
	private float duration;

	public override void OnEnter()
	{
		base.OnEnter();
		Animator modelAnimator = GetModelAnimator();
		if ((bool)modelAnimator)
		{
			int layerIndex = modelAnimator.GetLayerIndex("Body");
			modelAnimator.Play("EmoteSurprise", layerIndex, 0f);
			modelAnimator.Update(0f);
			duration = modelAnimator.GetCurrentAnimatorStateInfo(layerIndex).length;
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Death;
	}
}
