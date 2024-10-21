using UnityEngine;

namespace EntityStates.Jellyfish;

public class SwimState : BaseState
{
	private Animator modelAnimator;

	private bool skill1InputReceived;

	private bool skill2InputReceived;

	private bool skill3InputReceived;

	private bool skill4InputReceived;

	private bool jumpInputReceived;

	private static int swimrateParamHash = Animator.StringToHash("swim.rate");

	public override void OnEnter()
	{
		base.OnEnter();
		modelAnimator = GetModelAnimator();
	}

	public override void Update()
	{
		base.Update();
		if ((bool)base.inputBank)
		{
			skill1InputReceived = base.inputBank.skill1.down;
			skill2InputReceived |= base.inputBank.skill2.down;
			skill3InputReceived |= base.inputBank.skill3.down;
			skill4InputReceived |= base.inputBank.skill4.down;
			jumpInputReceived |= base.inputBank.jump.down;
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (!base.isAuthority)
		{
			return;
		}
		if ((bool)base.inputBank)
		{
			if ((bool)base.rigidbodyMotor)
			{
				base.rigidbodyMotor.moveVector = base.inputBank.moveVector * base.characterBody.moveSpeed;
				if ((bool)modelAnimator)
				{
					modelAnimator.SetFloat(swimrateParamHash, Vector3.Magnitude(base.rigidbodyMotor.rigid.velocity));
				}
			}
			if ((bool)base.rigidbodyDirection)
			{
				base.rigidbodyDirection.aimDirection = GetAimRay().direction;
			}
		}
		if ((bool)base.skillLocator)
		{
			if ((bool)base.skillLocator.primary && skill1InputReceived)
			{
				base.skillLocator.primary.ExecuteIfReady();
			}
			if ((bool)base.skillLocator.secondary && skill2InputReceived)
			{
				base.skillLocator.secondary.ExecuteIfReady();
			}
			if ((bool)base.skillLocator.utility && skill3InputReceived)
			{
				base.skillLocator.utility.ExecuteIfReady();
			}
			if ((bool)base.skillLocator.special && skill4InputReceived)
			{
				base.skillLocator.special.ExecuteIfReady();
			}
		}
	}
}
