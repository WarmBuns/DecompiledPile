using RoR2.Skills;
using UnityEngine;

namespace EntityStates.Railgunner.Backpack;

public abstract class BaseOnline : BaseBackpack
{
	[SerializeField]
	public SkillDef requiredSkillDef;

	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	[SerializeField]
	public string cooldownParamName;

	private int animationStateHash;

	private int cooldownParamHash;

	private Animator animator;

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation(animationLayerName, animationStateName);
		animator = GetModelAnimator();
		cooldownParamHash = Animator.StringToHash(cooldownParamName);
		animationStateHash = Animator.StringToHash(animationStateName);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if ((object)base.skillLocator.special.skillDef != requiredSkillDef)
		{
			outer.SetNextState(new Disconnected());
		}
		else if ((bool)animator)
		{
			float num = base.skillLocator.special.CalculateFinalRechargeInterval();
			float value = 0f;
			if (num > 0f)
			{
				value = base.skillLocator.special.cooldownRemaining / num;
			}
			animator.SetFloat(cooldownParamHash, value);
		}
	}

	public override void OnExit()
	{
		if ((bool)animator)
		{
			animator.SetFloat(cooldownParamHash, 0f);
		}
		base.OnExit();
	}
}
