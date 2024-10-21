using RoR2;
using UnityEngine;

namespace EntityStates.Mage;

public class MageCharacterMain : GenericCharacterMain
{
	private EntityStateMachine jetpackStateMachine;

	public bool jumpButtonState;

	private bool heldPress;

	private bool jumpToggledState;

	private float oldJumpHeldTime;

	private float jumpButtonHeldTime;

	public override void OnEnter()
	{
		base.OnEnter();
		jetpackStateMachine = EntityStateMachine.FindByCustomName(base.gameObject, "Jet");
	}

	public override void ProcessJump()
	{
		if (hasCharacterMotor && hasInputBank && base.isAuthority)
		{
			if (NetworkUser.readOnlyLocalPlayersList[0]?.localUser?.userProfile.toggleArtificerHover ?? true)
			{
				if (base.inputBank.jump.down)
				{
					oldJumpHeldTime = jumpButtonHeldTime;
					jumpButtonHeldTime += Time.deltaTime;
					heldPress = oldJumpHeldTime < 0.5f && jumpButtonHeldTime >= 0.5f;
				}
				else
				{
					oldJumpHeldTime = 0f;
					jumpButtonHeldTime = 0f;
					heldPress = false;
				}
				if (!base.characterMotor.isGrounded)
				{
					if (base.characterMotor.jumpCount == base.characterBody.maxJumpCount)
					{
						if (base.inputBank.jump.justPressed)
						{
							jumpButtonState = !jumpButtonState;
						}
					}
					else if (heldPress)
					{
						jumpButtonState = !jumpButtonState;
					}
				}
				else
				{
					jumpButtonState = false;
				}
			}
			else
			{
				jumpButtonState = base.inputBank.jump.down;
			}
			bool num = jumpButtonState && base.characterMotor.velocity.y < 0f && !base.characterMotor.isGrounded;
			bool flag = jetpackStateMachine.state.GetType() == typeof(JetpackOn);
			if (num && !flag)
			{
				jetpackStateMachine.SetNextState(new JetpackOn());
			}
			if (!num && flag)
			{
				jetpackStateMachine.SetNextState(new Idle());
			}
		}
		base.ProcessJump();
	}

	public override void OnExit()
	{
		if (base.isAuthority && (bool)jetpackStateMachine)
		{
			jetpackStateMachine.SetNextState(new Idle());
		}
		base.OnExit();
	}
}
