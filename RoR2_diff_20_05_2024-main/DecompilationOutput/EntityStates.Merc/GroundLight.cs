using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Merc;

public class GroundLight : BaseState
{
	public enum ComboState
	{
		GroundLight1,
		GroundLight2,
		GroundLight3
	}

	private struct ComboStateInfo
	{
		private string mecanimStateName;

		private string mecanimPlaybackRateName;
	}

	public static float baseComboAttackDuration;

	public static float baseFinisherAttackDuration;

	public static float baseEarlyExitDuration;

	public static string comboAttackSoundString;

	public static string finisherAttackSoundString;

	public static float comboDamageCoefficient;

	public static float finisherDamageCoefficient;

	public static float forceMagnitude;

	public static GameObject comboHitEffectPrefab;

	public static GameObject finisherHitEffectPrefab;

	public static GameObject comboSwingEffectPrefab;

	public static GameObject finisherSwingEffectPrefab;

	public static float hitPauseDuration;

	public static float selfForceMagnitude;

	public static string hitSoundString;

	public static float slashPitch;

	private float stopwatch;

	private float attackDuration;

	private float earlyExitDuration;

	private Animator animator;

	private OverlapAttack overlapAttack;

	private float hitPauseTimer;

	private bool isInHitPause;

	private bool hasSwung;

	private bool hasHit;

	private GameObject swingEffectInstance;

	public ComboState comboState;

	private Vector3 characterForward;

	private string slashChildName;

	private HitStopCachedState hitStopCachedState;

	private GameObject swingEffectPrefab;

	private GameObject hitEffectPrefab;

	private string attackSoundString;

	private static int GroundLight1StateHash = Animator.StringToHash("GroundLight1");

	private static int GroundLight2StateHash = Animator.StringToHash("GroundLight2");

	private static int GroundLight3StateHash = Animator.StringToHash("GroundLight3");

	private static int GroundLightParamHash = Animator.StringToHash("GroundLight.playbackRate");

	private static int SwordactiveParamHash = Animator.StringToHash("Sword.active");

	public override void OnEnter()
	{
		base.OnEnter();
		stopwatch = 0f;
		earlyExitDuration = baseEarlyExitDuration / attackSpeedStat;
		animator = GetModelAnimator();
		bool @bool = animator.GetBool("isMoving");
		bool bool2 = animator.GetBool("isGrounded");
		switch (comboState)
		{
		case ComboState.GroundLight1:
			attackDuration = baseComboAttackDuration / attackSpeedStat;
			overlapAttack = InitMeleeOverlap(comboDamageCoefficient, hitEffectPrefab, GetModelTransform(), "Sword");
			if (@bool || !bool2)
			{
				PlayAnimation("Gesture, Additive", GroundLight1StateHash, GroundLightParamHash, attackDuration);
				PlayAnimation("Gesture, Override", GroundLight1StateHash, GroundLightParamHash, attackDuration);
			}
			else
			{
				PlayAnimation("FullBody, Override", GroundLight1StateHash, GroundLightParamHash, attackDuration);
			}
			slashChildName = "GroundLight1Slash";
			swingEffectPrefab = comboSwingEffectPrefab;
			hitEffectPrefab = comboHitEffectPrefab;
			attackSoundString = comboAttackSoundString;
			break;
		case ComboState.GroundLight2:
			attackDuration = baseComboAttackDuration / attackSpeedStat;
			overlapAttack = InitMeleeOverlap(comboDamageCoefficient, hitEffectPrefab, GetModelTransform(), "Sword");
			if (@bool || !bool2)
			{
				PlayAnimation("Gesture, Additive", GroundLight2StateHash, GroundLightParamHash, attackDuration);
				PlayAnimation("Gesture, Override", GroundLight2StateHash, GroundLightParamHash, attackDuration);
			}
			else
			{
				PlayAnimation("FullBody, Override", GroundLight2StateHash, GroundLightParamHash, attackDuration);
			}
			slashChildName = "GroundLight2Slash";
			swingEffectPrefab = comboSwingEffectPrefab;
			hitEffectPrefab = comboHitEffectPrefab;
			attackSoundString = comboAttackSoundString;
			break;
		case ComboState.GroundLight3:
			attackDuration = baseFinisherAttackDuration / attackSpeedStat;
			overlapAttack = InitMeleeOverlap(finisherDamageCoefficient, hitEffectPrefab, GetModelTransform(), "SwordLarge");
			if (@bool || !bool2)
			{
				PlayAnimation("Gesture, Additive", GroundLight3StateHash, GroundLightParamHash, attackDuration);
				PlayAnimation("Gesture, Override", GroundLight3StateHash, GroundLightParamHash, attackDuration);
			}
			else
			{
				PlayAnimation("FullBody, Override", GroundLight3StateHash, GroundLightParamHash, attackDuration);
			}
			slashChildName = "GroundLight3Slash";
			swingEffectPrefab = finisherSwingEffectPrefab;
			hitEffectPrefab = finisherHitEffectPrefab;
			attackSoundString = finisherAttackSoundString;
			break;
		}
		base.characterBody.SetAimTimer(attackDuration + 1f);
		overlapAttack.hitEffectPrefab = hitEffectPrefab;
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		hitPauseTimer -= GetDeltaTime();
		if (base.isAuthority)
		{
			bool flag = FireMeleeOverlap(overlapAttack, animator, "Sword.active", forceMagnitude);
			hasHit |= flag;
			if (flag)
			{
				Util.PlaySound(hitSoundString, base.gameObject);
				if (!isInHitPause)
				{
					hitStopCachedState = CreateHitStopCachedState(base.characterMotor, animator, "GroundLight.playbackRate");
					hitPauseTimer = hitPauseDuration / attackSpeedStat;
					isInHitPause = true;
				}
			}
			if (hitPauseTimer <= 0f && isInHitPause)
			{
				ConsumeHitStopCachedState(hitStopCachedState, base.characterMotor, animator);
				isInHitPause = false;
			}
		}
		if (animator.GetFloat(SwordactiveParamHash) > 0f && !hasSwung)
		{
			Util.PlayAttackSpeedSound(attackSoundString, base.gameObject, slashPitch);
			HealthComponent healthComponent = base.characterBody.healthComponent;
			CharacterDirection component = base.characterBody.GetComponent<CharacterDirection>();
			if ((bool)healthComponent)
			{
				healthComponent.TakeDamageForce(selfForceMagnitude * component.forward, alwaysApply: true);
			}
			hasSwung = true;
			EffectManager.SimpleMuzzleFlash(swingEffectPrefab, base.gameObject, slashChildName, transmit: false);
		}
		if (!isInHitPause)
		{
			stopwatch += GetDeltaTime();
		}
		else
		{
			base.characterMotor.velocity = Vector3.zero;
			animator.SetFloat(GroundLightParamHash, 0f);
		}
		if (base.isAuthority && stopwatch >= attackDuration - earlyExitDuration)
		{
			if (!hasSwung)
			{
				overlapAttack.Fire();
			}
			if (base.inputBank.skill1.down && comboState != ComboState.GroundLight3)
			{
				GroundLight groundLight = new GroundLight();
				groundLight.comboState = comboState + 1;
				outer.SetNextState(groundLight);
			}
			else if (stopwatch >= attackDuration)
			{
				outer.SetNextStateToMain();
			}
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}

	public override void OnSerialize(NetworkWriter writer)
	{
		base.OnSerialize(writer);
		writer.Write((byte)comboState);
	}

	public override void OnDeserialize(NetworkReader reader)
	{
		base.OnDeserialize(reader);
		comboState = (ComboState)reader.ReadByte();
	}
}
