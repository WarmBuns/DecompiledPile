using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using HG;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public static class GoldTitanManager
{
	private class RemoveItemStealOnDeath : MonoBehaviour
	{
	}

	private static CharacterSpawnCard goldTitanSpawnCard;

	private static ItemIndex goldTitanItemIndex;

	private static MasterCatalog.MasterIndex brotherHurtMasterIndex;

	private static MasterCatalog.MasterIndex falseSonBossLunarShardBrokenMasterIndex;

	private static bool isFalseSonBossLunarShardBrokenMaster = false;

	private static readonly Xoroshiro128Plus rng = new Xoroshiro128Plus(0uL);

	private static object currentChanneler;

	private static readonly List<CharacterMaster> currentTitans = new List<CharacterMaster>();

	private static readonly Func<ItemIndex, bool> goldTitanItemFilterDelegate = GoldTitanItemFilter;

	private static readonly Func<ItemIndex, bool> noItemFilterDelegate = NoItemFilter;

	private static readonly Func<CharacterMaster, bool> allCharacterMastersFilterDelegate = AllCharacterMastersFilter;

	private static event Action onChannelEnd;

	public static event Action onGoldTitanSpawned;

	[SystemInitializer(new Type[]
	{
		typeof(ItemCatalog),
		typeof(MasterCatalog)
	})]
	private static void Init()
	{
		Run.onRunStartGlobal += OnRunStartGlobal;
		Run.onRunDestroyGlobal += OnRunDestroyGlobal;
		TeleporterInteraction.onTeleporterBeginChargingGlobal += OnTeleporterBeginChargingGlobal;
		TeleporterInteraction.onTeleporterChargedGlobal += OnTeleporterChargedGlobal;
		BossGroup.onBossGroupStartServer += OnBossGroupStartServer;
		LegacyResourcesAPI.LoadAsyncCallback("SpawnCards/CharacterSpawnCards/cscTitanGoldAlly", delegate(CharacterSpawnCard operationResult)
		{
			goldTitanSpawnCard = operationResult;
		});
		goldTitanItemIndex = RoR2Content.Items.TitanGoldDuringTP?.itemIndex ?? ItemIndex.None;
		brotherHurtMasterIndex = MasterCatalog.FindMasterIndex("BrotherHurtMaster");
		falseSonBossLunarShardBrokenMasterIndex = MasterCatalog.FindMasterIndex("FalseSonBossLunarShardBrokenMaster");
	}

	private static void CalcTitanPowerAndBestTeam(out int totalItemCount, out TeamIndex teamIndex)
	{
		TeamIndex teamIndex2 = TeamIndex.None;
		int num = 0;
		totalItemCount = 0;
		for (TeamIndex teamIndex3 = TeamIndex.Neutral; teamIndex3 < TeamIndex.Count; teamIndex3++)
		{
			int itemCountForTeam = Util.GetItemCountForTeam(teamIndex3, goldTitanItemIndex, requiresAlive: true);
			if (itemCountForTeam > num)
			{
				num = itemCountForTeam;
				teamIndex2 = teamIndex3;
			}
			totalItemCount += itemCountForTeam;
		}
		teamIndex = teamIndex2;
	}

	private static void KillTitansInList(List<CharacterMaster> titansList)
	{
		try
		{
			foreach (CharacterMaster titans in titansList)
			{
				if ((bool)titans)
				{
					titans.TrueKill();
				}
			}
		}
		catch (Exception message)
		{
			Debug.LogError(message);
		}
	}

	private static bool GoldTitanItemFilter(ItemIndex itemIndex)
	{
		return itemIndex == goldTitanItemIndex;
	}

	private static bool NoItemFilter(ItemIndex itemIndex)
	{
		return false;
	}

	private static bool AllCharacterMastersFilter(CharacterMaster characterMaster)
	{
		return true;
	}

	private static bool TryStartChannelingTitansServer(object channeler, Vector3 approximatePosition, Vector3? lookAtPosition = null, Action channelEndCallback = null)
	{
		CalcTitanPowerAndBestTeam(out var totalItemCount, out var _);
		if (totalItemCount <= 0)
		{
			return false;
		}
		List<CharacterMaster> newTitans = CollectionPool<CharacterMaster, List<CharacterMaster>>.RentCollection();
		float currentBoostHpCoefficient;
		float currentBoostDamageCoefficient;
		try
		{
			DirectorPlacementRule placementRule = new DirectorPlacementRule
			{
				placementMode = DirectorPlacementRule.PlacementMode.NearestNode,
				minDistance = 20f,
				maxDistance = 130f,
				position = approximatePosition
			};
			DirectorSpawnRequest directorSpawnRequest = new DirectorSpawnRequest(goldTitanSpawnCard, placementRule, rng);
			directorSpawnRequest.ignoreTeamMemberLimit = true;
			directorSpawnRequest.teamIndexOverride = TeamIndex.Player;
			currentBoostHpCoefficient = 1f;
			currentBoostDamageCoefficient = 1f;
			if (isFalseSonBossLunarShardBrokenMaster)
			{
				GoldTitanManager.onGoldTitanSpawned();
				directorSpawnRequest.teamIndexOverride = TeamIndex.Monster;
				currentBoostDamageCoefficient += Run.instance.difficultyCoefficient / 8f;
				currentBoostHpCoefficient += Run.instance.difficultyCoefficient / 2f;
			}
			currentBoostHpCoefficient *= Mathf.Pow(totalItemCount, 1f);
			currentBoostDamageCoefficient *= Mathf.Pow(totalItemCount, 0.5f);
			directorSpawnRequest.onSpawnedServer = OnSpawnedServer;
			DirectorCore.instance.TrySpawnObject(directorSpawnRequest);
			if (newTitans.Count > 0)
			{
				EndChannelingTitansServer(currentChanneler);
				GoldTitanManager.onChannelEnd = channelEndCallback;
				currentChanneler = channeler;
				ListUtils.AddRange(currentTitans, newTitans);
			}
			return true;
		}
		catch (Exception message)
		{
			Debug.LogError(message);
			KillTitansInList(newTitans);
			return false;
		}
		finally
		{
			CollectionPool<CharacterMaster, List<CharacterMaster>>.ReturnCollection(newTitans);
		}
		void OnSpawnedServer(SpawnCard.SpawnResult spawnResult)
		{
			GameObject spawnedInstance = spawnResult.spawnedInstance;
			ItemStealController titanItemStealController;
			if ((bool)spawnedInstance)
			{
				CharacterMaster component = spawnedInstance.GetComponent<CharacterMaster>();
				if ((bool)component)
				{
					newTitans.Add(component);
					component.inventory.GiveItem(RoR2Content.Items.BoostHp, Mathf.RoundToInt((currentBoostHpCoefficient - 1f) * 10f));
					component.inventory.GiveItem(RoR2Content.Items.BoostDamage, Mathf.RoundToInt((currentBoostDamageCoefficient - 1f) * 10f));
					if (lookAtPosition.HasValue)
					{
						CharacterBody body = component.GetBody();
						if ((bool)body)
						{
							if ((bool)body.characterDirection)
							{
								body.characterDirection.forward = lookAtPosition.Value - body.corePosition;
							}
							if ((bool)body.inputBank)
							{
								body.inputBank.aimDirection = lookAtPosition.Value - body.aimOrigin;
							}
						}
					}
					titanItemStealController = component.gameObject.AddComponent<ItemStealController>();
					titanItemStealController.itemStealFilter = goldTitanItemFilterDelegate;
					titanItemStealController.itemLendFilter = noItemFilterDelegate;
					titanItemStealController.stealInterval = 0f;
					component.onBodyStart += OnBodyDiscovered;
					component.onBodyDeath.AddListener(OnBodyLost);
					CharacterBody body2 = component.GetBody();
					if ((bool)body2)
					{
						OnBodyDiscovered(body2);
					}
				}
			}
			void OnBodyDiscovered(CharacterBody titanBody)
			{
				titanItemStealController.orbDestinationHurtBoxOverride = titanBody.mainHurtBox;
				titanItemStealController.StartSteal(allCharacterMastersFilterDelegate);
			}
			void OnBodyLost()
			{
				titanItemStealController?.ReclaimAllItems();
			}
		}
	}

	private static void EndChannelingTitansServer(object channeler)
	{
		if (channeler == null || channeler != currentChanneler)
		{
			return;
		}
		currentChanneler = null;
		KillTitansInList(currentTitans);
		currentTitans.Clear();
		Action action = GoldTitanManager.onChannelEnd;
		GoldTitanManager.onChannelEnd = null;
		try
		{
			action?.Invoke();
		}
		catch (Exception message)
		{
			Debug.LogError(message);
		}
	}

	private static bool TryStartChannelingAgainstCombatSquadServer(CombatSquad combatSquad)
	{
		if (!combatSquad)
		{
			return false;
		}
		List<Vector3> list = CollectionPool<Vector3, List<Vector3>>.RentCollection();
		List<Vector3> list2 = CollectionPool<Vector3, List<Vector3>>.RentCollection();
		try
		{
			combatSquad.onDefeatedServer += EndChannelingWhenDefeated;
			foreach (CharacterMaster readOnlyInstances in CharacterMaster.readOnlyInstancesList)
			{
				CharacterBody body = readOnlyInstances.GetBody();
				if ((bool)body && readOnlyInstances.inventory.GetItemCount(goldTitanItemIndex) > 0)
				{
					list2.Add(body.corePosition);
				}
			}
			foreach (CharacterMaster readOnlyMembers in combatSquad.readOnlyMembersList)
			{
				CharacterBody body2 = readOnlyMembers.GetBody();
				if ((bool)body2)
				{
					list.Add(body2.corePosition);
				}
			}
			if (list2.Count == 0 || list.Count == 0)
			{
				if (list2.Count == list.Count)
				{
					Vector3 position = combatSquad.transform.position;
					list2.Add(position);
					list.Add(position);
				}
				else
				{
					List<Vector3> dest = ((list2.Count == 0) ? list2 : list);
					List<Vector3> src = ((list2.Count != 0) ? list2 : list);
					ListUtils.AddRange(dest, src);
				}
			}
			Vector3 vector = Vector3Utils.AveragePrecise(list);
			Vector3 approximatePosition = Vector3.Lerp(Vector3Utils.AveragePrecise(list2), vector, 0.15f);
			return TryStartChannelingTitansServer(combatSquad, approximatePosition, vector, delegate
			{
				combatSquad.onDefeatedServer -= EndChannelingWhenDefeated;
			});
		}
		catch (Exception message)
		{
			Debug.LogError(message);
			return false;
		}
		finally
		{
			CollectionPool<Vector3, List<Vector3>>.ReturnCollection(list2);
			CollectionPool<Vector3, List<Vector3>>.ReturnCollection(list);
		}
		void EndChannelingWhenDefeated()
		{
			EndChannelingTitansServer(combatSquad);
		}
	}

	private static void OnRunStartGlobal(Run run)
	{
		if (NetworkServer.active)
		{
			rng.ResetSeed(run.seed + 88888888);
		}
	}

	private static void OnRunDestroyGlobal(Run run)
	{
		if (NetworkServer.active)
		{
			EndChannelingTitansServer(currentChanneler);
		}
	}

	private static void OnTeleporterBeginChargingGlobal(TeleporterInteraction teleporter)
	{
		if (NetworkServer.active)
		{
			TryStartChannelingTitansServer(teleporter, teleporter.transform.position);
		}
	}

	private static void OnTeleporterChargedGlobal(TeleporterInteraction teleporter)
	{
		if (NetworkServer.active)
		{
			EndChannelingTitansServer(teleporter);
		}
	}

	private static void OnBossGroupStartServer(BossGroup bossGroup)
	{
		CombatSquad combatSquad = bossGroup.combatSquad;
		bool flag = false;
		isFalseSonBossLunarShardBrokenMaster = false;
		foreach (CharacterMaster readOnlyMembers in combatSquad.readOnlyMembersList)
		{
			if (readOnlyMembers.masterIndex == brotherHurtMasterIndex)
			{
				flag = true;
				break;
			}
			if (readOnlyMembers.masterIndex == falseSonBossLunarShardBrokenMasterIndex)
			{
				isFalseSonBossLunarShardBrokenMaster = true;
				break;
			}
		}
		float timer;
		if (flag || isFalseSonBossLunarShardBrokenMaster)
		{
			timer = 2f;
			RoR2Application.onFixedUpdate += Check;
		}
		void Check()
		{
			bool flag2 = true;
			try
			{
				if ((bool)combatSquad)
				{
					ReadOnlyCollection<CharacterMaster> readOnlyMembersList = combatSquad.readOnlyMembersList;
					for (int i = 0; i < readOnlyMembersList.Count; i++)
					{
						CharacterMaster characterMaster = readOnlyMembersList[i];
						if ((bool)characterMaster)
						{
							CharacterBody body = characterMaster.GetBody();
							if (body.HasBuff(RoR2Content.Buffs.Immune) || body.outOfCombat)
							{
								flag2 = false;
							}
							else
							{
								timer -= Time.fixedDeltaTime;
								if (timer > 0f)
								{
									flag2 = false;
								}
							}
						}
					}
					if (flag2)
					{
						TryStartChannelingAgainstCombatSquadServer(combatSquad);
					}
				}
			}
			catch (Exception)
			{
			}
			if (flag2)
			{
				RoR2Application.onFixedUpdate -= Check;
			}
		}
	}
}
