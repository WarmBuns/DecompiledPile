using RoR2;
using UnityEngine;

namespace EntityStates.Merc;

public class WhirlwindBase : BaseState
{
	public static GameObject swingEffectPrefab;

	public static GameObject hitEffectPrefab;

	public static string attackSoundString;

	public static string hitSoundString;

	public static float slashPitch;

	public static float hitPauseDuration;

	[SerializeField]
	public float baseDuration;

	[SerializeField]
	public float baseDamageCoefficient;

	[SerializeField]
	public string slashChildName;

	[SerializeField]
	public float selfForceMagnitude;

	[SerializeField]
	public float moveSpeedBonusCoefficient;

	[SerializeField]
	public float smallHopVelocity;

	[SerializeField]
	public string hitboxString;

	protected Animator animator;

	protected float duration;

	protected float hitInterval;

	protected int swingCount;

	protected float hitPauseTimer;

	protected bool isInHitPause;

	protected OverlapAttack overlapAttack;

	protected HitStopCachedState hitStopCachedState;

	private static int SwordactiveParamHash = Animator.StringToHash("Sword.active");

	private static int WhirlwindplaybackRateParamHash = Animator.StringToHash("Whirlwind.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		animator = GetModelAnimator();
		duration = baseDuration / attackSpeedStat;
		overlapAttack = InitMeleeOverlap(baseDamageCoefficient, hitEffectPrefab, GetModelTransform(), hitboxString);
		if ((bool)base.characterDirection && (bool)base.inputBank)
		{
			base.characterDirection.forward = base.inputBank.aimDirection;
		}
		SmallHop(base.characterMotor, smallHopVelocity);
		PlayAnim();
	}

	protected virtual void PlayAnim()
	{
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		hitPauseTimer -= GetDeltaTime();
		if (animator.GetFloat("Sword.active") > (float)swingCount)
		{
			swingCount++;
			Util.PlayAttackSpeedSound(attackSoundString, base.gameObject, slashPitch);
			EffectManager.SimpleMuzzleFlash(swingEffectPrefab, base.gameObject, slashChildName, transmit: false);
			if (base.isAuthority)
			{
				overlapAttack.ResetIgnoredHealthComponents();
				if ((bool)base.characterMotor)
				{
					base.characterMotor.ApplyForce(selfForceMagnitude * base.characterDirection.forward, alwaysApply: true);
				}
			}
		}
		if (!base.isAuthority)
		{
			return;
		}
		if (FireMeleeOverlap(overlapAttack, animator, SwordactiveParamHash, 0f))
		{
			Util.PlaySound(hitSoundString, base.gameObject);
			if (!isInHitPause)
			{
				hitStopCachedState = CreateHitStopCachedState(base.characterMotor, animator, "Whirlwind.playbackRate");
				hitPauseTimer = hitPauseDuration / attackSpeedStat;
				isInHitPause = true;
			}
		}
		if (hitPauseTimer <= 0f && isInHitPause)
		{
			ConsumeHitStopCachedState(hitStopCachedState, base.characterMotor, animator);
			isInHitPause = false;
		}
		if (!isInHitPause)
		{
			if ((bool)base.characterMotor && (bool)base.characterDirection)
			{
				Vector3 velocity = base.characterDirection.forward * moveSpeedStat * Mathf.Lerp(moveSpeedBonusCoefficient, 1f, base.age / duration);
				velocity.y = base.characterMotor.velocity.y;
				base.characterMotor.velocity = velocity;
			}
		}
		else
		{
			base.characterMotor.velocity = Vector3.zero;
			hitPauseTimer -= GetDeltaTime();
			animator.SetFloat(WhirlwindplaybackRateParamHash, 0f);
		}
		if (base.fixedAge >= duration)
		{
			while (swingCount < 2)
			{
				swingCount++;
				overlapAttack.Fire();
			}
			outer.SetNextStateToMain();
		}
	}
}
