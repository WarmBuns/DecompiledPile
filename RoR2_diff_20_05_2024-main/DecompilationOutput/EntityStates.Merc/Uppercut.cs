using RoR2;
using UnityEngine;

namespace EntityStates.Merc;

public class Uppercut : BaseState
{
	public static GameObject swingEffectPrefab;

	public static GameObject hitEffectPrefab;

	public static string enterSoundString;

	public static string attackSoundString;

	public static string hitSoundString;

	public static float slashPitch;

	public static float hitPauseDuration;

	public static float upwardForceStrength;

	public static float baseDuration;

	public static float baseDamageCoefficient;

	public static string slashChildName;

	public static float moveSpeedBonusCoefficient;

	public static string hitboxString;

	public static AnimationCurve yVelocityCurve;

	protected Animator animator;

	protected float duration;

	protected float hitInterval;

	protected bool hasSwung;

	protected float hitPauseTimer;

	protected bool isInHitPause;

	protected OverlapAttack overlapAttack;

	protected HitStopCachedState hitStopCachedState;

	private static int UppercutExitStateHash = Animator.StringToHash("UppercutExit");

	private static int UppercutStateHash = Animator.StringToHash("Uppercut");

	private static int UppercutParamHash = Animator.StringToHash("Uppercut.playbackRate");

	private static int SwordactiveParamHash = Animator.StringToHash("Sword.active");

	public override void OnEnter()
	{
		base.OnEnter();
		animator = GetModelAnimator();
		duration = baseDuration / attackSpeedStat;
		overlapAttack = InitMeleeOverlap(baseDamageCoefficient, hitEffectPrefab, GetModelTransform(), hitboxString);
		overlapAttack.forceVector = Vector3.up * upwardForceStrength;
		if ((bool)base.characterDirection && (bool)base.inputBank)
		{
			base.characterDirection.forward = base.inputBank.aimDirection;
		}
		Util.PlaySound(enterSoundString, base.gameObject);
		PlayAnim();
	}

	protected virtual void PlayAnim()
	{
		PlayCrossfade("FullBody, Override", UppercutStateHash, UppercutParamHash, duration, 0.1f);
	}

	public override void OnExit()
	{
		base.OnExit();
		PlayAnimation("FullBody, Override", UppercutExitStateHash);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		float deltaTime = GetDeltaTime();
		hitPauseTimer -= deltaTime;
		if (!base.isAuthority)
		{
			return;
		}
		if (animator.GetFloat(SwordactiveParamHash) > 0.2f && !hasSwung)
		{
			hasSwung = true;
			base.characterMotor.Motor.ForceUnground();
			Util.PlayAttackSpeedSound(attackSoundString, base.gameObject, slashPitch);
			EffectManager.SimpleMuzzleFlash(swingEffectPrefab, base.gameObject, slashChildName, transmit: true);
		}
		if (FireMeleeOverlap(overlapAttack, animator, SwordactiveParamHash, 0f, calculateForceVector: false))
		{
			Util.PlaySound(hitSoundString, base.gameObject);
			if (!isInHitPause)
			{
				hitStopCachedState = CreateHitStopCachedState(base.characterMotor, animator, "Uppercut.playbackRate");
				hitPauseTimer = hitPauseDuration / attackSpeedStat;
				isInHitPause = true;
			}
		}
		if (hitPauseTimer <= 0f && isInHitPause)
		{
			ConsumeHitStopCachedState(hitStopCachedState, base.characterMotor, animator);
			base.characterMotor.Motor.ForceUnground();
			isInHitPause = false;
		}
		if (!isInHitPause)
		{
			if ((bool)base.characterMotor && (bool)base.characterDirection)
			{
				Vector3 velocity = base.characterDirection.forward * moveSpeedStat * Mathf.Lerp(moveSpeedBonusCoefficient, 0f, base.age / duration);
				velocity.y = yVelocityCurve.Evaluate(base.fixedAge / duration);
				base.characterMotor.velocity = velocity;
			}
		}
		else
		{
			base.fixedAge -= deltaTime;
			base.characterMotor.velocity = Vector3.zero;
			hitPauseTimer -= deltaTime;
			animator.SetFloat(UppercutParamHash, 0f);
		}
		if (base.fixedAge >= duration)
		{
			if (hasSwung)
			{
				hasSwung = true;
				overlapAttack.Fire();
			}
			outer.SetNextStateToMain();
		}
	}
}
