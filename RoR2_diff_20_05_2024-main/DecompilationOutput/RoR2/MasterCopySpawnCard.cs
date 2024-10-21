using System;
using RoR2.Navigation;
using UnityEngine;

namespace RoR2;

public class MasterCopySpawnCard : CharacterSpawnCard
{
	protected CharacterMaster srcCharacterMaster;

	protected int[] srcItemStacks;

	protected EquipmentIndex[] srcEquipment;

	protected Action<CharacterMaster> onPreSpawnSetup;

	public static MasterCopySpawnCard FromMaster(CharacterMaster srcCharacterMaster, bool copyItems, bool copyEquipment, Action<CharacterMaster> onPreSpawnSetup = null)
	{
		if (!srcCharacterMaster || !srcCharacterMaster.GetBody())
		{
			return null;
		}
		MasterCopySpawnCard masterCopySpawnCard = ScriptableObject.CreateInstance<MasterCopySpawnCard>();
		masterCopySpawnCard.onPreSpawnSetup = onPreSpawnSetup;
		CopyDataFromMaster(masterCopySpawnCard, srcCharacterMaster, copyItems, copyEquipment);
		return masterCopySpawnCard;
	}

	protected static void CopyDataFromMaster(MasterCopySpawnCard spawnCard, CharacterMaster srcCharacterMaster, bool copyItems, bool copyEquipment)
	{
		spawnCard.srcItemStacks = ItemCatalog.RequestItemStackArray();
		spawnCard.srcEquipment = Array.Empty<EquipmentIndex>();
		if (!srcCharacterMaster)
		{
			return;
		}
		spawnCard.sendOverNetwork = true;
		spawnCard.runtimeLoadout = new Loadout();
		spawnCard.srcCharacterMaster = srcCharacterMaster;
		spawnCard.srcCharacterMaster.loadout.Copy(spawnCard.runtimeLoadout);
		if (copyItems)
		{
			srcCharacterMaster.inventory.WriteItemStacks(spawnCard.srcItemStacks);
		}
		if (copyEquipment)
		{
			spawnCard.srcEquipment = new EquipmentIndex[srcCharacterMaster.inventory.GetEquipmentSlotCount()];
			for (uint num = 0u; num < spawnCard.srcEquipment.Length; num++)
			{
				spawnCard.srcEquipment[num] = srcCharacterMaster.inventory.GetEquipment(num).equipmentIndex;
			}
		}
		CharacterBody body = srcCharacterMaster.GetBody();
		if ((bool)body)
		{
			spawnCard.hullSize = body.hullClassification;
			spawnCard.nodeGraphType = (body.isFlying ? MapNodeGroup.GraphType.Air : MapNodeGroup.GraphType.Ground);
			spawnCard.prefab = MasterCatalog.GetMasterPrefab(MasterCatalog.FindAiMasterIndexForBody(body.bodyIndex));
		}
	}

	public void GiveItem(ItemIndex itemIndex, int count = 1)
	{
		srcItemStacks[(int)itemIndex] += count;
	}

	public void GiveItem(ItemDef itemDef, int count = 1)
	{
		GiveItem(itemDef?.itemIndex ?? ItemIndex.None, count);
	}

	protected override Loadout GetRuntimeLoadout()
	{
		return srcCharacterMaster.loadout;
	}

	protected override Action<CharacterMaster> GetPreSpawnSetupCallback()
	{
		Action<CharacterMaster> baseCallback = base.GetPreSpawnSetupCallback();
		return Callback;
		void Callback(CharacterMaster spawnedMaster)
		{
			if (srcItemStacks != null)
			{
				spawnedMaster.inventory.AddItemsFrom(srcItemStacks, (ItemIndex _) => true);
			}
			if (srcEquipment != null)
			{
				for (uint num = 0u; num < srcEquipment.Length; num++)
				{
					spawnedMaster.inventory.SetEquipmentIndexForSlot(srcEquipment[num], num);
				}
			}
			baseCallback?.Invoke(spawnedMaster);
			onPreSpawnSetup?.Invoke(spawnedMaster);
			if (srcItemStacks != null)
			{
				ItemCatalog.ReturnItemStackArray(srcItemStacks);
			}
		}
	}
}
