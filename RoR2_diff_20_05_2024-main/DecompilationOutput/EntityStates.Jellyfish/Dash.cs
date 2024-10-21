using UnityEngine;

namespace EntityStates.Jellyfish;

public class Dash : BaseState
{
	public static float duration = 1.8f;

	public static float speedCoefficient = 2f;

	private Animator modelAnimator;

	private static int swimrateParamHash = Animator.StringToHash("swim.rate");

	public override void OnEnter()
	{
		base.OnEnter();
		modelAnimator = GetModelAnimator();
		if ((bool)base.rigidbodyMotor)
		{
			base.rigidbodyMotor.moveVector = base.rigidbodyMotor.rigid.transform.forward * base.characterBody.moveSpeed * speedCoefficient;
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if ((bool)base.rigidbodyMotor && (bool)modelAnimator)
		{
			modelAnimator.SetFloat(swimrateParamHash, Vector3.Magnitude(base.rigidbodyMotor.rigid.velocity));
		}
		if (base.fixedAge >= duration)
		{
			outer.SetNextState(new SwimState());
		}
	}
}
