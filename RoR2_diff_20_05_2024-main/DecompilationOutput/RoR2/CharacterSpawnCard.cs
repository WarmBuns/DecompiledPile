using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Serialization;

namespace RoR2;

[CreateAssetMenu(menuName = "RoR2/SpawnCards/CharacterSpawnCard")]
public class CharacterSpawnCard : SpawnCard, MasterSummon.IInventorySetupCallback
{
	public bool noElites;

	public bool forbiddenAsBoss;

	[CanBeNull]
	[FormerlySerializedAs("loadout")]
	[SerializeField]
	[Tooltip("The loadout for any summoned character to use.")]
	private SerializableLoadout _loadout;

	[CanBeNull]
	[Tooltip("The inventory from which to initially paste into any summoned character's inventory. Will skip certain non-bequeathable items. This is usually not a good idea to set up in the editor, and is more reserved for runtime.")]
	public Inventory inventoryToCopy;

	[CanBeNull]
	public Func<ItemIndex, bool> inventoryItemCopyFilter;

	[NotNull]
	[Tooltip("The set of equipment to grant to any summoned character, after inventory copy.")]
	public EquipmentDef[] equipmentToGrant = Array.Empty<EquipmentDef>();

	[NotNull]
	[Tooltip("The set of items to grant to any summoned character, after inventory copy.")]
	public ItemCountPair[] itemsToGrant = Array.Empty<ItemCountPair>();

	[CanBeNull]
	protected Loadout runtimeLoadout;

	private static ResourceAvailability loadoutAvailability;

	[CanBeNull]
	public SerializableLoadout loadout
	{
		get
		{
			return _loadout;
		}
		set
		{
			if (_loadout != value)
			{
				_loadout = value;
				if (loadoutAvailability.available)
				{
					SetLoadoutFromSerializedLoadout();
				}
				else
				{
					loadoutAvailability.CallWhenAvailable(SetLoadoutFromSerializedLoadout);
				}
			}
		}
	}

	public void Awake()
	{
		if (!loadoutAvailability.available)
		{
			loadoutAvailability.CallWhenAvailable(SetLoadoutFromSerializedLoadout);
		}
		else
		{
			SetLoadoutFromSerializedLoadout();
		}
	}

	private void SetLoadoutFromSerializedLoadout()
	{
		SetLoadout(loadout);
	}

	private void SetLoadout([CanBeNull] SerializableLoadout serializableLoadout)
	{
		if (Application.isPlaying)
		{
			runtimeLoadout = ((serializableLoadout != null && !serializableLoadout.isEmpty) ? new Loadout() : null);
			if (runtimeLoadout != null)
			{
				serializableLoadout.Apply(runtimeLoadout);
			}
		}
	}

	[CanBeNull]
	protected virtual Loadout GetRuntimeLoadout()
	{
		return runtimeLoadout;
	}

	[CanBeNull]
	protected virtual Action<CharacterMaster> GetPreSpawnSetupCallback()
	{
		return null;
	}

	protected override void Spawn(Vector3 position, Quaternion rotation, DirectorSpawnRequest directorSpawnRequest, ref SpawnResult result)
	{
		MasterSummon masterSummon = new MasterSummon
		{
			masterPrefab = prefab,
			position = position,
			rotation = rotation,
			summonerBodyObject = directorSpawnRequest.summonerBodyObject,
			teamIndexOverride = directorSpawnRequest.teamIndexOverride,
			ignoreTeamMemberLimit = directorSpawnRequest.ignoreTeamMemberLimit,
			loadout = GetRuntimeLoadout(),
			inventoryToCopy = inventoryToCopy,
			inventoryItemCopyFilter = inventoryItemCopyFilter,
			inventorySetupCallback = this,
			preSpawnSetupCallback = GetPreSpawnSetupCallback(),
			useAmbientLevel = true
		};
		result.spawnedInstance = masterSummon.Perform()?.gameObject;
		result.success = result.spawnedInstance;
	}

	[SystemInitializer(new Type[] { typeof(Loadout) })]
	private static void Init()
	{
		loadoutAvailability.MakeAvailable();
	}

	void MasterSummon.IInventorySetupCallback.SetupSummonedInventory(MasterSummon masterSummon, Inventory summonedInventory)
	{
		SetupSummonedInventory(masterSummon, summonedInventory);
	}

	protected virtual void SetupSummonedInventory(MasterSummon masterSummon, Inventory summonedInventory)
	{
		for (int i = 0; i < equipmentToGrant.Length; i++)
		{
			EquipmentState equipment = summonedInventory.GetEquipment((uint)i);
			summonedInventory.SetEquipment(new EquipmentState(equipmentToGrant[i]?.equipmentIndex ?? EquipmentIndex.None, equipment.chargeFinishTime, equipment.charges), (uint)i);
		}
		for (int j = 0; j < itemsToGrant.Length; j++)
		{
			ItemCountPair itemCountPair = itemsToGrant[j];
			summonedInventory.GiveItem(itemCountPair.itemDef, itemCountPair.count);
		}
	}

	public void OnValidate()
	{
		if (occupyPosition)
		{
			Debug.LogErrorFormat("{0} OccupyPosition=1.This is only intended for limited spawns, and will prevent spawning on this node in the future! Are ya sure? ", this);
		}
	}
}
