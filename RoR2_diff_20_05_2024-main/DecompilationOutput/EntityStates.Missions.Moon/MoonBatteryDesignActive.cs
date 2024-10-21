using System.Collections.Generic;
using HG;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Missions.Moon;

public class MoonBatteryDesignActive : MoonBatteryActive
{
	private TeamFilter teamFilter;

	public static GameObject pulsePrefab;

	public static float pulseInterval;

	public static BuffDef buffDef;

	public static float buffDuration;

	public static float baseForce;

	private float pulseTimer;

	public override void OnEnter()
	{
		base.OnEnter();
		teamFilter = GetComponent<TeamFilter>();
		pulseTimer = 0f;
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (NetworkServer.active)
		{
			pulseTimer -= GetDeltaTime();
			if (pulseTimer < 0f)
			{
				pulseTimer = pulseInterval;
				CreatePulseServer();
			}
		}
	}

	private void CreatePulseServer()
	{
		if (!FindModelChild("PulseOrigin"))
		{
			_ = base.transform;
		}
		GameObject obj = Object.Instantiate(pulsePrefab, base.transform.position, base.transform.rotation);
		SphereSearch sphereSearch = new SphereSearch();
		PulseController component = obj.GetComponent<PulseController>();
		component.performSearch += PerformPulseSearch;
		component.onPulseHit += OnPulseHit;
		component.StartPulseServer();
		NetworkServer.Spawn(obj);
		static void OnPulseHit(PulseController _, PulseController.PulseHit hitInfo)
		{
			HealthComponent healthComponent = hitInfo.hitObject as HealthComponent;
			if ((bool)healthComponent)
			{
				CharacterBody body = healthComponent.body;
				if ((bool)body.characterMotor && body.characterMotor.isGrounded)
				{
					Vector3 normalized = (hitInfo.hitPos - hitInfo.pulseOrigin).normalized;
					body.characterMotor.ApplyForce(normalized * (baseForce * hitInfo.hitSeverity));
					body.AddTimedBuff(buffDef, buffDuration);
				}
			}
		}
		void PerformPulseSearch(PulseController _, Vector3 origin, float radius, List<PulseController.PulseSearchResult> dest)
		{
			TeamMask none = TeamMask.none;
			none.AddTeam(teamFilter.teamIndex);
			sphereSearch.origin = origin;
			sphereSearch.radius = radius;
			sphereSearch.queryTriggerInteraction = QueryTriggerInteraction.Ignore;
			sphereSearch.mask = LayerIndex.entityPrecise.mask;
			sphereSearch.RefreshCandidates();
			sphereSearch.FilterCandidatesByHurtBoxTeam(none);
			sphereSearch.OrderCandidatesByDistance();
			sphereSearch.FilterCandidatesByDistinctHurtBoxEntities();
			List<HurtBox> list = CollectionPool<HurtBox, List<HurtBox>>.RentCollection();
			sphereSearch.GetHurtBoxes(list);
			for (int i = 0; i < list.Count; i++)
			{
				HurtBox hurtBox = list[i];
				if ((bool)hurtBox)
				{
					_ = hurtBox.healthComponent;
					if ((bool)hurtBox.healthComponent)
					{
						Vector3 vector = hurtBox.collider.ClosestPoint(origin);
						Vector3.Distance(origin, vector);
						PulseController.PulseSearchResult pulseSearchResult = default(PulseController.PulseSearchResult);
						pulseSearchResult.hitObject = hurtBox.healthComponent;
						pulseSearchResult.hitPos = vector;
						PulseController.PulseSearchResult item = pulseSearchResult;
						dest.Add(item);
					}
				}
			}
			list = CollectionPool<HurtBox, List<HurtBox>>.ReturnCollection(list);
		}
	}
}
