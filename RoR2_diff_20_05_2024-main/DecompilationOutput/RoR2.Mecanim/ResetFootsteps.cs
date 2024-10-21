using UnityEngine;

namespace RoR2.Mecanim;

public class ResetFootsteps : StateMachineBehaviour
{
	public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		_ = (bool)animator.GetComponent<FootstepHandler>();
	}
}
