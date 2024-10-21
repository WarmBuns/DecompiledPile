using System.Collections.Generic;
using System.Linq;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.ScavMonster;

public class FindItem : BaseState
{
	public static float baseDuration;

	public static float tier1Chance;

	public static int tier1Count;

	public static float tier2Chance;

	public static int tier2Count;

	public static float tier3Chance;

	public static int tier3Count;

	public static string sound;

	private float duration;

	private PickupIndex dropPickup;

	private int itemsToGrant;

	private PickupDisplay pickupDisplay;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		PlayCrossfade("Body", "SitRummage", "Sit.playbackRate", duration, 0.1f);
		Util.PlaySound(sound, base.gameObject);
		if (base.isAuthority)
		{
			WeightedSelection<List<PickupIndex>> weightedSelection = new WeightedSelection<List<PickupIndex>>();
			weightedSelection.AddChoice(Run.instance.availableTier1DropList.Where(PickupIsNonBlacklistedItem).ToList(), tier1Chance);
			weightedSelection.AddChoice(Run.instance.availableTier2DropList.Where(PickupIsNonBlacklistedItem).ToList(), tier2Chance);
			weightedSelection.AddChoice(Run.instance.availableTier3DropList.Where(PickupIsNonBlacklistedItem).ToList(), tier3Chance);
			List<PickupIndex> list = weightedSelection.Evaluate(Random.value);
			dropPickup = list[Random.Range(0, list.Count)];
			PickupDef pickupDef = PickupCatalog.GetPickupDef(dropPickup);
			if (pickupDef != null)
			{
				ItemDef itemDef = ItemCatalog.GetItemDef(pickupDef.itemIndex);
				if (itemDef != null)
				{
					itemsToGrant = 0;
					switch (itemDef.tier)
					{
					case ItemTier.Tier1:
						itemsToGrant = tier1Count;
						break;
					case ItemTier.Tier2:
						itemsToGrant = tier2Count;
						break;
					case ItemTier.Tier3:
						itemsToGrant = tier3Count;
						break;
					default:
						itemsToGrant = 1;
						break;
					}
				}
			}
		}
		Transform transform = FindModelChild("PickupDisplay");
		pickupDisplay = transform.GetComponent<PickupDisplay>();
		pickupDisplay.SetPickupIndex(dropPickup);
	}

	public override void OnExit()
	{
		pickupDisplay.SetPickupIndex(PickupIndex.none);
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextState(new GrantItem
			{
				dropPickup = dropPickup,
				itemsToGrant = itemsToGrant
			});
		}
	}

	public override void OnSerialize(NetworkWriter writer)
	{
		base.OnSerialize(writer);
		writer.Write(dropPickup);
	}

	public override void OnDeserialize(NetworkReader reader)
	{
		base.OnDeserialize(reader);
		dropPickup = reader.ReadPickupIndex();
	}

	private bool PickupIsNonBlacklistedItem(PickupIndex pickupIndex)
	{
		PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
		if (pickupDef == null)
		{
			return false;
		}
		ItemDef itemDef = ItemCatalog.GetItemDef(pickupDef.itemIndex);
		if (itemDef == null)
		{
			return false;
		}
		return itemDef.DoesNotContainTag(ItemTag.AIBlacklist);
	}
}
