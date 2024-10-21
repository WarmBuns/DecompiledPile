using EntityStates.FalseSonBoss;

namespace RoR2.Achievements;

[RegisterAchievement("UnlockFalseSon", "Characters.FalseSon", null, 3u, typeof(UnlockFalseSonAchievementServer))]
public class UnlockFalseSonAchievement : BaseAchievement
{
	public class UnlockFalseSonAchievementServer : BaseServerAchievement
	{
		public override void OnInstall()
		{
			base.OnInstall();
			SkyJumpDeathState.falseSonUnlockEvent += OnFalseSonUnlock;
		}

		public override void OnUninstall()
		{
			base.OnUninstall();
			SkyJumpDeathState.falseSonUnlockEvent -= OnFalseSonUnlock;
		}

		private void OnFalseSonUnlock()
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
