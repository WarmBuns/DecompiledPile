using RoR2;
using UnityEngine;

namespace EntityStates.Loader;

public class PreGroundSlam : BaseCharacterMain
{
	public static float baseDuration;

	public static string enterSoundString;

	public static float upwardVelocity;

	private float duration;

	private static int PreGroundSlamStateHash = Animator.StringToHash("PreGroundSlam");

	private static int GroundSlamParamHash = Animator.StringToHash("GroundSlam.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		PlayAnimation("Body", PreGroundSlamStateHash, GroundSlamParamHash, duration);
		Util.PlaySound(enterSoundString, base.gameObject);
		base.characterMotor.Motor.ForceUnground();
		base.characterMotor.disableAirControlUntilCollision = false;
		base.characterMotor.velocity.y = upwardVelocity;
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		base.characterMotor.moveDirection = base.inputBank.moveVector;
		if (base.fixedAge > duration)
		{
			outer.SetNextState(new GroundSlam());
		}
	}
}
