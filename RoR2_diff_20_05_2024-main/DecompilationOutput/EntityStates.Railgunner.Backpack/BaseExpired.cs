using UnityEngine;

namespace EntityStates.Railgunner.Backpack;

public abstract class BaseExpired : BaseBackpack
{
	[SerializeField]
	public float duration;

	[SerializeField]
	public bool forceShieldRegen;

	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	[SerializeField]
	public string animationPlaybackRateParam;

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation(animationLayerName, animationStateName, animationPlaybackRateParam, duration);
		if (forceShieldRegen)
		{
			base.characterBody?.healthComponent?.ForceShieldRegen();
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration)
		{
			outer.SetNextState(InstantiateNextState());
		}
	}

	protected abstract EntityState InstantiateNextState();

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Death;
	}
}
