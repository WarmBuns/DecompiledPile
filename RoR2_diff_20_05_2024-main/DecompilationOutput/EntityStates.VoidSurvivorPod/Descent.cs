using EntityStates.SurvivorPod;
using RoR2;
using UnityEngine;

namespace EntityStates.VoidSurvivorPod;

public class Descent : SurvivorPodBaseState
{
	[SerializeField]
	public float duration;

	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	[SerializeField]
	public string enterSoundString;

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation(animationLayerName, animationStateName);
		Util.PlaySound(enterSoundString, base.gameObject);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && duration < base.fixedAge)
		{
			TransitionIntoNextState();
		}
	}

	protected virtual void TransitionIntoNextState()
	{
		outer.SetNextState(new Landed());
	}
}
