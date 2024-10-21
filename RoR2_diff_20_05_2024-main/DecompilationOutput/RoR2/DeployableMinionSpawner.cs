using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Events;

namespace RoR2;

public class DeployableMinionSpawner : IDisposable
{
	public float respawnInterval = 30f;

	[CanBeNull]
	public SpawnCard spawnCard;

	public float minSpawnDistance = 3f;

	public float maxSpawnDistance = 40f;

	[NotNull]
	private Xoroshiro128Plus rng;

	private float respawnStopwatch = float.PositiveInfinity;

	[CanBeNull]
	public CharacterMaster ownerMaster { get; }

	public DeployableSlot deployableSlot { get; }

	public event Action<SpawnCard.SpawnResult> onMinionSpawnedServer;

	public DeployableMinionSpawner([CanBeNull] CharacterMaster ownerMaster, DeployableSlot deployableSlot, [NotNull] Xoroshiro128Plus rng)
	{
		this.ownerMaster = ownerMaster;
		this.deployableSlot = deployableSlot;
		this.rng = rng;
		RoR2Application.onFixedUpdate += FixedUpdate;
	}

	public void Dispose()
	{
		RoR2Application.onFixedUpdate -= FixedUpdate;
	}

	private void FixedUpdate()
	{
		if (!ownerMaster)
		{
			return;
		}
		float fixedDeltaTime = Time.fixedDeltaTime;
		int deployableCount = ownerMaster.GetDeployableCount(deployableSlot);
		int deployableSameSlotLimit = ownerMaster.GetDeployableSameSlotLimit(deployableSlot);
		if (deployableCount >= deployableSameSlotLimit)
		{
			return;
		}
		respawnStopwatch += fixedDeltaTime;
		if (!(respawnStopwatch >= respawnInterval))
		{
			return;
		}
		CharacterBody body = ownerMaster.GetBody();
		if ((bool)body && (bool)spawnCard)
		{
			try
			{
				SpawnMinion(spawnCard, body);
			}
			catch (Exception message)
			{
				Debug.LogError(message);
			}
			respawnStopwatch = 0f;
		}
	}

	private void SpawnMinion([NotNull] SpawnCard spawnCard, [NotNull] CharacterBody spawnTarget)
	{
		DirectorSpawnRequest directorSpawnRequest = new DirectorSpawnRequest(spawnCard, new DirectorPlacementRule
		{
			placementMode = DirectorPlacementRule.PlacementMode.Approximate,
			minDistance = minSpawnDistance,
			maxDistance = maxSpawnDistance,
			spawnOnTarget = spawnTarget.transform
		}, RoR2Application.rng);
		directorSpawnRequest.summonerBodyObject = ownerMaster.GetBodyObject();
		directorSpawnRequest.onSpawnedServer = OnMinionSpawnedServerInternal;
		DirectorCore.instance.TrySpawnObject(directorSpawnRequest);
	}

	private void OnMinionSpawnedServerInternal([NotNull] SpawnCard.SpawnResult spawnResult)
	{
		GameObject spawnedInstance = spawnResult.spawnedInstance;
		if ((bool)spawnedInstance)
		{
			CharacterMaster component = spawnedInstance.GetComponent<CharacterMaster>();
			Deployable deployable = spawnedInstance.AddComponent<Deployable>();
			deployable.onUndeploy = deployable.onUndeploy ?? new UnityEvent();
			if ((bool)component)
			{
				deployable.onUndeploy.AddListener(component.TrueKill);
			}
			else
			{
				deployable.onUndeploy.AddListener(delegate
				{
					UnityEngine.Object.Destroy(spawnedInstance);
				});
			}
			ownerMaster.AddDeployable(deployable, deployableSlot);
		}
		this.onMinionSpawnedServer?.Invoke(spawnResult);
	}
}
