using RoR2;
using UnityEngine;

namespace EntityStates.Railgunner.Weapon;

public abstract class BaseChargeSnipe : BaseState, IBaseWeaponState
{
	private const string backpackStateMachineName = "Backpack";

	[SerializeField]
	public float baseDuration;

	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	[SerializeField]
	public string animationPlaybackRateParam;

	private float duration;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		PlayAnimation(animationLayerName, animationStateName, animationPlaybackRateParam, duration);
		EntityStateMachine entityStateMachine = EntityStateMachine.FindByCustomName(base.gameObject, "Backpack");
		if ((bool)entityStateMachine)
		{
			entityStateMachine.SetNextState(InstantiateBackpackState());
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration)
		{
			outer.SetNextStateToMain();
		}
	}

	public bool CanScope()
	{
		return false;
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Frozen;
	}

	protected abstract EntityState InstantiateBackpackState();
}
