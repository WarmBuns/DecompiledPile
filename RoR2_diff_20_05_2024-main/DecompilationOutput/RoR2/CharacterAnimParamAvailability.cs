using UnityEngine;

namespace RoR2;

public struct CharacterAnimParamAvailability
{
	public bool isMoving;

	public bool turnAngle;

	public bool isGrounded;

	public bool mainRootPlaybackRate;

	public bool forwardSpeed;

	public bool rightSpeed;

	public bool upSpeed;

	public bool walkSpeed;

	public bool isSprinting;

	public static CharacterAnimParamAvailability FromAnimator(Animator animator)
	{
		AnimatorControllerParameter[] parameters = animator.parameters;
		CharacterAnimParamAvailability result = default(CharacterAnimParamAvailability);
		result.isMoving = Util.HasAnimationParameter(AnimationParameters.isMoving, parameters);
		result.turnAngle = Util.HasAnimationParameter(AnimationParameters.turnAngle, parameters);
		result.isGrounded = Util.HasAnimationParameter(AnimationParameters.isGrounded, parameters);
		result.mainRootPlaybackRate = Util.HasAnimationParameter(AnimationParameters.mainRootPlaybackRate, parameters);
		result.forwardSpeed = Util.HasAnimationParameter(AnimationParameters.forwardSpeed, parameters);
		result.rightSpeed = Util.HasAnimationParameter(AnimationParameters.rightSpeed, parameters);
		result.upSpeed = Util.HasAnimationParameter(AnimationParameters.upSpeed, parameters);
		result.walkSpeed = Util.HasAnimationParameter(AnimationParameters.walkSpeed, parameters);
		result.isSprinting = Util.HasAnimationParameter(AnimationParameters.isSprinting, parameters);
		return result;
	}
}
