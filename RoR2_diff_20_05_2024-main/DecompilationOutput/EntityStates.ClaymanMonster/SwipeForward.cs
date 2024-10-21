using System;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.ClaymanMonster;

public class SwipeForward : BaseState
{
	public static float baseDuration = 3.5f;

	public static float damageCoefficient = 4f;

	public static float forceMagnitude = 16f;

	public static float selfForceMagnitude;

	public static float radius = 3f;

	public static GameObject hitEffectPrefab;

	public static GameObject swingEffectPrefab;

	public static string attackString;

	private OverlapAttack attack;

	private Animator modelAnimator;

	private float duration;

	private bool hasSlashed;

	private static int SwipeForwardStateHash = Animator.StringToHash("SwipeForward");

	private static int SwipeForwardParamHash = Animator.StringToHash("SwipeForward.playbackRate");

	private static int SwipeForwardHitboxActiveParamHash = Animator.StringToHash("SwipeForward.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		modelAnimator = GetModelAnimator();
		Transform modelTransform = GetModelTransform();
		attack = new OverlapAttack();
		attack.attacker = base.gameObject;
		attack.inflictor = base.gameObject;
		attack.teamIndex = TeamComponent.GetObjectTeam(attack.attacker);
		attack.damage = damageCoefficient * damageStat;
		attack.hitEffectPrefab = hitEffectPrefab;
		attack.isCrit = Util.CheckRoll(critStat, base.characterBody.master);
		Util.PlaySound(attackString, base.gameObject);
		if ((bool)modelTransform)
		{
			attack.hitBoxGroup = Array.Find(modelTransform.GetComponents<HitBoxGroup>(), (HitBoxGroup element) => element.groupName == "Sword");
		}
		if ((bool)modelAnimator)
		{
			PlayAnimation("Gesture, Override", SwipeForwardStateHash, SwipeForwardParamHash, duration);
			PlayAnimation("Gesture, Additive", SwipeForwardStateHash, SwipeForwardParamHash, duration);
		}
		if ((bool)base.characterBody)
		{
			base.characterBody.SetAimTimer(2f);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (NetworkServer.active && (bool)modelAnimator && modelAnimator.GetFloat(SwipeForwardHitboxActiveParamHash) > 0.1f)
		{
			if (!hasSlashed)
			{
				EffectManager.SimpleMuzzleFlash(swingEffectPrefab, base.gameObject, "SwingCenter", transmit: true);
				HealthComponent healthComponent = base.characterBody.healthComponent;
				CharacterDirection component = base.characterBody.GetComponent<CharacterDirection>();
				if ((bool)healthComponent)
				{
					healthComponent.TakeDamageForce(selfForceMagnitude * component.forward, alwaysApply: true);
				}
				hasSlashed = true;
			}
			attack.forceVector = base.transform.forward * forceMagnitude;
			attack.Fire();
		}
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}
