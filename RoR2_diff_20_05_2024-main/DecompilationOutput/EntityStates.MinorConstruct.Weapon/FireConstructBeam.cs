using UnityEngine;

namespace EntityStates.MinorConstruct.Weapon;

public class FireConstructBeam : GenericProjectileBaseState
{
	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation(animationLayerName, animationStateName);
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
