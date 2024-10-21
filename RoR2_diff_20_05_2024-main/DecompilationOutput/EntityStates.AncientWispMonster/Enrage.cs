using System;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.AncientWispMonster;

public class Enrage : BaseState
{
	public static float baseDuration = 3.5f;

	public static GameObject enragePrefab;

	private Animator modelAnimator;

	private float duration;

	private bool hasCastBuff;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		modelAnimator = GetModelAnimator();
		if ((bool)modelAnimator)
		{
			PlayCrossfade("Gesture", "Enrage", "Enrage.playbackRate", duration, 0.2f);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (NetworkServer.active && (bool)modelAnimator && modelAnimator.GetFloat("Enrage.activate") > 0.5f && !hasCastBuff)
		{
			EffectData effectData = new EffectData();
			effectData.origin = base.transform.position;
			effectData.SetNetworkedObjectReference(base.gameObject);
			EffectManager.SpawnEffect(enragePrefab, effectData, transmit: true);
			hasCastBuff = true;
			base.characterBody.AddBuff(JunkContent.Buffs.EnrageAncientWisp);
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
		HGPhysics.OverlapSphere(out var colliders, position, maxDistance);
		foreach (Collider collider in colliders)
		{
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
