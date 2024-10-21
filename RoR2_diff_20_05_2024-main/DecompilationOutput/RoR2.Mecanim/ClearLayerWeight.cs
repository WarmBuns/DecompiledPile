using UnityEngine;

namespace RoR2.Mecanim;

public class ClearLayerWeight : StateMachineBehaviour
{
	public bool resetOnExit = true;

	public string layerName;

	public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		base.OnStateEnter(animator, stateInfo, layerIndex);
		int layerIndex2 = layerIndex;
		if (layerName.Length > 0)
		{
			layerIndex2 = animator.GetLayerIndex(layerName);
		}
		animator.SetLayerWeight(layerIndex2, 0f);
	}

	public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		base.OnStateExit(animator, stateInfo, layerIndex);
		if (resetOnExit)
		{
			int layerIndex2 = layerIndex;
			if (layerName.Length > 0)
			{
				layerIndex2 = animator.GetLayerIndex(layerName);
			}
			animator.SetLayerWeight(layerIndex2, 1f);
		}
	}
}
