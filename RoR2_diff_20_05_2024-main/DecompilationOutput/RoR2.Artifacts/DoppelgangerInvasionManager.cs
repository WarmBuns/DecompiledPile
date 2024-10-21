using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Artifacts;

public class DoppelgangerInvasionManager : MonoBehaviour
{
	private readonly float invasionInterval = 600f;

	private int previousInvasionCycle;

	private ulong seed;

	private Run run;

	private Xoroshiro128Plus treasureRng;

	private PickupDropTable dropTable;

	private bool artifactIsEnabled => RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.shadowCloneArtifactDef);

	public static event Action<DamageReport> onDoppelgangerDeath;

	[SystemInitializer(new Type[] { typeof(ArtifactCatalog) })]
	private static void Init()
	{
		Run.onRunStartGlobal += OnRunStartGlobal;
	}

	private static void OnRunStartGlobal(Run run)
	{
		if (NetworkServer.active)
		{
			run.gameObject.AddComponent<DoppelgangerInvasionManager>();
		}
	}

	private void Start()
	{
		run = GetComponent<Run>();
		seed = run.seed;
		treasureRng = new Xoroshiro128Plus(seed);
		dropTable = LegacyResourcesAPI.Load<PickupDropTable>("DropTables/dtDoppelganger");
	}

	private void OnEnable()
	{
		GlobalEventManager.onCharacterDeathGlobal += OnCharacterDeathGlobal;
		ArtifactTrialMissionController.onShellTakeDamageServer += OnArtifactTrialShellTakeDamageServer;
	}

	private void OnDisable()
	{
		ArtifactTrialMissionController.onShellTakeDamageServer -= OnArtifactTrialShellTakeDamageServer;
		GlobalEventManager.onCharacterDeathGlobal -= OnCharacterDeathGlobal;
	}

	private void FixedUpdate()
	{
		int currentInvasionCycle = GetCurrentInvasionCycle();
		if (previousInvasionCycle < currentInvasionCycle)
		{
			previousInvasionCycle = currentInvasionCycle;
			if (artifactIsEnabled)
			{
				PerformInvasion(new Xoroshiro128Plus(seed + (ulong)currentInvasionCycle));
			}
		}
	}

	private void OnCharacterDeathGlobal(DamageReport damageReport)
	{
		Inventory inventory = damageReport.victimMaster?.inventory;
		if (!inventory)
		{
			return;
		}
		bool flag = damageReport.victimMaster.minionOwnership.ownerMaster;
		if (inventory.GetItemCount(RoR2Content.Items.InvadingDoppelganger) > 0 && inventory.GetItemCount(RoR2Content.Items.ExtraLife) == 0 && inventory.GetItemCount(DLC1Content.Items.ExtraLifeVoid) == 0 && !flag && !damageReport.victimBody.HasBuff(DLC2Content.Buffs.ExtraLifeBuff) && damageReport.victimBody.equipmentSlot.equipmentIndex != DLC2Content.Equipment.HealAndRevive.equipmentIndex)
		{
			DoppelgangerInvasionManager.onDoppelgangerDeath?.Invoke(damageReport);
			PickupIndex pickupIndex = dropTable.GenerateDrop(treasureRng);
			if (!(pickupIndex == PickupIndex.none))
			{
				PickupDropletController.CreatePickupDroplet(pickupIndex, damageReport.victimBody.corePosition, Vector3.up * 20f);
			}
		}
	}

	private void OnArtifactTrialShellTakeDamageServer(ArtifactTrialMissionController missionController, DamageReport damageReport)
	{
		if (artifactIsEnabled && damageReport.victim.alive)
		{
			PerformInvasion(new Xoroshiro128Plus((ulong)damageReport.victim.health));
		}
	}

	private int GetCurrentInvasionCycle()
	{
		return Mathf.FloorToInt(run.GetRunStopwatch() / invasionInterval);
	}

	public static void PerformInvasion(Xoroshiro128Plus rng)
	{
		for (int num = CharacterMaster.readOnlyInstancesList.Count - 1; num >= 0; num--)
		{
			CharacterMaster characterMaster = CharacterMaster.readOnlyInstancesList[num];
			if (characterMaster.teamIndex == TeamIndex.Player && (bool)characterMaster.playerCharacterMasterController)
			{
				CreateDoppelganger(characterMaster, rng);
			}
		}
	}

	private static void CreateDoppelganger(CharacterMaster srcCharacterMaster, Xoroshiro128Plus rng)
	{
		SpawnCard spawnCard = DoppelgangerSpawnCard.FromMaster(srcCharacterMaster);
		if (!spawnCard)
		{
			return;
		}
		Transform coreTransform;
		DirectorCore.MonsterSpawnDistance input;
		if ((bool)TeleporterInteraction.instance)
		{
			coreTransform = TeleporterInteraction.instance.transform;
			input = DirectorCore.MonsterSpawnDistance.Close;
		}
		else
		{
			coreTransform = srcCharacterMaster.GetBody().coreTransform;
			input = DirectorCore.MonsterSpawnDistance.Far;
		}
		DirectorPlacementRule directorPlacementRule = new DirectorPlacementRule
		{
			spawnOnTarget = coreTransform,
			placementMode = DirectorPlacementRule.PlacementMode.NearestNode
		};
		DirectorCore.GetMonsterSpawnDistance(input, out directorPlacementRule.minDistance, out directorPlacementRule.maxDistance);
		DirectorSpawnRequest directorSpawnRequest = new DirectorSpawnRequest(spawnCard, directorPlacementRule, rng);
		directorSpawnRequest.teamIndexOverride = TeamIndex.Monster;
		directorSpawnRequest.ignoreTeamMemberLimit = true;
		CombatSquad combatSquad = null;
		directorSpawnRequest.onSpawnedServer = (Action<SpawnCard.SpawnResult>)Delegate.Combine(directorSpawnRequest.onSpawnedServer, (Action<SpawnCard.SpawnResult>)delegate(SpawnCard.SpawnResult result)
		{
			if (!combatSquad)
			{
				combatSquad = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/Encounters/ShadowCloneEncounter")).GetComponent<CombatSquad>();
			}
			combatSquad.AddMember(result.spawnedInstance.GetComponent<CharacterMaster>());
		});
		DirectorCore.instance.TrySpawnObject(directorSpawnRequest);
		if ((bool)combatSquad)
		{
			NetworkServer.Spawn(combatSquad.gameObject);
		}
		UnityEngine.Object.Destroy(spawnCard);
	}
}
