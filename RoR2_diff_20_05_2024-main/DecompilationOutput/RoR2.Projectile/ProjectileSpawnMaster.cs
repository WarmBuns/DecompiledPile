using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Projectile;

[RequireComponent(typeof(ProjectileController))]
public class ProjectileSpawnMaster : MonoBehaviour, IProjectileImpactBehavior
{
	[SerializeField]
	private float maxLifetime = 10f;

	[SerializeField]
	private CharacterSpawnCard spawnCard;

	[SerializeField]
	public DeployableSlot deployableSlot = DeployableSlot.None;

	private float stopwatch;

	private bool isAlive = true;

	public void OnProjectileImpact(ProjectileImpactInfo impactInfo)
	{
		if (isAlive)
		{
			SpawnMaster();
			isAlive = false;
		}
	}

	protected void FixedUpdate()
	{
		stopwatch += Time.fixedDeltaTime;
		if (NetworkServer.active && (!isAlive || stopwatch > maxLifetime))
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	public void SpawnMaster()
	{
		ProjectileController component = GetComponent<ProjectileController>();
		if (!component || !component.owner)
		{
			return;
		}
		CharacterBody component2 = component.owner.GetComponent<CharacterBody>();
		if (!component2 || !component2.master || !spawnCard)
		{
			return;
		}
		CharacterMaster characterMaster = component2.master;
		if (!characterMaster || (deployableSlot != DeployableSlot.None && characterMaster.IsDeployableLimited(deployableSlot)))
		{
			return;
		}
		Transform spawnOnTarget = base.transform;
		DirectorPlacementRule directorPlacementRule = new DirectorPlacementRule
		{
			spawnOnTarget = spawnOnTarget,
			placementMode = DirectorPlacementRule.PlacementMode.Direct
		};
		DirectorCore.GetMonsterSpawnDistance(DirectorCore.MonsterSpawnDistance.Close, out directorPlacementRule.minDistance, out directorPlacementRule.maxDistance);
		DirectorSpawnRequest directorSpawnRequest = new DirectorSpawnRequest(spawnCard, directorPlacementRule, new Xoroshiro128Plus(Run.instance.seed + (ulong)Run.instance.fixedTime));
		directorSpawnRequest.teamIndexOverride = characterMaster.teamIndex;
		directorSpawnRequest.ignoreTeamMemberLimit = false;
		directorSpawnRequest.summonerBodyObject = component.owner;
		if (deployableSlot != DeployableSlot.None)
		{
			directorSpawnRequest.onSpawnedServer = (Action<SpawnCard.SpawnResult>)Delegate.Combine(directorSpawnRequest.onSpawnedServer, (Action<SpawnCard.SpawnResult>)delegate(SpawnCard.SpawnResult result)
			{
				if (result.success && (bool)result.spawnedInstance)
				{
					result.spawnedInstance.GetComponent<CharacterMaster>();
					Deployable deployable = result.spawnedInstance.AddComponent<Deployable>();
					characterMaster.AddDeployable(deployable, deployableSlot);
				}
			});
		}
		DirectorCore.instance.TrySpawnObject(directorSpawnRequest);
	}
}
