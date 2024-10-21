using RoR2.Stats;

namespace RoR2.Achievements;

[RegisterAchievement("CompleteThreeStagesWithoutHealing", "Items.IncreaseHealing", null, 3u, typeof(CompleteThreeStagesWithoutHealingServerAchievement))]
public class CompleteThreeStagesWithoutHealingsAchievement : BaseAchievement
{
	private class CompleteThreeStagesWithoutHealingServerAchievement : BaseServerAchievement
	{
		public override void OnInstall()
		{
			base.OnInstall();
			SceneExitController.onBeginExit += OnSceneBeginExit;
		}

		public override void OnUninstall()
		{
			SceneExitController.onBeginExit -= OnSceneBeginExit;
			base.OnUninstall();
		}

		private void OnSceneBeginExit(SceneExitController exitController)
		{
			Check();
		}

		private void Check()
		{
			if (!Run.instance || !(Run.instance.GetType() == typeof(Run)) || !(serverAchievementTracker != null) || !(base.networkUser != null) || serverAchievementTracker == null || serverAchievementTracker.networkUser == null)
			{
				return;
			}
			SceneDef sceneDefForCurrentScene = SceneCatalog.GetSceneDefForCurrentScene();
			if (!(base.networkUser.masterPlayerStatsComponent == null) && base.networkUser.masterPlayerStatsComponent.currentStats != null)
			{
				StatSheet currentStats = base.networkUser.masterPlayerStatsComponent.currentStats;
				CharacterBody currentBody = GetCurrentBody();
				if (sceneDefForCurrentScene != null && currentStats.GetStatValueULong(StatDef.highestStagesCompleted) >= 2 && (float)currentStats.GetStatValueULong(StatDef.totalHealthHealed) <= 0f && (bool)currentBody && (bool)currentBody.healthComponent && currentBody.healthComponent.alive)
				{
					Grant();
				}
			}
		}
	}

	private const int requirement = 2;

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
