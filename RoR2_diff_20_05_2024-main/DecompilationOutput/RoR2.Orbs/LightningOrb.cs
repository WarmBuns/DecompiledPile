using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RoR2.Orbs;

public class LightningOrb : Orb
{
	public enum LightningType
	{
		Ukulele,
		Tesla,
		BFG,
		TreePoisonDart,
		HuntressGlaive,
		Loader,
		RazorWire,
		CrocoDisease,
		MageLightning,
		AntlerShield,
		BeadDamage,
		Count
	}

	public float speed = 100f;

	public float damageValue;

	public GameObject attacker;

	public GameObject inflictor;

	public int bouncesRemaining;

	public List<HealthComponent> bouncedObjects;

	public TeamIndex teamIndex;

	public bool isCrit;

	public ProcChainMask procChainMask;

	public float procCoefficient = 1f;

	public DamageColorIndex damageColorIndex;

	public float range = 20f;

	public float damageCoefficientPerBounce = 1f;

	public int targetsToFindPerBounce = 1;

	public DamageTypeCombo damageType = DamageType.Generic;

	private bool canBounceOnSameTarget;

	private bool failedToKill;

	public LightningType lightningType;

	private BullseyeSearch search;

	public static event Action<LightningOrb> onLightningOrbKilledOnAllBounces;

	public override void Begin()
	{
		base.duration = 0.1f;
		string path = null;
		switch (lightningType)
		{
		case LightningType.Ukulele:
			path = "Prefabs/Effects/OrbEffects/LightningOrbEffect";
			break;
		case LightningType.Tesla:
			path = "Prefabs/Effects/OrbEffects/TeslaOrbEffect";
			break;
		case LightningType.BFG:
			path = "Prefabs/Effects/OrbEffects/BeamSphereOrbEffect";
			base.duration = 0.4f;
			break;
		case LightningType.HuntressGlaive:
			path = "Prefabs/Effects/OrbEffects/HuntressGlaiveOrbEffect";
			base.duration = base.distanceToTarget / speed;
			canBounceOnSameTarget = true;
			break;
		case LightningType.TreePoisonDart:
			path = "Prefabs/Effects/OrbEffects/TreePoisonDartOrbEffect";
			speed = 40f;
			base.duration = base.distanceToTarget / speed;
			break;
		case LightningType.Loader:
			path = "Prefabs/Effects/OrbEffects/LoaderLightningOrbEffect";
			break;
		case LightningType.RazorWire:
			path = "Prefabs/Effects/OrbEffects/RazorwireOrbEffect";
			base.duration = 0.2f;
			break;
		case LightningType.CrocoDisease:
			path = "Prefabs/Effects/OrbEffects/CrocoDiseaseOrbEffect";
			base.duration = 0.6f;
			targetsToFindPerBounce = 2;
			break;
		case LightningType.MageLightning:
			path = "Prefabs/Effects/OrbEffects/MageLightningOrbEffect";
			base.duration = 0.1f;
			break;
		case LightningType.AntlerShield:
			path = "Prefabs/Effects/OrbEffects/NegateAttackOrbEffect";
			base.duration = 0.2f;
			break;
		case LightningType.BeadDamage:
			path = "Prefabs/Effects/OrbEffects/BeadDamageOrbEffect";
			base.duration = 0.2f;
			break;
		}
		EffectData effectData = new EffectData
		{
			origin = origin,
			genericFloat = base.duration
		};
		effectData.SetHurtBoxReference(target);
		EffectManager.SpawnEffect(OrbStorageUtility.Get(path), effectData, transmit: true);
	}

	public override void OnArrival()
	{
		if (!target)
		{
			return;
		}
		HealthComponent healthComponent = target.healthComponent;
		if ((bool)healthComponent)
		{
			DamageInfo damageInfo = new DamageInfo();
			damageInfo.damage = damageValue;
			damageInfo.attacker = attacker;
			damageInfo.inflictor = inflictor;
			damageInfo.force = Vector3.zero;
			damageInfo.crit = isCrit;
			damageInfo.procChainMask = procChainMask;
			damageInfo.procCoefficient = procCoefficient;
			damageInfo.position = target.transform.position;
			damageInfo.damageColorIndex = damageColorIndex;
			damageInfo.damageType = damageType;
			healthComponent.TakeDamage(damageInfo);
			GlobalEventManager.instance.OnHitEnemy(damageInfo, healthComponent.gameObject);
			GlobalEventManager.instance.OnHitAll(damageInfo, healthComponent.gameObject);
		}
		failedToKill |= !healthComponent || healthComponent.alive;
		if (bouncesRemaining > 0)
		{
			for (int i = 0; i < targetsToFindPerBounce; i++)
			{
				if (bouncedObjects != null)
				{
					if (canBounceOnSameTarget)
					{
						bouncedObjects.Clear();
					}
					bouncedObjects.Add(target.healthComponent);
				}
				HurtBox hurtBox = PickNextTarget(target.transform.position);
				if ((bool)hurtBox)
				{
					LightningOrb lightningOrb = new LightningOrb();
					lightningOrb.search = search;
					lightningOrb.origin = target.transform.position;
					lightningOrb.target = hurtBox;
					lightningOrb.attacker = attacker;
					lightningOrb.inflictor = inflictor;
					lightningOrb.teamIndex = teamIndex;
					lightningOrb.damageValue = damageValue * damageCoefficientPerBounce;
					lightningOrb.bouncesRemaining = bouncesRemaining - 1;
					lightningOrb.isCrit = isCrit;
					lightningOrb.bouncedObjects = bouncedObjects;
					lightningOrb.lightningType = lightningType;
					lightningOrb.procChainMask = procChainMask;
					lightningOrb.procCoefficient = procCoefficient;
					lightningOrb.damageColorIndex = damageColorIndex;
					lightningOrb.damageCoefficientPerBounce = damageCoefficientPerBounce;
					lightningOrb.speed = speed;
					lightningOrb.range = range;
					lightningOrb.damageType = damageType;
					lightningOrb.failedToKill = failedToKill;
					OrbManager.instance.AddOrb(lightningOrb);
				}
			}
		}
		else if (!failedToKill)
		{
			LightningOrb.onLightningOrbKilledOnAllBounces?.Invoke(this);
		}
	}

	public HurtBox PickNextTarget(Vector3 position)
	{
		if (search == null)
		{
			search = new BullseyeSearch();
		}
		search.searchOrigin = position;
		search.searchDirection = Vector3.zero;
		search.teamMaskFilter = TeamMask.allButNeutral;
		search.teamMaskFilter.RemoveTeam(teamIndex);
		search.filterByLoS = false;
		search.sortMode = BullseyeSearch.SortMode.Distance;
		search.maxDistanceFilter = range;
		search.RefreshCandidates();
		HurtBox hurtBox = (from v in search.GetResults()
			where !bouncedObjects.Contains(v.healthComponent)
			select v).FirstOrDefault();
		if ((bool)hurtBox)
		{
			bouncedObjects.Add(hurtBox.healthComponent);
		}
		return hurtBox;
	}
}
