using System;
using System.Collections.ObjectModel;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.GrandParentBoss;

public class Offspring : BaseState
{
	[SerializeField]
	public GameObject SpawnerPodsPrefab;

	public static float randomRadius = 8f;

	public static float maxRange = 9999f;

	private Animator animator;

	private ChildLocator childLocator;

	private Transform modelTransform;

	private float duration;

	public static float baseDuration = 3.5f;

	public static string attackSoundString;

	private float summonInterval;

	private static float summonDuration = 3.26f;

	public static int maxSummonCount = 5;

	private float summonTimer;

	private bool isSummoning;

	private int summonCount;

	public static GameObject spawnEffect;

	private static int SpawnPodWarnStateHash = Animator.StringToHash("SpawnPodWarn");

	private static int SpawnPodWarnParamHash = Animator.StringToHash("spawnPodWarn.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		animator = GetModelAnimator();
		modelTransform = GetModelTransform();
		childLocator = modelTransform.GetComponent<ChildLocator>();
		duration = baseDuration;
		Util.PlaySound(attackSoundString, base.gameObject);
		summonInterval = summonDuration / (float)maxSummonCount;
		isSummoning = true;
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (isSummoning)
		{
			summonTimer += GetDeltaTime();
			if (NetworkServer.active && summonTimer > 0f && summonCount < maxSummonCount)
			{
				summonCount++;
				summonTimer -= summonInterval;
				SpawnPods();
			}
		}
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	private void SpawnPods()
	{
		Vector3 zero = Vector3.zero;
		Ray aimRay = GetAimRay();
		aimRay.origin += UnityEngine.Random.insideUnitSphere * randomRadius;
		if (Physics.Raycast(aimRay, out var hitInfo, (int)LayerIndex.world.mask))
		{
			zero = hitInfo.point;
		}
		zero = base.transform.position;
		Transform transform = FindTargetFarthest(zero, GetTeam());
		if ((bool)transform)
		{
			DirectorSpawnRequest directorSpawnRequest = new DirectorSpawnRequest(LegacyResourcesAPI.Load<SpawnCard>("SpawnCards/CharacterSpawnCards/cscParentPod"), new DirectorPlacementRule
			{
				placementMode = DirectorPlacementRule.PlacementMode.Approximate,
				minDistance = 3f,
				maxDistance = 20f,
				spawnOnTarget = transform
			}, RoR2Application.rng);
			directorSpawnRequest.summonerBodyObject = base.gameObject;
			directorSpawnRequest.onSpawnedServer = (Action<SpawnCard.SpawnResult>)Delegate.Combine(directorSpawnRequest.onSpawnedServer, (Action<SpawnCard.SpawnResult>)delegate(SpawnCard.SpawnResult spawnResult)
			{
				Inventory inventory = spawnResult.spawnedInstance.GetComponent<CharacterMaster>().inventory;
				Inventory inventory2 = base.characterBody.inventory;
				inventory.CopyEquipmentFrom(inventory2);
			});
			DirectorCore.instance.TrySpawnObject(directorSpawnRequest);
			PlayAnimation("Body", SpawnPodWarnStateHash, SpawnPodWarnParamHash, duration);
		}
	}

	private Transform FindTargetFarthest(Vector3 point, TeamIndex myTeam)
	{
		float num = 0f;
		Transform result = null;
		TeamMask enemyTeams = TeamMask.GetEnemyTeams(myTeam);
		for (TeamIndex teamIndex = TeamIndex.Neutral; teamIndex < TeamIndex.Count; teamIndex++)
		{
			if (!enemyTeams.HasTeam(teamIndex))
			{
				ReadOnlyCollection<TeamComponent> teamMembers = TeamComponent.GetTeamMembers(teamIndex);
				for (int i = 0; i < teamMembers.Count; i++)
				{
					float num2 = Vector3.Magnitude(teamMembers[i].transform.position - point);
					if (num2 > num && num2 < maxRange)
					{
						num = num2;
						result = teamMembers[i].transform;
					}
				}
			}
		}
		return result;
	}
}
