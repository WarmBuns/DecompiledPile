using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.Halcyonite;

public class WhirlwindRush : BaseState
{
	public static float chargeMovementSpeedCoefficient;

	public static float turnSpeed;

	public static float turnSmoothTime;

	public static float damageCoefficient;

	public static GameObject hitEffectPrefab;

	private Animator animator;

	private Vector3 targetMoveVector;

	private Vector3 targetMoveVectorVelocity;

	private HitBoxGroup hitboxGroup;

	private ChildLocator childLocator;

	[SerializeField]
	public float spinUpDuration = 1f;

	[SerializeField]
	public float duration = 2f;

	[SerializeField]
	public float endSpinDuration = 1f;

	[SerializeField]
	public GameObject projectilePrefab;

	private bool firedWhirlWind;

	private static int forwardSpeedParamHash = Animator.StringToHash("forwardSpeed");

	private float originalMoveSpeed = 6.6f;

	private float originalAccSpeed = 40f;

	[SerializeField]
	public float rushingMoveSpeed = 13.5f;

	[SerializeField]
	public float rushingAccSpeed = 200f;

	private Vector3 safePosition;

	private float ageStartedWhirlwind;

	private float intervalToCheckForSafePath = 1f;

	public override void OnEnter()
	{
		originalMoveSpeed = base.characterBody.baseMoveSpeed;
		originalAccSpeed = base.characterBody.baseAcceleration;
		base.characterBody.baseMoveSpeed = 0.5f;
		base.characterMotor.moveDirection = Vector3.zero;
		safePosition = base.characterBody.corePosition;
		animator = GetModelAnimator();
		childLocator = animator.GetComponent<ChildLocator>();
		PlayCrossfade("FullBody Override", "WhirlwindRushEnter", "WhirlwindRush.playbackRate", duration, 0.1f);
		Util.PlaySound("Play_halcyonite_skill3_start", base.gameObject);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		bool stopWhirlwind = base.fixedAge > duration + spinUpDuration + endSpinDuration;
		if (base.fixedAge > spinUpDuration * 0.5f && !firedWhirlWind)
		{
			ProjectileManager.instance.FireProjectile(projectilePrefab, base.gameObject.transform.position, Quaternion.identity, base.gameObject, base.characterBody.damage * damageCoefficient, 0f, Util.CheckRoll(critStat, base.characterBody.master));
			firedWhirlWind = true;
			Util.PlaySound("Play_halcyonite_skill3_loop", base.gameObject);
		}
		if (firedWhirlWind && !stopWhirlwind)
		{
			if (base.fixedAge - ageStartedWhirlwind > intervalToCheckForSafePath)
			{
				HandleIdentifySafePathForward(ref stopWhirlwind);
			}
			else
			{
				HandleProceedAlongPath(ref stopWhirlwind);
			}
		}
		if (base.fixedAge > duration + spinUpDuration && !stopWhirlwind)
		{
			base.characterBody.baseMoveSpeed = 0f;
			if ((bool)base.characterMotor)
			{
				base.characterMotor.walkSpeedPenaltyCoefficient = 0f;
			}
			if ((bool)base.characterBody)
			{
				base.characterBody.isSprinting = false;
			}
			if ((bool)base.characterDirection)
			{
				base.characterDirection.moveVector = base.characterDirection.forward;
			}
			if ((bool)base.rigidbodyMotor)
			{
				base.rigidbodyMotor.moveVector = Vector3.zero;
			}
		}
		if (stopWhirlwind)
		{
			base.characterBody.baseMoveSpeed = originalMoveSpeed;
			base.characterBody.baseAcceleration = originalAccSpeed;
			if ((bool)base.characterMotor)
			{
				base.characterMotor.walkSpeedPenaltyCoefficient = 1f;
			}
			if ((bool)base.characterBody)
			{
				base.characterBody.isSprinting = false;
			}
			if ((bool)base.characterDirection)
			{
				base.characterDirection.moveVector = base.characterDirection.forward;
			}
			if ((bool)base.rigidbodyMotor)
			{
				base.rigidbodyMotor.moveVector = Vector3.zero;
			}
			Util.PlaySound("Stop_halcyonite_skill3_loop", base.gameObject);
			outer.SetNextStateToMain();
		}
	}

	private void HandleIdentifySafePathForward(ref bool stopWhirlwind)
	{
		Vector3 vector = base.inputBank.aimDirection * rushingMoveSpeed * GetDeltaTime();
		vector.y = 0f;
		vector += base.characterBody.aimOrigin;
		Debug.DrawLine(base.characterBody.aimOrigin, base.characterBody.aimOrigin + Vector3.right, Color.grey, 10f);
		Debug.DrawRay(vector + Vector3.up * 2f, Vector3.down, Color.green, 10f);
		Debug.DrawLine(vector + Vector3.up * 2f, vector + Vector3.up * 2f + Vector3.up * -10f, Color.yellow, 10f);
		if (!Physics.Raycast(new Ray(vector + Vector3.up * 2f, Vector3.down), out var _, 10f, (int)LayerIndex.world.mask | (int)LayerIndex.CommonMasks.characterBodiesOrDefault, QueryTriggerInteraction.Ignore))
		{
			stopWhirlwind = true;
		}
		else
		{
			ageStartedWhirlwind = base.fixedAge;
		}
	}

	private void HandleProceedAlongPath(ref bool stopWhirlwind)
	{
		if (!base.characterMotor.isGrounded)
		{
			TeleportHelper.TeleportBody(base.characterBody, safePosition);
			stopWhirlwind = true;
		}
		else
		{
			safePosition = base.characterBody.footPosition;
		}
		targetMoveVector = Vector3.ProjectOnPlane(Vector3.SmoothDamp(targetMoveVector, base.inputBank.aimDirection, ref targetMoveVectorVelocity, turnSmoothTime, turnSpeed), Vector3.up).normalized;
		base.characterDirection.moveVector = targetMoveVector;
		Vector3 forward = base.characterDirection.forward;
		float value = moveSpeedStat * chargeMovementSpeedCoefficient;
		base.characterMotor.moveDirection = forward * chargeMovementSpeedCoefficient;
		animator.SetFloat(forwardSpeedParamHash, value);
		base.characterBody.baseMoveSpeed = rushingMoveSpeed;
		base.characterBody.baseAcceleration = rushingAccSpeed;
	}

	public override void OnExit()
	{
		base.characterMotor.moveDirection = Vector3.zero;
		PlayCrossfade("FullBody Override", "WhirlwindRushExit", "WhirlwindRush.playbackRate", duration, 0.1f);
		Util.PlaySound("Play_halcyonite_skill3_end", base.gameObject);
		Util.PlaySound("Stop_halcyonite_skill3_loop", base.gameObject);
		base.characterBody.RecalculateStats();
		base.OnExit();
	}
}
