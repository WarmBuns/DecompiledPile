using System.Collections.Generic;
using HG;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class RadialForce : MonoBehaviour
{
	public float radius;

	public float damping = 0.2f;

	public float forceMagnitude;

	public float forceCoefficientAtEdge = 0.5f;

	public TetherVfxOrigin tetherVfxOrigin;

	private SphereSearch sphereSearch;

	protected new Transform transform { get; private set; }

	protected TeamFilter teamFilter { get; private set; }

	protected void Awake()
	{
		transform = GetComponent<Transform>();
		teamFilter = GetComponent<TeamFilter>();
		sphereSearch = new SphereSearch();
	}

	protected void FixedUpdate()
	{
		List<HurtBox> list = CollectionPool<HurtBox, List<HurtBox>>.RentCollection();
		SearchForTargets(list);
		int i = 0;
		for (int count = list.Count; i < count; i++)
		{
			ApplyPullToHurtBox(list[i]);
		}
		if ((bool)tetherVfxOrigin)
		{
			List<Transform> list2 = CollectionPool<Transform, List<Transform>>.RentCollection();
			int j = 0;
			for (int count2 = list.Count; j < count2; j++)
			{
				HurtBox hurtBox = list[j];
				if (!hurtBox)
				{
					continue;
				}
				Transform item = hurtBox.transform;
				HealthComponent healthComponent = hurtBox.healthComponent;
				if ((bool)healthComponent)
				{
					Transform coreTransform = healthComponent.body.coreTransform;
					if ((bool)coreTransform)
					{
						item = coreTransform;
					}
				}
				list2.Add(item);
			}
			tetherVfxOrigin.SetTetheredTransforms(list2);
			CollectionPool<Transform, List<Transform>>.ReturnCollection(list2);
		}
		CollectionPool<HurtBox, List<HurtBox>>.ReturnCollection(list);
	}

	protected void SearchForTargets(List<HurtBox> dest)
	{
		sphereSearch.mask = LayerIndex.entityPrecise.mask;
		sphereSearch.origin = transform.position;
		sphereSearch.radius = radius;
		sphereSearch.queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
		sphereSearch.RefreshCandidates();
		sphereSearch.FilterCandidatesByHurtBoxTeam(TeamMask.GetEnemyTeams(teamFilter.teamIndex));
		sphereSearch.OrderCandidatesByDistance();
		sphereSearch.FilterCandidatesByDistinctHurtBoxEntities();
		sphereSearch.GetHurtBoxes(dest);
		sphereSearch.ClearCandidates();
	}

	protected void ApplyPullToHurtBox(HurtBox hurtBox)
	{
		if (!hurtBox)
		{
			return;
		}
		HealthComponent healthComponent = hurtBox.healthComponent;
		if (!healthComponent || !healthComponent.body || !hurtBox.transform || !transform || !NetworkServer.active)
		{
			return;
		}
		CharacterMotor characterMotor = healthComponent.body.characterMotor;
		Vector3 vector = hurtBox.transform.position - transform.position;
		float num = 1f - Mathf.Clamp(vector.magnitude / radius, 0f, 1f - forceCoefficientAtEdge);
		vector = vector.normalized * forceMagnitude * (1f - num);
		Vector3 vector2 = Vector3.zero;
		float num2 = 0f;
		if ((bool)characterMotor)
		{
			vector2 = characterMotor.velocity;
			num2 = characterMotor.mass;
		}
		else
		{
			Rigidbody rigidbody = healthComponent.body.rigidbody;
			if ((bool)rigidbody)
			{
				vector2 = rigidbody.velocity;
				num2 = rigidbody.mass;
			}
		}
		vector2.y += Physics.gravity.y * Time.fixedDeltaTime;
		healthComponent.TakeDamageForce(vector - vector2 * damping * num2 * num, alwaysApply: true);
	}
}
