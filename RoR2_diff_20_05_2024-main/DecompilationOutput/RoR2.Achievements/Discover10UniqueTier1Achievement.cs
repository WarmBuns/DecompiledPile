using System;

namespace RoR2.Achievements;

[RegisterAchievement("Discover10UniqueTier1", "Items.Crowbar", null, 1u, null)]
public class Discover10UniqueTier1Achievement : BaseAchievement
{
	private const int requirement = 10;

	public override void OnInstall()
	{
		base.OnInstall();
		UserProfile obj = base.userProfile;
		obj.onPickupDiscovered = (Action<PickupIndex>)Delegate.Combine(obj.onPickupDiscovered, new Action<PickupIndex>(OnPickupDiscovered));
		Check();
	}

	public override void OnUninstall()
	{
		UserProfile obj = base.userProfile;
		obj.onPickupDiscovered = (Action<PickupIndex>)Delegate.Remove(obj.onPickupDiscovered, new Action<PickupIndex>(OnPickupDiscovered));
		base.OnUninstall();
	}

	public override float ProgressForAchievement()
	{
		return (float)UniqueTier1Discovered() / 10f;
	}

	private void OnPickupDiscovered(PickupIndex pickupIndex)
	{
		ItemIndex itemIndex = PickupCatalog.GetPickupDef(pickupIndex)?.itemIndex ?? ItemIndex.None;
		if (itemIndex != ItemIndex.None && ItemCatalog.GetItemDef(itemIndex).tier == ItemTier.Tier1)
		{
			Check();
		}
	}

	private int UniqueTier1Discovered()
	{
		int num = 0;
		ItemIndex itemIndex = ItemIndex.Count;
		for (ItemIndex itemCount = (ItemIndex)ItemCatalog.itemCount; itemIndex < itemCount; itemIndex++)
		{
			if (ItemCatalog.GetItemDef(itemIndex).tier == ItemTier.Tier1 && base.userProfile.HasDiscoveredPickup(PickupCatalog.FindPickupIndex(itemIndex)))
			{
				num++;
			}
		}
		return num;
	}

	private void Check()
	{
		if (UniqueTier1Discovered() >= 10)
		{
			Grant();
		}
	}
}
