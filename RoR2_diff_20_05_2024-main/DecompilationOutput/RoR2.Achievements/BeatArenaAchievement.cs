namespace RoR2.Achievements;

[RegisterAchievement("BeatArena", "Characters.Croco", null, 3u, typeof(BeatArenaServerAchievement))]
public class BeatArenaAchievement : BaseAchievement
{
	private class BeatArenaServerAchievement : BaseServerAchievement
	{
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
			Grant();
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
