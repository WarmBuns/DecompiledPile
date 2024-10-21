using RoR2.Stats;

namespace RoR2.Achievements;

[RegisterAchievement("CompleteThreeStages", "Characters.Bandit2", null, 3u, null)]
public class CompleteThreeStagesAchievement : BaseAchievement
{
	private const int requirement = 3;

	public override void OnInstall()
	{
		base.OnInstall();
		TeleporterInteraction.onTeleporterChargedGlobal += OnTeleporterCharged;
	}

	public override void OnUninstall()
	{
		TeleporterInteraction.onTeleporterChargedGlobal -= OnTeleporterCharged;
		base.OnUninstall();
	}

	private void OnTeleporterCharged(TeleporterInteraction teleporterInteraction)
	{
		Check();
	}

	private void Check()
	{
		if ((bool)Run.instance && Run.instance.GetType() == typeof(Run))
		{
			SceneDef sceneDefForCurrentScene = SceneCatalog.GetSceneDefForCurrentScene();
			if (!(sceneDefForCurrentScene == null) && base.localUser.currentNetworkUser.masterPlayerStatsComponent.currentStats.GetStatValueULong(StatDef.totalDeaths) == 0L && sceneDefForCurrentScene.stageOrder == 3)
			{
				Grant();
			}
		}
	}
}
