using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class PhasedInventorySetter : MonoBehaviour
{
	[Serializable]
	public struct PhaseItems
	{
		public ItemCountPair[] itemCounts;
	}

	[SerializeField]
	private CharacterBody body;

	[SerializeField]
	private PhaseItems[] phases;

	private bool isPhaseDirty = true;

	private int phaseIndex;

	public bool AdvancePhase()
	{
		if (phaseIndex < phases.Length - 1)
		{
			phaseIndex++;
			isPhaseDirty = true;
			TryUpdateInventory();
			return true;
		}
		return false;
	}

	private void FixedUpdate()
	{
		TryUpdateInventory();
	}

	private void TryUpdateInventory()
	{
		if (NetworkServer.active && isPhaseDirty && phaseIndex < phases.Length && (bool)body && (bool)body.inventory)
		{
			ItemCountPair[] itemCounts = phases[phaseIndex].itemCounts;
			for (int i = 0; i < itemCounts.Length; i++)
			{
				ItemCountPair itemCountPair = itemCounts[i];
				int itemCount = body.inventory.GetItemCount(itemCountPair.itemDef);
				body.inventory.GiveItem(itemCountPair.itemDef, itemCountPair.count - itemCount);
			}
			isPhaseDirty = false;
		}
	}

	public int GetNumPhases()
	{
		return phases.Length;
	}

	public int GetItemCountForPhase(int phaseIndex, ItemDef itemDef)
	{
		if (phases.Length > phaseIndex)
		{
			ItemCountPair[] itemCounts = phases[phaseIndex].itemCounts;
			for (int i = 0; i < itemCounts.Length; i++)
			{
				ItemCountPair itemCountPair = itemCounts[i];
				if ((object)itemDef == itemCountPair.itemDef)
				{
					return itemCountPair.count;
				}
			}
		}
		return 0;
	}
}
