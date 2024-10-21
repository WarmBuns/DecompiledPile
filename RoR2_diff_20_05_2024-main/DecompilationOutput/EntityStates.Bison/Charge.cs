using System;
using RoR2;
using UnityEngine;

namespace EntityStates.Bison;

public class Charge : BaseState
{
	public static float chargeDuration;

	public static float chargeMovementSpeedCoefficient;

	public static float turnSpeed;

	public static float turnSmoothTime;

	public static float impactDamageCoefficient;

	public static float impactForce;

	public static float damageCoefficient;

	public static float upwardForceMagnitude;

	public static float awayForceMagnitude;

	public static GameObject hitEffectPrefab;

	public static float overlapResetFrequency;

	public static float overlapSphereRadius;

	public static float selfStunDuration;

	public static float selfStunForce;

	public static string startSoundString;

	public static string endSoundString;

	public static string footstepOverrideSoundString;

	public static string headbuttImpactSound;

	private float stopwatch;

	private float overlapResetStopwatch;

	private Animator animator;

	private Vector3 targetMoveVector;

	private Vector3 targetMoveVectorVelocity;

	private ContactDamage contactDamage;

	private OverlapAttack attack;

	private HitBoxGroup hitboxGroup;

	private ChildLocator childLocator;

	private Transform sphereCheckTransform;

	private string baseFootstepString;

	private static int forwardSpeedParamHash = Animator.StringToHash("forwardSpeed");

	public override void OnEnter()
	{
		base.OnEnter();
		animator = GetModelAnimator();
		childLocator = animator.GetComponent<ChildLocator>();
		FootstepHandler component = animator.GetComponent<FootstepHandler>();
		if ((bool)component)
		{
			baseFootstepString = component.baseFootstepString;
			component.baseFootstepString = footstepOverrideSoundString;
		}
		Util.PlaySound(startSoundString, base.gameObject);
		PlayCrossfade("Body", "ChargeForward", 0.2f);
		ResetOverlapAttack();
		SetSprintEffectActive(active: true);
		if ((bool)childLocator)
		{
			sphereCheckTransform = childLocator.FindChild("SphereCheckTransform");
		}
		if (!sphereCheckTransform && (bool)base.characterBody)
		{
			sphereCheckTransform = base.characterBody.coreTransform;
		}
		if (!sphereCheckTransform)
		{
			sphereCheckTransform = base.transform;
		}
	}

	private void SetSprintEffectActive(bool active)
	{
		if ((bool)childLocator)
		{
			childLocator.FindChild("SprintEffect")?.gameObject.SetActive(active);
		}
	}

	public override void OnExit()
	{
		base.OnExit();
		base.characterMotor.moveDirection = Vector3.zero;
		Util.PlaySound(endSoundString, base.gameObject);
		Util.PlaySound("stop_bison_charge_attack_loop", base.gameObject);
		SetSprintEffectActive(active: false);
		FootstepHandler component = animator.GetComponent<FootstepHandler>();
		if ((bool)component)
		{
			component.baseFootstepString = baseFootstepString;
		}
	}

	public override void FixedUpdate()
	{
		targetMoveVector = Vector3.ProjectOnPlane(Vector3.SmoothDamp(targetMoveVector, base.inputBank.aimDirection, ref targetMoveVectorVelocity, turnSmoothTime, turnSpeed), Vector3.up).normalized;
		base.characterDirection.moveVector = targetMoveVector;
		Vector3 forward = base.characterDirection.forward;
		float value = moveSpeedStat * chargeMovementSpeedCoefficient;
		base.characterMotor.moveDirection = forward * chargeMovementSpeedCoefficient;
		animator.SetFloat(forwardSpeedParamHash, value);
		if (base.isAuthority && attack.Fire())
		{
			Util.PlaySound(headbuttImpactSound, base.gameObject);
		}
		if (overlapResetStopwatch >= 1f / overlapResetFrequency)
		{
			overlapResetStopwatch -= 1f / overlapResetFrequency;
		}
		if (base.isAuthority && HGPhysics.DoesOverlapSphere(sphereCheckTransform.position, overlapSphereRadius, LayerIndex.world.mask))
		{
			Util.PlaySound(headbuttImpactSound, base.gameObject);
			EffectManager.SimpleMuzzleFlash(hitEffectPrefab, base.gameObject, "SphereCheckTransform", transmit: true);
			base.healthComponent.TakeDamageForce(forward * selfStunForce, alwaysApply: true);
			StunState stunState = new StunState();
			stunState.stunDuration = selfStunDuration;
			outer.SetNextState(stunState);
			return;
		}
		float deltaTime = GetDeltaTime();
		stopwatch += deltaTime;
		overlapResetStopwatch += deltaTime;
		if (stopwatch > chargeDuration)
		{
			outer.SetNextStateToMain();
		}
		base.FixedUpdate();
	}

	private void ResetOverlapAttack()
	{
		if (!hitboxGroup)
		{
			Transform modelTransform = GetModelTransform();
			if ((bool)modelTransform)
			{
				hitboxGroup = Array.Find(modelTransform.GetComponents<HitBoxGroup>(), (HitBoxGroup element) => element.groupName == "Charge");
			}
		}
		attack = new OverlapAttack();
		attack.attacker = base.gameObject;
		attack.inflictor = base.gameObject;
		attack.teamIndex = TeamComponent.GetObjectTeam(attack.attacker);
		attack.damage = damageCoefficient * damageStat;
		attack.hitEffectPrefab = hitEffectPrefab;
		attack.forceVector = Vector3.up * upwardForceMagnitude;
		attack.pushAwayForce = awayForceMagnitude;
		attack.hitBoxGroup = hitboxGroup;
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
