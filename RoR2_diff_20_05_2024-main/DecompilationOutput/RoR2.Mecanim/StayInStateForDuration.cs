using UnityEngine;

namespace RoR2.Mecanim;

public class StayInStateForDuration : StateMachineBehaviour
{
	[Tooltip("The reference float - this is how long we will stay in this state")]
	public string durationFloatParameterName;

	private int durationFloatParameterHash;

	[Tooltip("The counter float - this is exposed incase we want to reset it")]
	public string stopwatchFloatParameterName;

	private int stopwatchFloatParameterHash;

	[Tooltip("The bool that will be set to 'false' once the duration is up, and 'true' when entering this state.")]
	public string deactivationBoolParameterName;

	private int deactivationBoolParameterHash;

	public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		base.OnStateEnter(animator, stateInfo, layerIndex);
		durationFloatParameterHash = Animator.StringToHash(durationFloatParameterName);
		stopwatchFloatParameterHash = Animator.StringToHash(stopwatchFloatParameterName);
		deactivationBoolParameterHash = Animator.StringToHash(deactivationBoolParameterName);
		animator.SetBool(deactivationBoolParameterHash, value: true);
	}

	public override void OnStateUpdate(Animator animator, AnimatorStateInfo animatorStateInfo, int layerIndex)
	{
		base.OnStateUpdate(animator, animatorStateInfo, layerIndex);
		float @float = animator.GetFloat(stopwatchFloatParameterHash);
		float float2 = animator.GetFloat(durationFloatParameterHash);
		@float += Time.deltaTime;
		if (@float >= float2)
		{
			animator.SetFloat(stopwatchFloatParameterHash, 0f);
			animator.SetBool(deactivationBoolParameterHash, value: false);
		}
		else
		{
			animator.SetFloat(stopwatchFloatParameterHash, @float);
		}
	}
}
