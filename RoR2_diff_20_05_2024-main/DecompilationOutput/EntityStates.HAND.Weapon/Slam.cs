using System;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.HAND.Weapon;

public class Slam : BaseState
{
	public static float baseDuration = 3.5f;

	public static float returnToIdlePercentage;

	public static float impactDamageCoefficient = 2f;

	public static float earthquakeDamageCoefficient = 2f;

	public static float forceMagnitude = 16f;

	public static float radius = 3f;

	public static GameObject hitEffectPrefab;

	public static GameObject swingEffectPrefab;

	public static GameObject projectilePrefab;

	private Transform hammerChildTransform;

	private OverlapAttack attack;

	private Animator modelAnimator;

	private float duration;

	private bool hasSwung;

	private static int SlamStateHash = Animator.StringToHash("Slam");

	private static int SlamParamHash = Animator.StringToHash("Slam.playbackRate");

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
		attack.damage = impactDamageCoefficient * damageStat;
		attack.hitEffectPrefab = hitEffectPrefab;
		attack.isCrit = Util.CheckRoll(critStat, base.characterBody.master);
		if ((bool)modelTransform)
		{
			attack.hitBoxGroup = Array.Find(modelTransform.GetComponents<HitBoxGroup>(), (HitBoxGroup element) => element.groupName == "Hammer");
			ChildLocator component = modelTransform.GetComponent<ChildLocator>();
			if ((bool)component)
			{
				hammerChildTransform = component.FindChild("SwingCenter");
			}
		}
		if ((bool)modelAnimator)
		{
			PlayAnimation("Gesture", SlamStateHash, SlamParamHash, duration);
		}
		if ((bool)base.characterBody)
		{
			base.characterBody.SetAimTimer(2f);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (NetworkServer.active && (bool)modelAnimator && modelAnimator.GetFloat("Hammer.hitBoxActive") > 0.5f)
		{
			if (!hasSwung)
			{
				Ray aimRay = GetAimRay();
				EffectManager.SimpleMuzzleFlash(swingEffectPrefab, base.gameObject, "SwingCenter", transmit: true);
				ProjectileManager.instance.FireProjectile(projectilePrefab, aimRay.origin, Util.QuaternionSafeLookRotation(aimRay.direction), base.gameObject, damageStat * earthquakeDamageCoefficient, forceMagnitude, Util.CheckRoll(critStat, base.characterBody.master));
				hasSwung = true;
			}
			attack.forceVector = hammerChildTransform.right;
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
