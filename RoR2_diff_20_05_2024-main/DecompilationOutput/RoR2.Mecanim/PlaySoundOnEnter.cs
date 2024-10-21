using UnityEngine;

namespace RoR2.Mecanim;

public class PlaySoundOnEnter : StateMachineBehaviour
{
	public string soundString;

	public string stopSoundString;

	public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		base.OnStateEnter(animator, stateInfo, layerIndex);
		Util.PlaySound(soundString, animator.gameObject);
	}

	public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		base.OnStateEnter(animator, stateInfo, layerIndex);
		Util.PlaySound(stopSoundString, animator.gameObject);
	}
}
