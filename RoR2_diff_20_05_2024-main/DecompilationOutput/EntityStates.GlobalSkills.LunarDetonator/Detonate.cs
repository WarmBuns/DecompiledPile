using System;
using System.Collections.Generic;
using System.Linq;
using RoR2;
using RoR2.Orbs;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.GlobalSkills.LunarDetonator;

public class Detonate : BaseState
{
	private class DetonationController
	{
		public HurtBox[] detonationTargets;

		public CharacterBody characterBody;

		public float damageStat;

		public bool isCrit;

		public float interval;

		private int i;

		private float timer;

		private bool _active;

		public bool active
		{
			get
			{
				return _active;
			}
			set
			{
				if (_active != value)
				{
					_active = value;
					if (_active)
					{
						RoR2Application.onFixedUpdate += FixedUpdate;
					}
					else
					{
						RoR2Application.onFixedUpdate -= FixedUpdate;
					}
				}
			}
		}

		private void FixedUpdate()
		{
			if (!characterBody || !characterBody.healthComponent || !characterBody.healthComponent.alive)
			{
				active = false;
				return;
			}
			timer -= Time.deltaTime;
			if (!(timer <= 0f))
			{
				return;
			}
			for (timer = interval; i < detonationTargets.Length; i++)
			{
				try
				{
					HurtBox a = null;
					Util.Swap(ref a, ref detonationTargets[i]);
					if (DoDetonation(a))
					{
						break;
					}
				}
				catch (Exception message)
				{
					Debug.LogError(message);
				}
			}
			if (i >= detonationTargets.Length)
			{
				active = false;
			}
		}

		private bool DoDetonation(HurtBox targetHurtBox)
		{
			if (!targetHurtBox)
			{
				return false;
			}
			HealthComponent healthComponent = targetHurtBox.healthComponent;
			if (!healthComponent)
			{
				return false;
			}
			CharacterBody body = healthComponent.body;
			if (!body)
			{
				return false;
			}
			if (body.GetBuffCount(RoR2Content.Buffs.LunarDetonationCharge) <= 0)
			{
				return false;
			}
			LunarDetonatorOrb lunarDetonatorOrb = new LunarDetonatorOrb();
			lunarDetonatorOrb.origin = characterBody.corePosition;
			lunarDetonatorOrb.target = targetHurtBox;
			lunarDetonatorOrb.attacker = characterBody.gameObject;
			lunarDetonatorOrb.baseDamage = damageStat * baseDamageCoefficient;
			lunarDetonatorOrb.damagePerStack = damageStat * damageCoefficientPerStack;
			lunarDetonatorOrb.damageColorIndex = DamageColorIndex.Default;
			lunarDetonatorOrb.isCrit = isCrit;
			lunarDetonatorOrb.procChainMask = default(ProcChainMask);
			lunarDetonatorOrb.procCoefficient = 1f;
			lunarDetonatorOrb.detonationEffectPrefab = detonationEffectPrefab;
			lunarDetonatorOrb.travelSpeed = 120f;
			lunarDetonatorOrb.orbEffectPrefab = orbEffectPrefab;
			OrbManager.instance.AddOrb(lunarDetonatorOrb);
			return true;
		}
	}

	public static float baseDuration;

	public static float baseDamageCoefficient;

	public static float damageCoefficientPerStack;

	public static float procCoefficient;

	public static float detonationInterval;

	public static GameObject detonationEffectPrefab;

	public static GameObject orbEffectPrefab;

	public static GameObject enterEffectPrefab;

	public static string enterSoundString;

	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	[SerializeField]
	public string playbackRateParam;

	private float duration;

	private HurtBox[] detonationTargets;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		EffectManager.SimpleImpactEffect(enterEffectPrefab, base.characterBody.corePosition, Vector3.up, transmit: false);
		Util.PlaySound(enterSoundString, base.gameObject);
		if (NetworkServer.active)
		{
			BullseyeSearch bullseyeSearch = new BullseyeSearch();
			bullseyeSearch.filterByDistinctEntity = true;
			bullseyeSearch.filterByLoS = false;
			bullseyeSearch.maxDistanceFilter = float.PositiveInfinity;
			bullseyeSearch.minDistanceFilter = 0f;
			bullseyeSearch.minAngleFilter = 0f;
			bullseyeSearch.maxAngleFilter = 180f;
			bullseyeSearch.sortMode = BullseyeSearch.SortMode.Distance;
			bullseyeSearch.teamMaskFilter = TeamMask.GetUnprotectedTeams(GetTeam());
			bullseyeSearch.searchOrigin = base.characterBody.corePosition;
			bullseyeSearch.viewer = null;
			bullseyeSearch.RefreshCandidates();
			bullseyeSearch.FilterOutGameObject(base.gameObject);
			IEnumerable<HurtBox> results = bullseyeSearch.GetResults();
			detonationTargets = results.ToArray();
			new DetonationController
			{
				characterBody = base.characterBody,
				interval = detonationInterval,
				detonationTargets = detonationTargets,
				damageStat = damageStat,
				isCrit = RollCrit(),
				active = true
			};
		}
		PlayAnimation(animationLayerName, animationStateName, playbackRateParam, duration);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && base.fixedAge >= duration)
		{
			outer.SetNextStateToMain();
		}
	}
}
