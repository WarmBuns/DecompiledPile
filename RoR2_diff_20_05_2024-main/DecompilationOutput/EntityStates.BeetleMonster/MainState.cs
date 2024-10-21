using UnityEngine;

namespace EntityStates.BeetleMonster;

public class MainState : EntityState
{
	private Animator modelAnimator;

	private static int walkToRunBlendParamHash = Animator.StringToHash("walkToRunBlend");

	public override void OnEnter()
	{
		base.OnEnter();
		modelAnimator = GetModelAnimator();
		if ((bool)modelAnimator)
		{
			modelAnimator.CrossFadeInFixedTime("Walk", 0.1f);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if ((bool)modelAnimator)
		{
			modelAnimator.SetFloat(walkToRunBlendParamHash, 1f);
		}
	}
}
