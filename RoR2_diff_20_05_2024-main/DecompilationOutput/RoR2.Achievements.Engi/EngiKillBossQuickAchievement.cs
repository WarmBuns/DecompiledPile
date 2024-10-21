using Assets.RoR2.Scripts.Platform;

namespace RoR2.Achievements.Engi;

[RegisterAchievement("EngiKillBossQuick", "Skills.Engi.SpiderMine", "Complete30StagesCareer", 3u, typeof(EngiKillBossQuickServerAchievement))]
public class EngiKillBossQuickAchievement : BaseAchievement
{
	private class EngiKillBossQuickServerAchievement : BaseServerAchievement
	{
		private const float requirement = 5f;

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
			if (bossGroup.fixedTimeSinceEnabled <= 5f)
			{
				Grant();
				ServerTryToCompleteActivity();
			}
		}
	}

	protected override BodyIndex LookUpRequiredBodyIndex()
	{
		return BodyCatalog.FindBodyIndex("EngiBody");
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

	public override void TryToCompleteActivity()
	{
		if (base.localUser.id == LocalUserManager.GetFirstLocalUser().id && shouldGrant)
		{
			BaseActivitySelector baseActivitySelector = new BaseActivitySelector();
			baseActivitySelector.activityAchievementID = "EngiKillBossQuick";
			PlatformSystems.activityManager.TryToCompleteActivity(baseActivitySelector);
		}
	}
}
