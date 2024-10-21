using System.Collections.Generic;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.TeleporterHealNovaController;

public class TeleporterHealNovaPulse : BaseState
{
	private class HealPulse
	{
		private readonly List<HealthComponent> healedTargets = new List<HealthComponent>();

		private readonly SphereSearch sphereSearch;

		private float rate;

		private float t;

		private float finalRadius;

		private float healFractionValue;

		private TeamMask teamMask;

		private readonly List<HurtBox> hurtBoxesList = new List<HurtBox>();

		public bool isFinished => t >= 1f;

		public HealPulse(Vector3 origin, float finalRadius, float healFractionValue, float duration, TeamIndex teamIndex)
		{
			sphereSearch = new SphereSearch
			{
				mask = LayerIndex.entityPrecise.mask,
				origin = origin,
				queryTriggerInteraction = QueryTriggerInteraction.Collide,
				radius = 0f
			};
			this.finalRadius = finalRadius;
			this.healFractionValue = healFractionValue;
			rate = 1f / duration;
			teamMask = default(TeamMask);
			teamMask.AddTeam(teamIndex);
		}

		public void Update(float deltaTime)
		{
			t += rate * deltaTime;
			t = ((t > 1f) ? 1f : t);
			sphereSearch.radius = finalRadius * novaRadiusCurve.Evaluate(t);
			sphereSearch.RefreshCandidates().FilterCandidatesByHurtBoxTeam(teamMask).FilterCandidatesByDistinctHurtBoxEntities()
				.GetHurtBoxes(hurtBoxesList);
			int i = 0;
			for (int count = hurtBoxesList.Count; i < count; i++)
			{
				HealthComponent healthComponent = hurtBoxesList[i].healthComponent;
				if (!healedTargets.Contains(healthComponent))
				{
					healedTargets.Add(healthComponent);
					HealTarget(healthComponent);
				}
			}
			hurtBoxesList.Clear();
		}

		private void HealTarget(HealthComponent target)
		{
			target.HealFraction(healFractionValue, default(ProcChainMask));
			Util.PlaySound("Play_item_proc_TPhealingNova_hitPlayer", target.gameObject);
		}
	}

	public static AnimationCurve novaRadiusCurve;

	public static float duration;

	private Transform effectTransform;

	private HealPulse healPulse;

	private float radius;

	public override void OnEnter()
	{
		base.OnEnter();
		if ((bool)base.transform.parent)
		{
			HoldoutZoneController componentInParent = base.gameObject.GetComponentInParent<HoldoutZoneController>();
			if ((bool)componentInParent)
			{
				radius = componentInParent.currentRadius;
			}
		}
		TeamFilter component = GetComponent<TeamFilter>();
		TeamIndex teamIndex = (component ? component.teamIndex : TeamIndex.None);
		if (NetworkServer.active)
		{
			healPulse = new HealPulse(base.transform.position, radius, 0.5f, duration, teamIndex);
		}
		effectTransform = base.transform.Find("PulseEffect");
		if ((bool)effectTransform)
		{
			effectTransform.gameObject.SetActive(value: true);
		}
	}

	public override void OnExit()
	{
		if ((bool)effectTransform)
		{
			effectTransform.gameObject.SetActive(value: false);
		}
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (NetworkServer.active)
		{
			healPulse.Update(GetDeltaTime());
			if (duration < base.fixedAge)
			{
				EntityState.Destroy(outer.gameObject);
			}
		}
	}

	public override void Update()
	{
		if ((bool)effectTransform)
		{
			float num = radius * novaRadiusCurve.Evaluate(base.fixedAge / duration);
			effectTransform.localScale = new Vector3(num, num, num);
		}
	}
}
