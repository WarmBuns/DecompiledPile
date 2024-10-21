using System;
using UnityEngine;

namespace RoR2.Mecanim;

public class ClockParamWriter : StateMachineBehaviour
{
	public string targetParamName = "time";

	public float cyclesPerDay = 2f;

	private const float secondsPerDay = 86400f;

	public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		base.OnStateUpdate(animator, stateInfo, layerIndex);
		float num = 0f;
		num = ((!Run.instance) ? ((float)(DateTime.Now - DateTime.Today).TotalSeconds) : Run.instance.GetRunStopwatch());
		animator.SetFloat(targetParamName, cyclesPerDay * num / 86400f);
	}
}
