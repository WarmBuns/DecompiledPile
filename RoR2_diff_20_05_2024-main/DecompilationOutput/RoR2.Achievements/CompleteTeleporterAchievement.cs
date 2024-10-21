namespace RoR2.Achievements;

[RegisterAchievement("CompleteTeleporter", "Items.BossDamageBonus", null, 1u, typeof(CompleteTeleporterServerAchievement))]
public class CompleteTeleporterAchievement : BaseAchievement
{
	private class CompleteTeleporterServerAchievement : BaseServerAchievement
	{
		public override void OnInstall()
		{
			base.OnInstall();
			SceneExitController.onBeginExit += OnSceneBeginExit;
			SceneExitControllerColossusPortal.onBeginExit += OnSceneColossusExit;
		}

		public override void OnUninstall()
		{
			SceneExitController.onBeginExit -= OnSceneBeginExit;
			SceneExitControllerColossusPortal.onBeginExit -= OnSceneColossusExit;
			base.OnInstall();
		}

		private void OnSceneBeginExit(SceneExitController exitController)
		{
			if (exitController.portalUsedAfterTeleporterEventFinish)
			{
				Check();
			}
		}

		private void OnSceneColossusExit(SceneExitControllerColossusPortal exitColossusController)
		{
			Check();
		}

		private void Check()
		{
			CharacterBody currentBody = GetCurrentBody();
			if ((bool)currentBody && (bool)currentBody.healthComponent && currentBody.healthComponent.alive)
			{
				Grant();
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
		SetServerTracked(shouldTrack: false);
		base.OnUninstall();
	}
}
