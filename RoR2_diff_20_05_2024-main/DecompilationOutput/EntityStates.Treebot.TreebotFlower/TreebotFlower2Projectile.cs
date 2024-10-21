using System.Collections.Generic;
using RoR2;
using RoR2.Orbs;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Treebot.TreebotFlower;

public class TreebotFlower2Projectile : BaseState
{
	public static float yankIdealDistance;

	public static AnimationCurve yankSuitabilityCurve;

	public static float healthFractionYieldPerHit;

	public static float radius;

	public static float healPulseCount;

	public static float duration;

	public static float rootPulseCount;

	public static string enterSoundString;

	public static string exitSoundString;

	public static GameObject enterEffectPrefab;

	public static GameObject exitEffectPrefab;

	private List<CharacterBody> rootedBodies;

	private float healTimer;

	private float rootPulseTimer;

	private GameObject owner;

	private ProcChainMask procChainMask;

	private float procCoefficient;

	private TeamIndex teamIndex = TeamIndex.None;

	private float damage;

	private DamageTypeCombo damageType;

	private bool crit;

	private float healPulseHealthFractionValue;

	private static int SpawnToIdleStateHash = Animator.StringToHash("SpawnToIdle");

	public override void OnEnter()
	{
		base.OnEnter();
		ProjectileController component = GetComponent<ProjectileController>();
		if ((bool)component)
		{
			owner = component.owner;
			procChainMask = component.procChainMask;
			procCoefficient = component.procCoefficient;
			teamIndex = component.teamFilter.teamIndex;
		}
		ProjectileDamage component2 = GetComponent<ProjectileDamage>();
		if ((bool)component2)
		{
			damage = component2.damage;
			damageType = component2.damageType;
			crit = component2.crit;
		}
		if (NetworkServer.active)
		{
			rootedBodies = new List<CharacterBody>();
		}
		PlayAnimation("Base", SpawnToIdleStateHash);
		Util.PlaySound(enterSoundString, base.gameObject);
		if ((bool)enterEffectPrefab)
		{
			EffectManager.SimpleEffect(enterEffectPrefab, base.transform.position, base.transform.rotation, transmit: false);
		}
		ChildLocator component3 = GetModelTransform().GetComponent<ChildLocator>();
		if ((bool)component3)
		{
			Transform obj = component3.FindChild("AreaIndicator");
			obj.localScale = new Vector3(radius, radius, radius);
			obj.gameObject.SetActive(value: true);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (NetworkServer.active)
		{
			float deltaTime = GetDeltaTime();
			rootPulseTimer -= deltaTime;
			healTimer -= deltaTime;
			if (rootPulseTimer <= 0f)
			{
				rootPulseTimer += duration / rootPulseCount;
				RootPulse();
			}
			if (healTimer <= 0f)
			{
				healTimer += duration / healPulseCount;
				HealPulse();
			}
			if (base.fixedAge >= duration)
			{
				EntityState.Destroy(base.gameObject);
			}
		}
	}

	private void RootPulse()
	{
		Vector3 position = base.transform.position;
		HurtBox[] hurtBoxes = new SphereSearch
		{
			origin = position,
			radius = radius,
			mask = LayerIndex.entityPrecise.mask
		}.RefreshCandidates().FilterCandidatesByHurtBoxTeam(TeamMask.GetEnemyTeams(teamIndex)).OrderCandidatesByDistance()
			.FilterCandidatesByDistinctHurtBoxEntities()
			.GetHurtBoxes();
		foreach (HurtBox hurtBox in hurtBoxes)
		{
			CharacterBody body = hurtBox.healthComponent.body;
			if (!rootedBodies.Contains(body))
			{
				rootedBodies.Add(body);
				body.AddBuff(RoR2Content.Buffs.Entangle);
				body.RecalculateStats();
				Vector3 vector = hurtBox.transform.position - position;
				float magnitude = vector.magnitude;
				Vector3 vector2 = vector / magnitude;
				Rigidbody component = hurtBox.healthComponent.GetComponent<Rigidbody>();
				float num = (component ? component.mass : 1f);
				float num2 = magnitude - yankIdealDistance;
				float num3 = yankSuitabilityCurve.Evaluate(num);
				Vector3 vector3 = (component ? component.velocity : Vector3.zero);
				if (HGMath.IsVectorNaN(vector3))
				{
					vector3 = Vector3.zero;
				}
				Vector3 vector4 = -vector3;
				if (num2 > 0f)
				{
					vector4 = vector2 * (0f - Trajectory.CalculateInitialYSpeedForHeight(num2, 0f - body.acceleration));
				}
				Vector3 force = vector4 * (num * num3);
				DamageInfo damageInfo = new DamageInfo
				{
					attacker = owner,
					inflictor = base.gameObject,
					crit = crit,
					damage = damage,
					damageColorIndex = DamageColorIndex.Default,
					damageType = damageType,
					force = force,
					position = hurtBox.transform.position,
					procChainMask = procChainMask,
					procCoefficient = procCoefficient
				};
				hurtBox.healthComponent.TakeDamage(damageInfo);
				HurtBox hurtBoxReference = hurtBox;
				HurtBoxGroup hurtBoxGroup = hurtBox.hurtBoxGroup;
				for (int j = 0; (float)j < Mathf.Min(4f, body.radius * 2f); j++)
				{
					EffectData effectData = new EffectData
					{
						scale = 1f,
						origin = position,
						genericFloat = Mathf.Max(0.2f, duration - base.fixedAge)
					};
					effectData.SetHurtBoxReference(hurtBoxReference);
					EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/OrbEffects/EntangleOrbEffect"), effectData, transmit: true);
					hurtBoxReference = hurtBoxGroup.hurtBoxes[Random.Range(0, hurtBoxGroup.hurtBoxes.Length)];
				}
			}
		}
	}

	public override void OnExit()
	{
		if (rootedBodies != null)
		{
			foreach (CharacterBody rootedBody in rootedBodies)
			{
				rootedBody.RemoveBuff(RoR2Content.Buffs.Entangle);
			}
			rootedBodies = null;
		}
		Util.PlaySound(exitSoundString, base.gameObject);
		if ((bool)exitEffectPrefab)
		{
			EffectManager.SimpleEffect(exitEffectPrefab, base.transform.position, base.transform.rotation, transmit: false);
		}
		base.OnExit();
	}

	private void HealPulse()
	{
		HealthComponent healthComponent = (owner ? owner.GetComponent<HealthComponent>() : null);
		if ((bool)healthComponent && rootedBodies.Count > 0)
		{
			float num = 1f / healPulseCount;
			HealOrb healOrb = new HealOrb();
			healOrb.origin = base.transform.position;
			healOrb.target = healthComponent.body.mainHurtBox;
			healOrb.healValue = num * healthFractionYieldPerHit * healthComponent.fullHealth * (float)rootedBodies.Count;
			healOrb.overrideDuration = 0.3f;
			OrbManager.instance.AddOrb(healOrb);
		}
	}
}
