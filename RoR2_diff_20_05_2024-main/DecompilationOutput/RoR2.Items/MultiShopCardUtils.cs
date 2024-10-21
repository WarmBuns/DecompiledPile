using RoR2.Orbs;

namespace RoR2.Items;

public static class MultiShopCardUtils
{
	private const float refundPercentage = 0.1f;

	public static void OnNonMoneyPurchase(CostTypeDef.PayCostContext context)
	{
		OnPurchase(context, 0);
	}

	public static void OnMoneyPurchase(CostTypeDef.PayCostContext context)
	{
		OnPurchase(context, context.cost);
	}

	private static void OnPurchase(CostTypeDef.PayCostContext context, int moneyCost)
	{
		CharacterMaster activatorMaster = context.activatorMaster;
		if (!activatorMaster || !activatorMaster.hasBody || !activatorMaster.inventory || activatorMaster.inventory.currentEquipmentIndex != DLC1Content.Equipment.MultiShopCard.equipmentIndex)
		{
			return;
		}
		CharacterBody body = activatorMaster.GetBody();
		if (body.equipmentSlot.stock <= 0)
		{
			return;
		}
		bool flag = false;
		if (moneyCost > 0)
		{
			flag = true;
			GoldOrb goldOrb = new GoldOrb();
			goldOrb.origin = context.purchasedObject?.transform?.position ?? body.corePosition;
			goldOrb.target = body.mainHurtBox;
			goldOrb.goldAmount = (uint)(0.1f * (float)moneyCost);
			OrbManager.instance.AddOrb(goldOrb);
		}
		ShopTerminalBehavior shopTerminalBehavior = context.purchasedObject?.GetComponent<ShopTerminalBehavior>();
		if ((bool)shopTerminalBehavior && (bool)shopTerminalBehavior.serverMultiShopController)
		{
			flag = true;
			shopTerminalBehavior.serverMultiShopController.SetCloseOnTerminalPurchase(context.purchasedObject.GetComponent<PurchaseInteraction>(), doCloseMultiShop: false);
		}
		if (flag)
		{
			if (body.hasAuthority)
			{
				body.equipmentSlot.OnEquipmentExecuted();
			}
			else
			{
				body.equipmentSlot.CallCmdOnEquipmentExecuted();
			}
		}
	}
}
