using UnityEngine;

namespace EntityStates.VoidRaidCrab;

public abstract class BaseVacuumAttackState : BaseState
{
	[SerializeField]
	public float baseDuration;

	[SerializeField]
	public string animLayerName;

	[SerializeField]
	public string animStateName;

	[SerializeField]
	public string animPlaybackRateParamName;

	public static string vacuumOriginChildLocatorName;

	protected float duration { get; private set; }

	protected Transform vacuumOrigin { get; private set; }

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		if (!string.IsNullOrEmpty(animLayerName) && !string.IsNullOrEmpty(animStateName) && !string.IsNullOrEmpty(animPlaybackRateParamName))
		{
			PlayAnimation(animLayerName, animStateName, animPlaybackRateParamName, duration);
		}
		if (!string.IsNullOrEmpty(vacuumOriginChildLocatorName))
		{
			vacuumOrigin = FindModelChild(vacuumOriginChildLocatorName);
		}
		else
		{
			vacuumOrigin = base.transform;
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && base.fixedAge >= duration)
		{
			OnLifetimeExpiredAuthority();
		}
	}

	protected abstract void OnLifetimeExpiredAuthority();

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Pain;
	}
}
