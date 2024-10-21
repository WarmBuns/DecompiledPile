using EntityStates.Missions.Goldshores;

namespace RoR2.Achievements;

[RegisterAchievement("KillGoldTitanInOneCycle", "Items.Gateway", null, 2u, typeof(KillGoldTitanInOnePhaseServerAchievement))]
public class KillGoldTitanInOneCycleAchievement : BaseAchievement
{
	public class KillGoldTitanInOnePhaseServerAchievement : BaseServerAchievement
	{
		private BodyIndex goldTitanBodyIndex = BodyIndex.None;

		public override void OnInstall()
		{
			base.OnInstall();
			goldTitanBodyIndex = BodyCatalog.FindBodyIndex("TitanGoldBody");
			GoldshoresBossfight.onOneCycleGoldTitanKill += OnOneCycleGoldTitanKill;
		}

		private void OnOneCycleGoldTitanKill()
		{
			if ((bool)serverAchievementTracker.networkUser.GetCurrentBody())
			{
				Grant();
			}
		}

		public override void OnUninstall()
		{
			GoldshoresBossfight.onOneCycleGoldTitanKill -= OnOneCycleGoldTitanKill;
			base.OnUninstall();
		}
	}

	public override void OnInstall()
	{
		base.OnInstall();
		SetServerTracked(shouldTrack: true);
	}

	public override void OnUninstall()
	{
		SetServerTracked(shouldTrack: false);
		base.OnUninstall();
	}
}
