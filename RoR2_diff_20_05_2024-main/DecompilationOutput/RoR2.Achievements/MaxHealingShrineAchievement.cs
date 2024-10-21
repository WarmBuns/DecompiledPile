namespace RoR2.Achievements;

[RegisterAchievement("MaxHealingShrine", "Items.PassiveHealing", null, 1u, typeof(MaxHealingShrineServerAchievement))]
public class MaxHealingShrineAchievement : BaseAchievement
{
	private class MaxHealingShrineServerAchievement : BaseServerAchievement
	{
		private const int requirement = 2;

		public override void OnInstall()
		{
			base.OnInstall();
			ShrineHealingBehavior.onActivated += OnHealingShrineActivated;
		}

		public override void OnUninstall()
		{
			ShrineHealingBehavior.onActivated -= OnHealingShrineActivated;
			base.OnUninstall();
		}

		private void OnHealingShrineActivated(ShrineHealingBehavior shrine, Interactor activator)
		{
			if (shrine.purchaseCount >= shrine.maxPurchaseCount)
			{
				CharacterBody currentBody = GetCurrentBody();
				if ((bool)currentBody && currentBody.gameObject == activator.gameObject)
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
