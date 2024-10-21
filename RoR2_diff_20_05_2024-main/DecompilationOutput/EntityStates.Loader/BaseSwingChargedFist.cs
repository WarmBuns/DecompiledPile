using System;
using RoR2;
using UnityEngine;

namespace EntityStates.Loader;

public class BaseSwingChargedFist : LoaderMeleeAttack
{
	public float charge;

	[SerializeField]
	public float minLungeSpeed;

	[SerializeField]
	public float maxLungeSpeed;

	[SerializeField]
	public float minPunchForce;

	[SerializeField]
	public float maxPunchForce;

	[SerializeField]
	public float minDuration;

	[SerializeField]
	public float maxDuration;

	public static bool disableAirControlUntilCollision;

	public static float speedCoefficientOnExit;

	public static float velocityDamageCoefficient;

	protected Vector3 punchVelocity;

	private float bonusDamage;

	private static int ChargePunchStateHash = Animator.StringToHash("ChargePunch");

	private static int ChargePunchParamHash = Animator.StringToHash("ChargePunch.playbackRate");

	public float punchSpeed { get; private set; }

	public static event Action<BaseSwingChargedFist> onHitAuthorityGlobal;

	public override void OnEnter()
	{
		base.OnEnter();
		if (base.isAuthority)
		{
			base.characterMotor.Motor.ForceUnground();
			base.characterMotor.disableAirControlUntilCollision |= disableAirControlUntilCollision;
			punchVelocity = CalculateLungeVelocity(base.characterMotor.velocity, GetAimRay().direction, charge, minLungeSpeed, maxLungeSpeed);
			base.characterMotor.velocity = punchVelocity;
			base.characterDirection.forward = base.characterMotor.velocity.normalized;
			punchSpeed = base.characterMotor.velocity.magnitude;
			bonusDamage = punchSpeed * (velocityDamageCoefficient * damageStat);
		}
	}

	protected override float CalcDuration()
	{
		return Mathf.Lerp(minDuration, maxDuration, charge);
	}

	protected override void PlayAnimation()
	{
		base.PlayAnimation();
		PlayAnimation("FullBody, Override", ChargePunchStateHash, ChargePunchParamHash, duration);
	}

	protected override void AuthorityFixedUpdate()
	{
		base.AuthorityFixedUpdate();
		if (!base.authorityInHitPause)
		{
			base.characterMotor.velocity = punchVelocity;
			base.characterDirection.forward = punchVelocity;
			base.characterBody.isSprinting = true;
		}
	}

	protected override void AuthorityModifyOverlapAttack(OverlapAttack overlapAttack)
	{
		base.AuthorityModifyOverlapAttack(overlapAttack);
		overlapAttack.damage = damageCoefficient * damageStat + bonusDamage;
		overlapAttack.forceVector = base.characterMotor.velocity + GetAimRay().direction * Mathf.Lerp(minPunchForce, maxPunchForce, charge);
		if (base.fixedAge + GetDeltaTime() >= duration)
		{
			HitBoxGroup hitBoxGroup = FindHitBoxGroup("PunchLollypop");
			if ((bool)hitBoxGroup)
			{
				base.hitBoxGroup = hitBoxGroup;
				overlapAttack.hitBoxGroup = hitBoxGroup;
			}
		}
	}

	protected override void OnMeleeHitAuthority()
	{
		base.OnMeleeHitAuthority();
		BaseSwingChargedFist.onHitAuthorityGlobal?.Invoke(this);
	}

	public override void OnExit()
	{
		base.OnExit();
		base.characterMotor.velocity *= speedCoefficientOnExit;
	}

	public static Vector3 CalculateLungeVelocity(Vector3 currentVelocity, Vector3 aimDirection, float charge, float minLungeSpeed, float maxLungeSpeed)
	{
		currentVelocity = ((Vector3.Dot(currentVelocity, aimDirection) < 0f) ? Vector3.zero : Vector3.Project(currentVelocity, aimDirection));
		return currentVelocity + aimDirection * Mathf.Lerp(minLungeSpeed, maxLungeSpeed, charge);
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}
