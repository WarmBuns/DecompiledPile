using System.Collections.Generic;
using EntityStates.Barrel;
using UnityEngine;

namespace RoR2;

public class DelusionChestController : MonoBehaviour
{
	private const int DELUSION_PICKUP_INDEXES_NUMBER = 3;

	public ChestBehavior delusionChest;

	private PickupIndex _delusionPickupIndex = PickupIndex.none;

	private Dictionary<Interactor, PickupIndex[]> _delusionPickupIndexesTable;

	private NetworkUIPromptController _netUIPromptController;

	private PickupPickerController _pickupPickerController;

	private PickupIndex _selectedPickupIndex = PickupIndex.none;

	private Interactor interactor;

	private Inventory interactorInventory;

	private Interactor previousInteractor;

	private bool hasBeenReset;

	public PickupIndex DelusionPickupIndex
	{
		get
		{
			return _delusionPickupIndex;
		}
		set
		{
			_delusionPickupIndex = value;
		}
	}

	public PickupIndex SelectedPickupIndex
	{
		get
		{
			return _selectedPickupIndex;
		}
		set
		{
			_selectedPickupIndex = value;
		}
	}

	public bool IsDelusionChoiceReady => _delusionPickupIndex != PickupIndex.none;

	public bool isDelusionSelected => _selectedPickupIndex != PickupIndex.none;

	public static bool isDelusionEnable => RunArtifactManager.instance.IsArtifactEnabled(CU8Content.Artifacts.Delusion);

	private void Start()
	{
		delusionChest = GetComponent<ChestBehavior>();
		_netUIPromptController = GetComponent<NetworkUIPromptController>();
		_pickupPickerController = GetComponent<PickupPickerController>();
		_netUIPromptController.enabled = false;
		_pickupPickerController.enabled = false;
		_pickupPickerController.SetAvailable(newAvailable: false);
		_delusionPickupIndexesTable = new Dictionary<Interactor, PickupIndex[]>();
		base.enabled = isDelusionEnable;
		hasBeenReset = false;
	}

	public void ResetChestForDelusion()
	{
		if (!hasBeenReset)
		{
			Debug.LogError("reset chest for delusion");
			EntityStateMachine component = GetComponent<EntityStateMachine>();
			PurchaseInteraction component2 = GetComponent<PurchaseInteraction>();
			if ((bool)component)
			{
				component.SetNextState(new Closing());
				Object.Destroy(component2);
				_netUIPromptController.enabled = true;
				_pickupPickerController.enabled = true;
				_pickupPickerController.SetAvailable(newAvailable: true);
				Util.PlaySound("Play_ui_artifact_delusion_reset", base.gameObject);
				hasBeenReset = true;
			}
		}
	}

	public void GenerateDelusionPickupIndexes(Interactor activator)
	{
		if (_delusionPickupIndexesTable.TryGetValue(activator, out var value))
		{
			return;
		}
		value = new PickupIndex[3]
		{
			_delusionPickupIndex,
			default(PickupIndex),
			default(PickupIndex)
		};
		interactorInventory = activator.GetComponent<CharacterBody>()?.inventory;
		interactor = activator;
		if ((bool)interactorInventory)
		{
			List<ItemIndex> availableItems = GetAvailableItems(interactorInventory.itemAcquisitionOrder);
			if (availableItems.Count < 2)
			{
				AppendAvailableItems(ref availableItems);
			}
			for (int i = 1; i < 3; i++)
			{
				int maxExclusive = availableItems.Count - 1;
				int index = Random.Range(0, maxExclusive);
				value[i] = PickupCatalog.FindPickupIndex(availableItems[index]);
				availableItems.RemoveAt(index);
			}
			GenerateRandomPickupIndexes(value);
			_delusionPickupIndexesTable[activator] = value;
		}
	}

	private void AppendAvailableItems(ref List<ItemIndex> availableItems)
	{
		for (int i = availableItems.Count; i < 2; i++)
		{
			ItemIndex itemIndex = delusionChest.GetRandomPickupIndex().itemIndex;
			if (IsItemValid(itemIndex))
			{
				availableItems.Add(itemIndex);
			}
		}
	}

	private List<ItemIndex> GetRandomAvailableItems()
	{
		List<ItemIndex> list = new List<ItemIndex>();
		for (int i = 0; i < 10; i++)
		{
			ItemIndex itemIndex = delusionChest.GetRandomPickupIndex().itemIndex;
			if (IsItemValid(itemIndex))
			{
				list.Add(itemIndex);
			}
		}
		return list;
	}

	private bool IsItemValid(ItemIndex item)
	{
		bool flag = false;
		bool flag2 = true;
		bool flag3 = false;
		ItemDef itemDef = ItemCatalog.GetItemDef(item);
		ItemTierDef itemTierDef = ItemTierCatalog.GetItemTierDef(itemDef.tier);
		if ((!itemTierDef || itemTierDef.canScrap) && itemDef.canRemove && !itemDef.hidden && itemDef.DoesNotContainTag(ItemTag.Scrap) && itemDef.DoesNotContainTag(ItemTag.WorldUnique) && _delusionPickupIndex.itemIndex != item)
		{
			flag = true;
		}
		BasicPickupDropTable basicPickupDropTable = delusionChest.dropTable as BasicPickupDropTable;
		if ((bool)basicPickupDropTable && itemTierDef != null)
		{
			if (basicPickupDropTable.IsFilterRequired())
			{
				flag2 = basicPickupDropTable.PassesFilter(PickupCatalog.FindPickupIndex(item));
			}
			if (basicPickupDropTable.tier1Weight != 0f && itemTierDef.tier == ItemTier.Tier1 && Util.CheckRoll(basicPickupDropTable.tier1Weight * 100f))
			{
				flag3 = true;
			}
			else if (itemTierDef.tier == ItemTier.Tier2 && Util.CheckRoll(basicPickupDropTable.tier2Weight * 100f))
			{
				flag3 = true;
			}
			else if (itemTierDef.tier == ItemTier.Tier3 && Util.CheckRoll(basicPickupDropTable.tier3Weight * 100f))
			{
				flag3 = true;
			}
		}
		return flag && flag2 && flag3;
	}

	private List<ItemIndex> GetAvailableItems(List<ItemIndex> items)
	{
		List<ItemIndex> list = new List<ItemIndex>();
		for (int i = 0; i < items.Count; i++)
		{
			if (IsItemValid(items[i]))
			{
				list.Add(items[i]);
			}
		}
		return list;
	}

	private void GenerateRandomPickupIndexes(PickupIndex[] indexes)
	{
		for (int i = 0; i < 2; i++)
		{
			int num = Random.Range(i, 3);
			PickupIndex pickupIndex = indexes[i];
			indexes[i] = indexes[num];
			indexes[num] = pickupIndex;
		}
	}

	public bool IsSelectedCorrectly()
	{
		return _delusionPickupIndex == SelectedPickupIndex;
	}

	public void RemoveDelusionChoice()
	{
		if ((bool)interactorInventory)
		{
			GameObject effectPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/DelusionItemDissolveVFX");
			EffectData effectData = new EffectData
			{
				origin = interactor.transform.position,
				genericFloat = 1.5f,
				genericUInt = (uint)(_selectedPickupIndex.itemIndex + 1)
			};
			effectData.SetNetworkedObjectReference(base.gameObject);
			EffectManager.SpawnEffect(effectPrefab, effectData, transmit: true);
			interactorInventory.RemoveItem(_selectedPickupIndex.itemIndex);
		}
	}

	public PickupIndex[] GetPickupIndexes(Interactor activator)
	{
		if (_delusionPickupIndexesTable.TryGetValue(activator, out var value))
		{
			return value;
		}
		return null;
	}
}
