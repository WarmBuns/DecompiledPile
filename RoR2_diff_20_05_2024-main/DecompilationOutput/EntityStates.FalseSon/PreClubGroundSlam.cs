using RoR2;

namespace EntityStates.FalseSon;

public class PreClubGroundSlam : BaseState
{
	public float charge;

	public static float baseDuration;

	public static string enterSoundString;

	public static float upwardVelocity;

	private float duration;

	private bool forceAnimWrapUp = true;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		PlayAnimation("Body", "PreGroundSlam", "GroundSlam.playbackRate", duration);
		Util.PlaySound(enterSoundString, base.gameObject);
		base.characterMotor.Motor.ForceUnground();
		base.characterMotor.disableAirControlUntilCollision = false;
		base.characterMotor.velocity.y = upwardVelocity;
		base.characterBody.bodyFlags |= CharacterBody.BodyFlags.IgnoreFallDamage;
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		base.characterMotor.moveDirection = base.inputBank.moveVector;
		base.characterBody.SetSpreadBloom(charge);
		if (base.fixedAge > duration)
		{
			forceAnimWrapUp = false;
			outer.SetNextState(new ClubGroundSlam
			{
				charge = charge
			});
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Frozen;
	}

	public override void ModifyNextState(EntityState nextState)
	{
		ClubGroundSlam clubGroundSlam = nextState as ClubGroundSlam;
		forceAnimWrapUp = clubGroundSlam == null;
	}

	public override void OnExit()
	{
		base.OnExit();
		if (forceAnimWrapUp)
		{
			PlayAnimation("Gesture, Override", "Empty");
			PlayAnimation("Gesture, Additive", "Empty");
			PlayAnimation("Body", "Idle");
			base.characterBody.bodyFlags &= ~CharacterBody.BodyFlags.IgnoreFallDamage;
		}
	}
}
