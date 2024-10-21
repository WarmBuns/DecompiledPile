using UnityEngine;

namespace RoR2.Mecanim;

public class SetRandomIntOnEnter : StateMachineBehaviour
{
	public string intParameterName;

	public int rangeMin;

	public int rangeMax;

	public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		base.OnStateEnter(animator, stateInfo, layerIndex);
		animator.SetInteger(intParameterName, Random.Range(rangeMin, rangeMax + 1));
	}
}
