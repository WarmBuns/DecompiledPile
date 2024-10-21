using System;

namespace RoR2.Achievements;

[RegisterAchievement("Discover5Equipment", "Items.EquipmentMagazine", null, 2u, null)]
public class Discover5EquipmentAchievement : BaseAchievement
{
	private const int requirement = 5;

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
		return (float)EquipmentDiscovered() / 5f;
	}

	private void OnPickupDiscovered(PickupIndex pickupIndex)
	{
		if ((PickupCatalog.GetPickupDef(pickupIndex)?.equipmentIndex ?? EquipmentIndex.None) != EquipmentIndex.None)
		{
			Check();
		}
	}

	private int EquipmentDiscovered()
	{
		int num = 0;
		EquipmentIndex equipmentIndex = (EquipmentIndex)0;
		for (EquipmentIndex equipmentCount = (EquipmentIndex)EquipmentCatalog.equipmentCount; equipmentIndex < equipmentCount; equipmentIndex++)
		{
			if (base.userProfile.HasDiscoveredPickup(PickupCatalog.FindPickupIndex(equipmentIndex)))
			{
				num++;
			}
		}
		return num;
	}

	private void Check()
	{
		if (EquipmentDiscovered() >= 5)
		{
			Grant();
		}
	}
}
