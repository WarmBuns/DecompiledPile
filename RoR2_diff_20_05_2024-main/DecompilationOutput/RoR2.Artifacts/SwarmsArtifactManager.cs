using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Artifacts;

public static class SwarmsArtifactManager
{
	private const int swarmSpawnCount = 2;

	private static bool inSpawn;

	private static ArtifactDef myArtifact => RoR2Content.Artifacts.swarmsArtifactDef;

	[SystemInitializer(new Type[] { typeof(ArtifactCatalog) })]
	private static void Init()
	{
		RunArtifactManager.onArtifactEnabledGlobal += OnArtifactEnabled;
		RunArtifactManager.onArtifactDisabledGlobal += OnArtifactDisabled;
	}

	private static void OnArtifactEnabled(RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
	{
		if (!(artifactDef != myArtifact) && NetworkServer.active)
		{
			SpawnCard.onSpawnedServerGlobal += OnSpawnCardOnSpawnedServerGlobal;
		}
	}

	private static void OnArtifactDisabled(RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
	{
		if (!(artifactDef != myArtifact))
		{
			SpawnCard.onSpawnedServerGlobal -= OnSpawnCardOnSpawnedServerGlobal;
		}
	}

	private static void OnSpawnCardOnSpawnedServerGlobal(SpawnCard.SpawnResult result)
	{
		if (!result.success || !(result.spawnRequest.spawnCard as CharacterSpawnCard) || result.spawnRequest.teamIndexOverride == TeamIndex.Player)
		{
			return;
		}
		CharacterMaster component = result.spawnedInstance.gameObject.GetComponent<CharacterMaster>();
		if ((bool)component)
		{
			component.inventory.GiveItem(RoR2Content.Items.CutHp);
			GameObject bodyObject = component.GetBodyObject();
			if ((bool)bodyObject)
			{
				DeathRewards component2 = bodyObject.GetComponent<DeathRewards>();
				if ((bool)component2)
				{
					component2.spawnValue = (int)Mathf.Max(1f, (float)component2.spawnValue / 2f);
					component2.expReward = (uint)Mathf.Ceil((float)component2.expReward / 2f);
					component2.goldReward = (uint)Mathf.Ceil((float)component2.goldReward / 2f);
				}
			}
		}
		if (inSpawn)
		{
			return;
		}
		for (int i = 1; i < 2; i++)
		{
			inSpawn = true;
			try
			{
				DirectorCore.instance.TrySpawnObject(result.spawnRequest);
			}
			catch (Exception message)
			{
				Debug.LogError(message);
			}
			inSpawn = false;
		}
	}
}
