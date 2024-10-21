using RoR2;
using UnityEngine;

namespace EntityStates.ClaymanMonster;

public class Leap : BaseState
{
	public static string leapSoundString;

	public static float minimumDuration;

	public static float verticalJumpSpeed;

	public static float horizontalJumpSpeedCoefficient;

	private Vector3 forwardDirection;

	private Animator animator;

	private float stopwatch;

	private bool playedImpact;

	private static int LeapcycleParamHash = Animator.StringToHash("Leap.cycle");

	private static int LightImpactStateHash = Animator.StringToHash("LightImpact");

	private static int IdleStateHash = Animator.StringToHash("Idle");

	public override void OnEnter()
	{
		base.OnEnter();
		animator = GetModelAnimator();
		Util.PlaySound(leapSoundString, base.gameObject);
		if ((bool)base.characterMotor)
		{
			base.characterMotor.velocity.y = verticalJumpSpeed;
		}
		forwardDirection = Vector3.ProjectOnPlane(base.inputBank.aimDirection, Vector3.up);
		base.characterDirection.moveVector = forwardDirection;
		PlayCrossfade("Body", "LeapAirLoop", 0.15f);
	}

	public override void FixedUpdate()
	{
		stopwatch += GetDeltaTime();
		animator.SetFloat(LeapcycleParamHash, Mathf.Clamp01(Util.Remap(base.characterMotor.velocity.y, 0f - verticalJumpSpeed, verticalJumpSpeed, 1f, 0f)));
		Vector3 velocity = forwardDirection * base.characterBody.moveSpeed * horizontalJumpSpeedCoefficient;
		velocity.y = base.characterMotor.velocity.y;
		base.characterMotor.velocity = velocity;
		base.FixedUpdate();
		if (base.characterMotor.isGrounded && stopwatch > minimumDuration && !playedImpact)
		{
			playedImpact = true;
			int layerIndex = animator.GetLayerIndex("Impact");
			if (layerIndex >= 0)
			{
				animator.SetLayerWeight(layerIndex, 1.5f);
				animator.PlayInFixedTime(LightImpactStateHash, layerIndex, 0f);
			}
			if (base.isAuthority)
			{
				outer.SetNextStateToMain();
			}
		}
	}

	public override void OnExit()
	{
		PlayAnimation("Body", IdleStateHash);
		base.OnExit();
	}
}
