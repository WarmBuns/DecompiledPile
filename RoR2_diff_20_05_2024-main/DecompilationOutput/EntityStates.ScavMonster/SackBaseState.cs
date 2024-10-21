using UnityEngine;

namespace EntityStates.ScavMonster;

public class SackBaseState : BaseState
{
	public static string muzzleName;

	protected Transform muzzleTransform;

	public override void OnEnter()
	{
		base.OnEnter();
		muzzleTransform = FindModelChild(muzzleName);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}
