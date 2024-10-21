using System;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.HAND.Weapon;

public class FullSwing : BaseState
{
	public static float baseDuration = 3.5f;

	public static float returnToIdlePercentage;

	public static float damageCoefficient = 4f;

	public static float forceMagnitude = 16f;

	public static float radius = 3f;

	public static GameObject hitEffectPrefab;

	public static GameObject swingEffectPrefab;

	private Transform hammerChildTransform;

	private OverlapAttack attack;

	private Animator modelAnimator;

	private float duration;

	private bool hasSwung;

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
			int layerIndex = modelAnimator.GetLayerIndex("Gesture");
			if (modelAnimator.GetCurrentAnimatorStateInfo(layerIndex).IsName("FullSwing3") || modelAnimator.GetCurrentAnimatorStateInfo(layerIndex).IsName("FullSwing1"))
			{
				PlayCrossfade("Gesture", "FullSwing2", "FullSwing.playbackRate", duration / (1f - returnToIdlePercentage), 0.2f);
			}
			else if (modelAnimator.GetCurrentAnimatorStateInfo(layerIndex).IsName("FullSwing2"))
			{
				PlayCrossfade("Gesture", "FullSwing3", "FullSwing.playbackRate", duration / (1f - returnToIdlePercentage), 0.2f);
			}
			else
			{
				PlayCrossfade("Gesture", "FullSwing1", "FullSwing.playbackRate", duration / (1f - returnToIdlePercentage), 0.2f);
			}
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
				EffectManager.SimpleMuzzleFlash(swingEffectPrefab, base.gameObject, "SwingCenter", transmit: true);
				hasSwung = true;
			}
			attack.forceVector = hammerChildTransform.right * (0f - forceMagnitude);
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

	private static void PullEnemies(Vector3 position, Vector3 direction, float coneAngle, float maxDistance, float force, TeamIndex excludedTeam)
	{
		float num = Mathf.Cos(coneAngle * 0.5f * (MathF.PI / 180f));
		Collider[] colliders;
		int num2 = HGPhysics.OverlapSphere(out colliders, position, maxDistance);
		for (int i = 0; i < num2; i++)
		{
			Collider collider = colliders[i];
			Vector3 position2 = collider.transform.position;
			Vector3 normalized = (position - position2).normalized;
			if (!(Vector3.Dot(-normalized, direction) >= num))
			{
				continue;
			}
			TeamIndex teamIndex = TeamIndex.Neutral;
			TeamComponent component = collider.GetComponent<TeamComponent>();
			if (!component)
			{
				continue;
			}
			teamIndex = component.teamIndex;
			if (teamIndex != excludedTeam)
			{
				CharacterMotor component2 = collider.GetComponent<CharacterMotor>();
				if ((bool)component2)
				{
					component2.ApplyForce(normalized * force);
				}
				Rigidbody component3 = collider.GetComponent<Rigidbody>();
				if ((bool)component3)
				{
					component3.AddForce(normalized * force, ForceMode.Impulse);
				}
			}
		}
		HGPhysics.ReturnResults(colliders);
	}
}
