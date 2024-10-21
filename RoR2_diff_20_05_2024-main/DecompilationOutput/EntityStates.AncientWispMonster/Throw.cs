using System;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.AncientWispMonster;

public class Throw : BaseState
{
	public static float baseDuration = 3.5f;

	public static float returnToIdlePercentage;

	public static float damageCoefficient = 4f;

	public static float forceMagnitude = 16f;

	public static float radius = 3f;

	public static GameObject projectilePrefab;

	public static GameObject swingEffectPrefab;

	private Transform rightMuzzleTransform;

	private Animator modelAnimator;

	private float duration;

	private bool hasSwung;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		modelAnimator = GetModelAnimator();
		Transform modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			ChildLocator component = modelTransform.GetComponent<ChildLocator>();
			if ((bool)component)
			{
				rightMuzzleTransform = component.FindChild("MuzzleRight");
			}
		}
		if ((bool)modelAnimator)
		{
			int layerIndex = modelAnimator.GetLayerIndex("Gesture");
			if (modelAnimator.GetCurrentAnimatorStateInfo(layerIndex).IsName("Throw1"))
			{
				PlayCrossfade("Gesture", "Throw2", "Throw.playbackRate", duration / (1f - returnToIdlePercentage), 0.2f);
			}
			else
			{
				PlayCrossfade("Gesture", "Throw1", "Throw.playbackRate", duration / (1f - returnToIdlePercentage), 0.2f);
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
		if (NetworkServer.active && (bool)modelAnimator && modelAnimator.GetFloat("Throw.activate") > 0f && !hasSwung)
		{
			Ray aimRay = GetAimRay();
			Vector3 forward = aimRay.direction;
			if (Physics.Raycast(aimRay, out var hitInfo, (int)LayerIndex.world.mask))
			{
				forward = hitInfo.point - rightMuzzleTransform.position;
			}
			ProjectileManager.instance.FireProjectile(projectilePrefab, rightMuzzleTransform.position, Util.QuaternionSafeLookRotation(forward), base.gameObject, damageStat * damageCoefficient, forceMagnitude, Util.CheckRoll(critStat, base.characterBody.master));
			EffectManager.SimpleMuzzleFlash(swingEffectPrefab, base.gameObject, "RightSwingCenter", transmit: true);
			hasSwung = true;
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
