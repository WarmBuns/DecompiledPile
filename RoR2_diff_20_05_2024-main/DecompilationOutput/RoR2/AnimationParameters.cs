using UnityEngine;

namespace RoR2;

public static class AnimationParameters
{
	public static readonly int isMoving = Animator.StringToHash("isMoving");

	public static readonly int turnAngle = Animator.StringToHash("turnAngle");

	public static readonly int isGrounded = Animator.StringToHash("isGrounded");

	public static readonly int mainRootPlaybackRate = Animator.StringToHash("mainRootPlaybackRate");

	public static readonly int forwardSpeed = Animator.StringToHash("forwardSpeed");

	public static readonly int rightSpeed = Animator.StringToHash("rightSpeed");

	public static readonly int upSpeed = Animator.StringToHash("upSpeed");

	public static readonly int walkSpeed = Animator.StringToHash("walkSpeed");

	public static readonly int isSprinting = Animator.StringToHash("isSprinting");

	public static readonly int aimWeight = Animator.StringToHash("aimWeight");
}
