using RoR2;
using RoR2.Orbs;
using UnityEngine;

namespace EntityStates.SiphonItem;

public class DetonateState : BaseSiphonItemState
{
	public static float baseSiphonRange = 50f;

	public static float baseDuration;

	public static float healPulseFraction = 0.5f;

	public static float healMultiplier = 2f;

	public static float damageFraction = 0.1f;

	public static GameObject burstEffectPrefab;

	public static string explosionSound;

	public static string siphonLoopSound;

	public static string retractSound;

	private float duration;

	private float gainedHealth;

	private float gainedHealthFraction;

	private float healTimer;

	private bool burstPlayed;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration;
		GetItemStack();
		Vector3 position = base.attachedBody.transform.position;
		SphereSearch obj = new SphereSearch
		{
			origin = position,
			radius = baseSiphonRange,
			mask = LayerIndex.entityPrecise.mask
		};
		float num = base.attachedBody.healthComponent.fullCombinedHealth * damageFraction;
		TeamMask mask = default(TeamMask);
		mask.AddTeam(base.attachedBody.teamComponent.teamIndex);
		HurtBox[] hurtBoxes = obj.RefreshCandidates().FilterCandidatesByHurtBoxTeam(mask).OrderCandidatesByDistance()
			.FilterCandidatesByDistinctHurtBoxEntities()
			.GetHurtBoxes();
		foreach (HurtBox hurtBox in hurtBoxes)
		{
			if (hurtBox.healthComponent != base.attachedBody.healthComponent)
			{
				if (!burstPlayed)
				{
					burstPlayed = true;
					Util.PlaySound(explosionSound, base.gameObject);
					Util.PlaySound(siphonLoopSound, base.gameObject);
					EffectData effectData = new EffectData
					{
						scale = 1f,
						origin = position
					};
					effectData.SetHurtBoxReference(base.attachedBody.modelLocator.modelTransform.GetComponent<ChildLocator>().FindChild("Base").gameObject);
					EffectManager.SpawnEffect(burstEffectPrefab, effectData, transmit: false);
				}
				DamageInfo damageInfo = new DamageInfo
				{
					attacker = base.attachedBody.gameObject,
					inflictor = base.attachedBody.gameObject,
					crit = false,
					damage = num,
					damageColorIndex = DamageColorIndex.Default,
					damageType = DamageType.Generic,
					force = Vector3.zero,
					position = hurtBox.transform.position
				};
				hurtBox.healthComponent.TakeDamage(damageInfo);
				HurtBox hurtBoxReference = hurtBox;
				HurtBoxGroup hurtBoxGroup = hurtBox.hurtBoxGroup;
				for (int j = 0; (float)j < Mathf.Min(4f, base.attachedBody.radius * 2f); j++)
				{
					EffectData effectData2 = new EffectData
					{
						scale = 1f,
						origin = position,
						genericFloat = 3f
					};
					effectData2.SetHurtBoxReference(hurtBoxReference);
					GameObject obj2 = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/OrbEffects/SiphonOrbEffect");
					obj2.GetComponent<OrbEffect>().parentObjectTransform = base.attachedBody.mainHurtBox.transform;
					EffectManager.SpawnEffect(obj2, effectData2, transmit: true);
					hurtBoxReference = hurtBoxGroup.hurtBoxes[Random.Range(0, hurtBoxGroup.hurtBoxes.Length)];
				}
				gainedHealth += num;
			}
		}
		gainedHealthFraction = gainedHealth * healPulseFraction;
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		healTimer += GetDeltaTime();
		if (base.isAuthority && healTimer > duration * healPulseFraction && gainedHealth > 0f)
		{
			base.attachedBody.healthComponent.Heal(gainedHealthFraction, default(ProcChainMask));
			TurnOnHealingFX();
			healTimer = 0f;
			if (base.fixedAge >= duration)
			{
				Util.PlaySound(retractSound, base.gameObject);
				TurnOffHealingFX();
				outer.SetNextState(new RechargeState());
			}
		}
	}
}
