using UnityEngine;

namespace EntityStates.Vulture;

public class FallingDeath : GenericCharacterDeath
{
	private Animator animator;

	private static int isGroundedParamHash = Animator.StringToHash("isGrounded");

	public override void OnEnter()
	{
		base.OnEnter();
		if ((bool)base.characterMotor)
		{
			base.characterMotor.velocity.y = 0f;
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if ((bool)animator)
		{
			animator.SetBool(isGroundedParamHash, base.characterMotor.isGrounded);
			return;
		}
		animator = GetModelAnimator();
		if ((bool)animator)
		{
			int layerIndex = animator.GetLayerIndex("FlyOverride");
			animator.SetLayerWeight(layerIndex, 0f);
		}
	}
}
