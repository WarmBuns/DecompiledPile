using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RoR2;

public class DroneWeaponsBehavior : CharacterBody.ItemBehavior
{
	private const float display2Chance = 0.1f;

	private int previousStack;

	private CharacterSpawnCard droneSpawnCard;

	private Xoroshiro128Plus rng;

	private DirectorPlacementRule placementRule;

	private const float minSpawnDist = 3f;

	private const float maxSpawnDist = 40f;

	private const float spawnRetryDelay = 1f;

	private bool hasSpawnedDrone;

	private float spawnDelay;

	private void OnEnable()
	{
		ulong seed = Run.instance.seed ^ (ulong)Run.instance.stageClearCount;
		rng = new Xoroshiro128Plus(seed);
		droneSpawnCard = Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/DLC1/DroneCommander/cscDroneCommander.asset").WaitForCompletion();
		placementRule = new DirectorPlacementRule
		{
			placementMode = DirectorPlacementRule.PlacementMode.Approximate,
			minDistance = 3f,
			maxDistance = 40f,
			spawnOnTarget = base.transform
		};
		UpdateAllMinions(stack);
		MasterSummon.onServerMasterSummonGlobal += OnServerMasterSummonGlobal;
	}

	private void OnDisable()
	{
		MasterSummon.onServerMasterSummonGlobal -= OnServerMasterSummonGlobal;
		UpdateAllMinions(0);
	}

	private void FixedUpdate()
	{
		if (previousStack != stack)
		{
			UpdateAllMinions(stack);
		}
		spawnDelay -= Time.fixedDeltaTime;
		if (!hasSpawnedDrone && (bool)body && spawnDelay <= 0f)
		{
			TrySpawnDrone();
		}
	}

	private void OnServerMasterSummonGlobal(MasterSummon.MasterSummonReport summonReport)
	{
		if (!body || !body.master || !(body.master == summonReport.leaderMasterInstance))
		{
			return;
		}
		CharacterMaster summonMasterInstance = summonReport.summonMasterInstance;
		if ((bool)summonMasterInstance)
		{
			CharacterBody characterBody = summonMasterInstance.GetBody();
			if ((bool)characterBody)
			{
				UpdateMinionInventory(summonMasterInstance.inventory, characterBody.bodyFlags, stack);
			}
		}
	}

	private void UpdateAllMinions(int newStack)
	{
		if (!body || !(body?.master))
		{
			return;
		}
		MinionOwnership.MinionGroup minionGroup = MinionOwnership.MinionGroup.FindGroup(body.master.netId);
		if (minionGroup == null)
		{
			return;
		}
		MinionOwnership[] members = minionGroup.members;
		foreach (MinionOwnership minionOwnership in members)
		{
			if (!minionOwnership)
			{
				continue;
			}
			CharacterMaster component = minionOwnership.GetComponent<CharacterMaster>();
			if ((bool)component && (bool)component.inventory)
			{
				CharacterBody characterBody = component.GetBody();
				if ((bool)characterBody)
				{
					UpdateMinionInventory(component.inventory, characterBody.bodyFlags, newStack);
				}
			}
		}
		previousStack = newStack;
	}

	private void UpdateMinionInventory(Inventory inventory, CharacterBody.BodyFlags bodyFlags, int newStack)
	{
		if (((bool)inventory && newStack > 0 && (bodyFlags & CharacterBody.BodyFlags.Mechanical) != 0) || (bodyFlags & CharacterBody.BodyFlags.Devotion) != 0)
		{
			int itemCount = inventory.GetItemCount(DLC1Content.Items.DroneWeaponsBoost);
			int itemCount2 = inventory.GetItemCount(DLC1Content.Items.DroneWeaponsDisplay1);
			int itemCount3 = inventory.GetItemCount(DLC1Content.Items.DroneWeaponsDisplay2);
			if (itemCount < stack)
			{
				inventory.GiveItem(DLC1Content.Items.DroneWeaponsBoost, stack - itemCount);
			}
			else if (itemCount > stack)
			{
				inventory.RemoveItem(DLC1Content.Items.DroneWeaponsBoost, itemCount - stack);
			}
			if (itemCount2 + itemCount3 <= 0)
			{
				ItemDef itemDef = DLC1Content.Items.DroneWeaponsDisplay1;
				if (Random.value < 0.1f)
				{
					itemDef = DLC1Content.Items.DroneWeaponsDisplay2;
				}
				inventory.GiveItem(itemDef);
			}
		}
		else
		{
			inventory.ResetItem(DLC1Content.Items.DroneWeaponsBoost);
			inventory.ResetItem(DLC1Content.Items.DroneWeaponsDisplay1);
			inventory.ResetItem(DLC1Content.Items.DroneWeaponsDisplay2);
		}
	}

	private void TrySpawnDrone()
	{
		if (!body.master.IsDeployableLimited(DeployableSlot.DroneWeaponsDrone))
		{
			spawnDelay = 1f;
			DirectorSpawnRequest directorSpawnRequest = new DirectorSpawnRequest(droneSpawnCard, placementRule, rng);
			directorSpawnRequest.summonerBodyObject = base.gameObject;
			directorSpawnRequest.onSpawnedServer = OnMasterSpawned;
			DirectorCore.instance.TrySpawnObject(directorSpawnRequest);
		}
	}

	private void OnMasterSpawned(SpawnCard.SpawnResult spawnResult)
	{
		hasSpawnedDrone = true;
		GameObject spawnedInstance = spawnResult.spawnedInstance;
		if (!spawnedInstance)
		{
			return;
		}
		CharacterMaster component = spawnedInstance.GetComponent<CharacterMaster>();
		if ((bool)component)
		{
			Deployable component2 = component.GetComponent<Deployable>();
			if ((bool)component2)
			{
				body.master.AddDeployable(component2, DeployableSlot.DroneWeaponsDrone);
			}
		}
	}
}
