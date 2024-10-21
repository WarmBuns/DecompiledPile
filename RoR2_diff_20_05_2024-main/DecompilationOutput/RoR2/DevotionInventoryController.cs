using System;
using System.Collections;
using System.Collections.Generic;
using RoR2.Navigation;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class DevotionInventoryController : NetworkBehaviour
{
	private static NetworkSoundEventDef activationSoundEventDef;

	public static GameObject s_effectPrefab;

	public SfxLocator sfxLocator;

	private static List<DevotionInventoryController> InstanceList = new List<DevotionInventoryController>();

	private static List<EquipmentIndex> lowLevelEliteBuffs = new List<EquipmentIndex>();

	private static List<EquipmentIndex> highLevelEliteBuffs = new List<EquipmentIndex>();

	private bool _isRespawning;

	private CharacterMaster _summonerMaster;

	private Inventory _devotionMinionInventory;

	public CharacterMaster SummonerMaster
	{
		get
		{
			return _summonerMaster;
		}
		set
		{
			_summonerMaster = value;
		}
	}

	public static bool isDevotionEnable => RunArtifactManager.instance.IsArtifactEnabled(CU8Content.Artifacts.Devotion);

	private Interactor _summonerInteractor => _summonerMaster.GetBody().GetComponent<Interactor>();

	[SystemInitializer(new Type[] { typeof(ArtifactCatalog) })]
	private static void Init()
	{
		RunArtifactManager.onArtifactEnabledGlobal += OnDevotionArtifactEnabled;
		RunArtifactManager.onArtifactDisabledGlobal += OnDevotionArtifactDisabled;
		LegacyResourcesAPI.LoadAsyncCallback("NetworkSoundEventDefs/nseNullifiedBuffApplied", delegate(NetworkSoundEventDef asset)
		{
			activationSoundEventDef = asset;
		});
	}

	private static void OnDevotionArtifactEnabled(RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
	{
		if (!(artifactDef != CU8Content.Artifacts.Devotion))
		{
			lowLevelEliteBuffs.Add(RoR2Content.Equipment.AffixRed.equipmentIndex);
			lowLevelEliteBuffs.Add(RoR2Content.Equipment.AffixWhite.equipmentIndex);
			lowLevelEliteBuffs.Add(RoR2Content.Equipment.AffixBlue.equipmentIndex);
			highLevelEliteBuffs.Add(RoR2Content.Equipment.AffixRed.equipmentIndex);
			highLevelEliteBuffs.Add(RoR2Content.Equipment.AffixWhite.equipmentIndex);
			highLevelEliteBuffs.Add(RoR2Content.Equipment.AffixBlue.equipmentIndex);
			highLevelEliteBuffs.Add(RoR2Content.Equipment.AffixPoison.equipmentIndex);
			highLevelEliteBuffs.Add(RoR2Content.Equipment.AffixLunar.equipmentIndex);
			highLevelEliteBuffs.Add(RoR2Content.Equipment.AffixHaunted.equipmentIndex);
			if (DLC1Content.Elites.Earth.IsAvailable())
			{
				lowLevelEliteBuffs.Add(DLC1Content.Elites.Earth.eliteEquipmentDef.equipmentIndex);
				highLevelEliteBuffs.Add(DLC1Content.Elites.Earth.eliteEquipmentDef.equipmentIndex);
			}
			Run.onRunDestroyGlobal += OnRunDestroy;
			s_effectPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/OrbEffects/ItemTakenOrbEffect");
			BossGroup.onBossGroupDefeatedServer += OnBossGroupDefeatedServer;
		}
	}

	private static void OnDevotionArtifactDisabled(RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
	{
		if (!(artifactDef != CU8Content.Artifacts.Devotion))
		{
			lowLevelEliteBuffs.Clear();
			highLevelEliteBuffs.Clear();
			Run.onRunDestroyGlobal -= OnRunDestroy;
			s_effectPrefab = null;
			BossGroup.onBossGroupDefeatedServer -= OnBossGroupDefeatedServer;
		}
	}

	private static void OnRunDestroy(Run run)
	{
		for (int i = 0; i < InstanceList.Count; i++)
		{
			UnityEngine.Object.Destroy(InstanceList[i].gameObject);
		}
		InstanceList.Clear();
	}

	public static void StartRespawnAllLemurians()
	{
		foreach (DevotionInventoryController instance in InstanceList)
		{
			if (!instance._isRespawning)
			{
				instance.StartCoroutine(instance.TryRespawnLemurians());
			}
		}
	}

	public static DevotionInventoryController GetOrCreateDevotionInventoryController(Interactor summoner)
	{
		DevotionInventoryController devotionInventoryController = null;
		foreach (DevotionInventoryController instance in InstanceList)
		{
			if (instance._summonerInteractor == summoner)
			{
				devotionInventoryController = instance;
				break;
			}
		}
		if (!devotionInventoryController)
		{
			GameObject obj = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/DevotionMinionInventory"));
			devotionInventoryController = obj.GetComponent<DevotionInventoryController>();
			devotionInventoryController.GetComponent<TeamFilter>().teamIndex = TeamIndex.Player;
			devotionInventoryController._summonerMaster = summoner.GetComponent<CharacterBody>().master;
			NetworkServer.Spawn(obj);
		}
		return devotionInventoryController;
	}

	[Server]
	public static void ActivateAllDevotedEvolution()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.DevotionInventoryController::ActivateAllDevotedEvolution()' called on client");
			return;
		}
		foreach (DevotionInventoryController instance in InstanceList)
		{
			instance.UpdateAllMinions(shouldEvolve: true);
		}
	}

	[Server]
	public void ActivateDevotedEvolution()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.DevotionInventoryController::ActivateDevotedEvolution()' called on client");
		}
		else
		{
			UpdateAllMinions(shouldEvolve: true);
		}
	}

	[Server]
	public void UpdateAllMinions(bool shouldEvolve = false)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.DevotionInventoryController::UpdateAllMinions(System.Boolean)' called on client");
		}
		else
		{
			if (!_summonerMaster)
			{
				return;
			}
			MinionOwnership.MinionGroup minionGroup = MinionOwnership.MinionGroup.FindGroup(_summonerMaster.netId);
			if (minionGroup == null)
			{
				return;
			}
			MinionOwnership[] members = minionGroup.members;
			foreach (MinionOwnership minionOwnership in members)
			{
				if ((bool)minionOwnership && minionOwnership.GetComponent<CharacterMaster>().TryGetComponent<DevotedLemurianController>(out var component))
				{
					UpdateMinionInventory(component, shouldEvolve);
				}
			}
		}
	}

	public override int GetNetworkChannel()
	{
		return QosChannelIndex.defaultReliable.intVal;
	}

	public void GiveItem(ItemIndex itemIndex, int count = 1)
	{
		_devotionMinionInventory.GiveItem(itemIndex, count);
	}

	public void RemoveItem(ItemIndex itedmIndex, int count = 1)
	{
		_devotionMinionInventory.RemoveItem(itedmIndex, count);
	}

	public bool HasItem(ItemDef item)
	{
		return _devotionMinionInventory.GetItemCount(item) > 0;
	}

	public void DropScrapOnDeath(ItemIndex devotionItem, CharacterBody minionBody)
	{
		PickupIndex pickupIndex = PickupIndex.none;
		ItemDef itemDef = ItemCatalog.GetItemDef(devotionItem);
		if (itemDef != null)
		{
			switch (itemDef.tier)
			{
			case ItemTier.Tier1:
				pickupIndex = PickupCatalog.FindPickupIndex("ItemIndex.ScrapWhite");
				break;
			case ItemTier.Tier2:
				pickupIndex = PickupCatalog.FindPickupIndex("ItemIndex.ScrapGreen");
				break;
			case ItemTier.Tier3:
				pickupIndex = PickupCatalog.FindPickupIndex("ItemIndex.ScrapRed");
				break;
			case ItemTier.Boss:
				pickupIndex = PickupCatalog.FindPickupIndex("ItemIndex.ScrapYellow");
				break;
			}
		}
		if (pickupIndex != PickupIndex.none)
		{
			PickupDropletController.CreatePickupDroplet(pickupIndex, minionBody.corePosition, Vector3.down * 15f);
		}
	}

	[Server]
	private void UpdateMinionInventory(DevotedLemurianController devotedLemurianController, bool shouldEvolve)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.DevotionInventoryController::UpdateMinionInventory(DevotedLemurianController,System.Boolean)' called on client");
			return;
		}
		CharacterBody lemurianBody = devotedLemurianController.LemurianBody;
		Inventory lemurianInventory = devotedLemurianController.LemurianInventory;
		lemurianInventory.CleanInventory();
		if (shouldEvolve)
		{
			if ((bool)activationSoundEventDef)
			{
				Debug.Log("playing sound effect");
				EffectManager.SimpleSoundEffect(activationSoundEventDef.index, lemurianBody.gameObject.transform.position, transmit: true);
			}
			devotedLemurianController.DevotedEvolutionLevel++;
			_devotionMinionInventory.GiveItem(devotedLemurianController.DevotionItem);
			EvolveDevotedLumerian(devotedLemurianController);
		}
		int num = 0;
		num = ((devotedLemurianController.DevotedEvolutionLevel != 0 && devotedLemurianController.DevotedEvolutionLevel != 2) ? ((devotedLemurianController.DevotedEvolutionLevel != 1 && devotedLemurianController.DevotedEvolutionLevel != 3) ? (20 + devotedLemurianController.DevotedEvolutionLevel - 3) : 20) : 10);
		lemurianInventory.GiveItem(RoR2Content.Items.BoostHp, num);
		lemurianInventory.GiveItem(RoR2Content.Items.BoostDamage, num);
		lemurianInventory.AddItemsFrom(_devotionMinionInventory);
	}

	[Server]
	private void EvolveDevotedLumerian(DevotedLemurianController devotedLemurianController)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.DevotionInventoryController::EvolveDevotedLumerian(DevotedLemurianController)' called on client");
			return;
		}
		CharacterBody lemurianBody = devotedLemurianController.LemurianBody;
		switch (devotedLemurianController.DevotedEvolutionLevel)
		{
		case 1:
			GenerateEliteBuff(lemurianBody, devotedLemurianController, isLowLevel: true);
			break;
		case 2:
			lemurianBody.inventory.SetEquipmentIndex(EquipmentIndex.None);
			lemurianBody.master.TransformBody("DevotedLemurianBruiserBody");
			break;
		case 3:
			GenerateEliteBuff(lemurianBody, devotedLemurianController, isLowLevel: false);
			break;
		default:
			Debug.LogError("shouldn't evolve!");
			break;
		}
	}

	[Server]
	private void GenerateEliteBuff(CharacterBody body, DevotedLemurianController devotedCntroller, bool isLowLevel)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.DevotionInventoryController::GenerateEliteBuff(RoR2.CharacterBody,DevotedLemurianController,System.Boolean)' called on client");
			return;
		}
		List<EquipmentIndex> list = (isLowLevel ? lowLevelEliteBuffs : highLevelEliteBuffs);
		int index = UnityEngine.Random.Range(0, list.Count);
		body.inventory.SetEquipmentIndex(list[index]);
	}

	private void Awake()
	{
		_devotionMinionInventory = base.gameObject.GetComponent<Inventory>();
		_devotionMinionInventory.GiveItem(CU8Content.Items.LemurianHarness);
	}

	public static void OnBossGroupDefeatedServer(BossGroup group)
	{
		Debug.Log("on boss group defeated server in devotion");
		if (!SceneCatalog.GetSceneDefForCurrentScene().needSkipDevotionRespawn)
		{
			ActivateAllDevotedEvolution();
		}
	}

	private void OnEnable()
	{
		InstanceList.Add(this);
	}

	private void OnDisable()
	{
		InstanceList.Remove(this);
	}

	private IEnumerator TryRespawnLemurians()
	{
		_isRespawning = true;
		while (!_summonerMaster.GetBody())
		{
			yield return new WaitForSeconds(1f);
		}
		NodeGraph nodeGraph = SceneInfo.instance.GetNodeGraph(MapNodeGroup.GraphType.Ground);
		List<NodeGraph.NodeIndex> list = nodeGraph.FindNodesInRangeWithFlagConditions(_summonerInteractor.transform.position, 3f, 40f, HullMask.None, NodeFlags.None, NodeFlags.NoCharacterSpawn, preventOverhead: false);
		while (list.Count == 0)
		{
			yield return new WaitForSeconds(1f);
			list = nodeGraph.FindNodesInRangeWithFlagConditions(_summonerInteractor.transform.position, 3f, 40f, HullMask.None, NodeFlags.None, NodeFlags.NoCharacterSpawn, preventOverhead: false);
		}
		while (list.Count > 0)
		{
			int index = UnityEngine.Random.Range(0, list.Count);
			NodeGraph.NodeIndex nodeIndex = list[index];
			if (!nodeGraph.GetNodePosition(nodeIndex, out var position))
			{
				continue;
			}
			MinionOwnership.MinionGroup minionGroup = MinionOwnership.MinionGroup.FindGroup(_summonerMaster.netId);
			if (minionGroup == null)
			{
				break;
			}
			MinionOwnership[] members = minionGroup.members;
			foreach (MinionOwnership minionOwnership in members)
			{
				if ((bool)minionOwnership)
				{
					CharacterMaster component = minionOwnership.GetComponent<CharacterMaster>();
					if (component.TryGetComponent<DevotedLemurianController>(out var _))
					{
						component.Respawn(position, Quaternion.identity);
					}
				}
			}
			break;
		}
		_isRespawning = false;
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		bool result = default(bool);
		return result;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
	}

	public override void PreStartClient()
	{
	}
}
