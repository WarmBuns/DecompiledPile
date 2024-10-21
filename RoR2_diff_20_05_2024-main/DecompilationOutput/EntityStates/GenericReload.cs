using RoR2;
using UnityEngine;

namespace EntityStates;

public class GenericReload : BaseState
{
	[SerializeField]
	public string enterSoundString;

	protected float duration;

	public override void OnEnter()
	{
		base.OnEnter();
		if ((bool)base.skillLocator && (bool)base.skillLocator.primary)
		{
			duration = base.skillLocator.primary.CalculateFinalRechargeInterval();
		}
		Util.PlaySound(enterSoundString, base.gameObject);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge > duration)
		{
			outer.SetNextStateToMain();
		}
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Any;
	}
}
