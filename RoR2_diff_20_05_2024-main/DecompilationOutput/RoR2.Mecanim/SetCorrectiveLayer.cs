using UnityEngine;

namespace RoR2.Mecanim;

public class SetCorrectiveLayer : StateMachineBehaviour
{
	public string referenceOverrideLayerName;

	public float maxWeight = 1f;

	private float smoothVelocity;

	public override void OnStateMachineEnter(Animator animator, int stateMachinePathHash)
	{
		base.OnStateMachineEnter(animator, stateMachinePathHash);
	}

	public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		int layerIndex2 = animator.GetLayerIndex(referenceOverrideLayerName);
		float target = Mathf.Min(animator.GetLayerWeight(layerIndex2), maxWeight);
		float weight = Mathf.SmoothDamp(animator.GetLayerWeight(layerIndex), target, ref smoothVelocity, 0.2f);
		animator.SetLayerWeight(layerIndex, weight);
		base.OnStateUpdate(animator, stateInfo, layerIndex);
	}
}
