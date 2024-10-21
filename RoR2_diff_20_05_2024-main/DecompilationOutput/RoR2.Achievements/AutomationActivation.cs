using JetBrains.Annotations;
using RoR2.Stats;

namespace RoR2.Achievements;

[RegisterAchievement("AutomationActivation", "Items.Squid", null, 2u, typeof(AutomationActivation))]
public class AutomationActivation : BaseAchievement
{
	private static readonly ulong requirement = 6uL;

	private static StatDef statDef => StatDef.totalTurretsPurchased;

	public override void OnInstall()
	{
		base.OnInstall();
		owner.onRunStatsUpdated += OnRunStatsUpdated;
	}

	public override void OnUninstall()
	{
		owner.onRunStatsUpdated -= OnRunStatsUpdated;
		base.OnUninstall();
	}

	private void OnRunStatsUpdated([NotNull] StatSheet runStatSheet)
	{
		if (runStatSheet.GetStatValueULong(statDef) >= requirement)
		{
			Grant();
		}
	}
}
