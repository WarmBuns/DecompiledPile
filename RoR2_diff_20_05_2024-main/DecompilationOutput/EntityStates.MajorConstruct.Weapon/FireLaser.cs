using System;
using EntityStates.EngiTurret.EngiTurretWeapon;
using RoR2;
using RoR2.Audio;
using UnityEngine;

namespace EntityStates.MajorConstruct.Weapon;

public class FireLaser : FireBeam
{
	[SerializeField]
	public float duration;

	[SerializeField]
	public float aimMaxSpeed;

	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	[SerializeField]
	public string animationPlaybackParameterName;

	[SerializeField]
	public LoopSoundDef loopSoundDef;

	private LoopSoundManager.SoundLoopPtr loopPtr;

	private AimAnimator.DirectionOverrideRequest animatorDirectionOverrideRequest;

	private Vector3 aimDirection;

	private Vector3 aimVelocity;

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation(animationLayerName, animationStateName, animationPlaybackParameterName, duration);
		if ((bool)loopSoundDef)
		{
			loopPtr = LoopSoundManager.PlaySoundLoopLocal(base.gameObject, loopSoundDef);
		}
		AimAnimator component = GetComponent<AimAnimator>();
		if ((bool)component)
		{
			animatorDirectionOverrideRequest = component.RequestDirectionOverride(GetAimDirection);
		}
		aimDirection = GetTargetDirection();
	}

	public override void OnExit()
	{
		animatorDirectionOverrideRequest?.Dispose();
		LoopSoundManager.StopSoundLoopLocal(loopPtr);
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		aimDirection = Vector3.RotateTowards(aimDirection, GetTargetDirection(), aimMaxSpeed * (MathF.PI / 180f) * GetDeltaTime(), float.PositiveInfinity);
		base.FixedUpdate();
	}

	public override void ModifyBullet(BulletAttack bulletAttack)
	{
	}

	protected override EntityState GetNextState()
	{
		return new TerminateLaser(GetBeamEndPoint());
	}

	public override bool ShouldFireLaser()
	{
		return duration > base.fixedAge;
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Pain;
	}

	private Vector3 GetAimDirection()
	{
		return aimDirection;
	}

	private Vector3 GetTargetDirection()
	{
		if ((bool)base.inputBank)
		{
			return base.inputBank.aimDirection;
		}
		return base.transform.forward;
	}

	public override Ray GetLaserRay()
	{
		if ((bool)base.inputBank)
		{
			return new Ray(base.inputBank.aimOrigin, aimDirection);
		}
		return new Ray(base.transform.position, aimDirection);
	}
}
