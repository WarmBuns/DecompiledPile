namespace RoR2.Achievements;

[RegisterAchievement("FailShrineChance", "Items.Hoof", null, 1u, typeof(FailShrineChanceServerAchievement))]
public class FailShrineChanceAchievement : BaseAchievement
{
	private class FailShrineChanceServerAchievement : BaseServerAchievement
	{
		private int failedInARow;

		private const int requirement = 3;

		public override void OnInstall()
		{
			base.OnInstall();
			ShrineChanceBehavior.onShrineChancePurchaseGlobal += OnShrineChancePurchase;
			Run.onRunStartGlobal += OnRunStartGlobal;
		}

		public override void OnUninstall()
		{
			base.OnInstall();
			ShrineChanceBehavior.onShrineChancePurchaseGlobal -= OnShrineChancePurchase;
			Run.onRunStartGlobal -= OnRunStartGlobal;
		}

		private void OnRunStartGlobal(Run run)
		{
			failedInARow = 0;
		}

		private void OnShrineChancePurchase(bool failed, Interactor interactor)
		{
			CharacterBody currentBody = serverAchievementTracker.networkUser.GetCurrentBody();
			if (!currentBody || !(currentBody.GetComponent<Interactor>() == interactor))
			{
				return;
			}
			if (failed)
			{
				failedInARow++;
				if (failedInARow >= 3)
				{
					Grant();
				}
			}
			else
			{
				failedInARow = 0;
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
