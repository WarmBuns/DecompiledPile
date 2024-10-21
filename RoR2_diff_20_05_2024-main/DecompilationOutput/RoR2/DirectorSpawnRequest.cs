using System;
using UnityEngine;

namespace RoR2;

public class DirectorSpawnRequest
{
	public SpawnCard spawnCard;

	public DirectorPlacementRule placementRule;

	public Xoroshiro128Plus rng;

	public GameObject summonerBodyObject;

	public TeamIndex? teamIndexOverride;

	public bool ignoreTeamMemberLimit;

	public Action<SpawnCard.SpawnResult> onSpawnedServer;

	public DirectorSpawnRequest(SpawnCard spawnCard, DirectorPlacementRule placementRule, Xoroshiro128Plus rng)
	{
		this.spawnCard = spawnCard;
		this.placementRule = placementRule;
		this.rng = rng;
	}
}
