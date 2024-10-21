namespace RoR2.Achievements;

[RegisterAchievement("CompleteVoidEnding", "Characters.VoidSurvivor", null, 3u, typeof(CompleteWave50ServerAchievement))]
public class CompleteVoidEndingAchievement : BaseEndingAchievement
{
	private class CompleteWave50ServerAchievement : BaseServerAchievement
	{
		public const int waveRequirement = 50;

		public override void OnInstall()
		{
			base.OnInstall();
			InfiniteTowerRun.onAllEnemiesDefeatedServer += OnAllEnemiesDefeatedServer;
		}

		public override void OnUninstall()
		{
			InfiniteTowerRun.onAllEnemiesDefeatedServer -= OnAllEnemiesDefeatedServer;
			base.OnUninstall();
		}

		private void OnAllEnemiesDefeatedServer(InfiniteTowerWaveController waveController)
		{
			InfiniteTowerRun infiniteTowerRun = Run.instance as InfiniteTowerRun;
			if ((bool)infiniteTowerRun && infiniteTowerRun.waveIndex >= 50)
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

	protected override bool ShouldGrant(RunReport runReport)
	{
		if (runReport.gameEnding == DLC1Content.GameEndings.VoidEnding)
		{
			return true;
		}
		return false;
	}
}
