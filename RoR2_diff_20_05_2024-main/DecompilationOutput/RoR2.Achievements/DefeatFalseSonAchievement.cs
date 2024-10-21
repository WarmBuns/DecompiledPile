using EntityStates.FalseSonBoss;

namespace RoR2.Achievements;

[RegisterAchievement("DefeatFalseSon", "Items.HealAndRevive", null, 5u, typeof(DefeatFalseSonServerAchievement))]
public class DefeatFalseSonAchievement : BaseAchievement
{
	private class DefeatFalseSonServerAchievement : BaseServerAchievement
	{
		public override void OnInstall()
		{
			base.OnInstall();
			SkyJumpDeathState.falseSonDeathEvent += OnFalseSonDefeated;
		}

		public override void OnUninstall()
		{
			base.OnInstall();
			SkyJumpDeathState.falseSonDeathEvent -= OnFalseSonDefeated;
		}

		private void OnFalseSonDefeated()
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
