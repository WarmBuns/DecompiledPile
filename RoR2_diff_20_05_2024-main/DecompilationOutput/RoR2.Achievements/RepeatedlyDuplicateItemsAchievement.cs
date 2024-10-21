namespace RoR2.Achievements;

[RegisterAchievement("RepeatedlyDuplicateItems", "Items.Firework", null, 1u, typeof(RepeatedlyDuplicateItemsServerAchievement))]
public class RepeatedlyDuplicateItemsAchievement : BaseAchievement
{
	private class RepeatedlyDuplicateItemsServerAchievement : BaseServerAchievement
	{
		private const int requirement = 7;

		private ItemIndex trackingItemIndex = ItemIndex.None;

		private int progress;

		public override void OnInstall()
		{
			base.OnInstall();
			PurchaseInteraction.onItemSpentOnPurchase += OnItemSpentOnPurchase;
			Run.onRunStartGlobal += OnRunStartGlobal;
		}

		public override void OnUninstall()
		{
			base.OnInstall();
			PurchaseInteraction.onItemSpentOnPurchase -= OnItemSpentOnPurchase;
			Run.onRunStartGlobal -= OnRunStartGlobal;
		}

		private void OnRunStartGlobal(Run run)
		{
			progress = 0;
		}

		private void OnItemSpentOnPurchase(PurchaseInteraction purchaseInteraction, Interactor interactor)
		{
			CharacterBody currentBody = serverAchievementTracker.networkUser.GetCurrentBody();
			if (!currentBody || !(currentBody.GetComponent<Interactor>() == interactor) || !purchaseInteraction.gameObject.name.Contains("Duplicator"))
			{
				return;
			}
			ShopTerminalBehavior component = purchaseInteraction.GetComponent<ShopTerminalBehavior>();
			if ((bool)component)
			{
				ItemIndex itemIndex = PickupCatalog.GetPickupDef(component.CurrentPickupIndex())?.itemIndex ?? ItemIndex.None;
				if (trackingItemIndex != itemIndex)
				{
					trackingItemIndex = itemIndex;
					progress = 0;
				}
				progress++;
				if (progress >= 7)
				{
					Grant();
				}
			}
		}
	}

	public override void OnInstall()
	{
		base.OnInstall();
		SetServerTracked(shouldTrack: true);
	}

	public override void OnUninstall()
	{
		base.OnUninstall();
	}
}
