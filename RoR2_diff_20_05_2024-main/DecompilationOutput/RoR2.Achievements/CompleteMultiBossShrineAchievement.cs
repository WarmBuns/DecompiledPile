namespace RoR2.Achievements;

[RegisterAchievement("CompleteMultiBossShrine", "Items.Lightning", null, 2u, typeof(CompleteMultiBossShrineServerAchievement))]
public class CompleteMultiBossShrineAchievement : BaseAchievement
{
	private class CompleteMultiBossShrineServerAchievement : BaseServerAchievement
	{
		private const int requirement = 2;

		public override void OnInstall()
		{
			base.OnInstall();
			BossGroup.onBossGroupDefeatedServer += OnBossGroupDefeatedServer;
		}

		public override void OnUninstall()
		{
			BossGroup.onBossGroupDefeatedServer -= OnBossGroupDefeatedServer;
			base.OnUninstall();
		}

		private void OnBossGroupDefeatedServer(BossGroup bossGroup)
		{
			CharacterBody currentBody = GetCurrentBody();
			if ((bool)currentBody && (bool)currentBody.healthComponent && currentBody.healthComponent.alive && (bool)TeleporterInteraction.instance && CheckTeleporter(bossGroup, TeleporterInteraction.instance))
			{
				Grant();
			}
		}

		private bool CheckTeleporter(BossGroup bossGroup, TeleporterInteraction teleporterInteraction)
		{
			if (teleporterInteraction.bossDirector.combatSquad == bossGroup.combatSquad)
			{
				return teleporterInteraction.shrineBonusStacks >= 2;
			}
			return false;
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
