using RoR2.Stats;

namespace RoR2.Achievements.Toolbot;

[RegisterAchievement("ToolbotBeatArenaLater", "Skills.Toolbot.SpecialAlt", "RepeatFirstTeleporter", 3u, typeof(ToolbotBeatArenaLaterServerAchievement))]
public class ToolbotBeatArenaLaterAchievement : BaseAchievement
{
	private class ToolbotBeatArenaLaterServerAchievement : BaseServerAchievement
	{
		private MemoizedGetComponent<PlayerStatsComponent> playerStatsComponentGetter;

		public override void OnInstall()
		{
			base.OnInstall();
			ArenaMissionController.onBeatArena += OnBeatArena;
		}

		public override void OnUninstall()
		{
			ArenaMissionController.onBeatArena -= OnBeatArena;
			base.OnInstall();
		}

		private void OnBeatArena()
		{
			PlayerStatsComponent playerStatsComponent = playerStatsComponentGetter.Get(base.networkUser.masterObject);
			if ((bool)playerStatsComponent && playerStatsComponent.currentStats.GetStatValueULong(StatDef.highestStagesCompleted) >= 6)
			{
				Grant();
			}
		}
	}

	private const int requirement = 6;

	protected override BodyIndex LookUpRequiredBodyIndex()
	{
		return BodyCatalog.FindBodyIndex("ToolbotBody");
	}

	protected override void OnBodyRequirementMet()
	{
		base.OnBodyRequirementMet();
		SetServerTracked(shouldTrack: true);
	}

	protected override void OnBodyRequirementBroken()
	{
		SetServerTracked(shouldTrack: false);
		base.OnBodyRequirementBroken();
	}
}
