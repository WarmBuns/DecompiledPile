namespace RoR2;

public static class StrengthenBurnUtils
{
	public static void CheckDotForUpgrade(Inventory inventory, ref InflictDotInfo dotInfo)
	{
		if (dotInfo.dotIndex == DotController.DotIndex.Burn || dotInfo.dotIndex == DotController.DotIndex.Helfire)
		{
			int itemCount = inventory.GetItemCount(DLC1Content.Items.StrengthenBurn);
			if (itemCount > 0)
			{
				dotInfo.preUpgradeDotIndex = dotInfo.dotIndex;
				dotInfo.dotIndex = DotController.DotIndex.StrongerBurn;
				float num = 1 + 3 * itemCount;
				dotInfo.damageMultiplier *= num;
				dotInfo.totalDamage *= num;
			}
		}
	}
}
