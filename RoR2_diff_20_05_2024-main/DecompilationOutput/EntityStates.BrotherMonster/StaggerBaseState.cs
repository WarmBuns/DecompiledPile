using UnityEngine;

namespace EntityStates.BrotherMonster;

public class StaggerBaseState : BaseState
{
	[SerializeField]
	public float duration;

	public virtual EntityState nextState => null;

	public override void OnEnter()
	{
		base.OnEnter();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration)
		{
			outer.SetNextState(nextState);
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Frozen;
	}
}
