namespace RoR2.Achievements;

[RegisterAchievement("DefeatSuperRoboBallBoss", "Characters.Loader", null, 3u, typeof(DefeatSuperRoboBallBossServerAchievement))]
public class DefeatSuperRoboBallBossAchievement : BaseAchievement
{
	private class DefeatSuperRoboBallBossServerAchievement : BaseServerAchievement
	{
		private BodyIndex superRoboBallBossBodyIndex;

		public override void OnInstall()
		{
			base.OnInstall();
			superRoboBallBossBodyIndex = BodyCatalog.FindBodyIndex("SuperRoboBallBossBody");
			GlobalEventManager.onCharacterDeathGlobal += OnCharacterDeathGlobal;
		}

		private void OnCharacterDeathGlobal(DamageReport damageReport)
		{
			if (damageReport.victimBodyIndex == superRoboBallBossBodyIndex && damageReport.victimTeamIndex != TeamIndex.Player)
			{
				Grant();
			}
		}

		public override void OnUninstall()
		{
			GlobalEventManager.onCharacterDeathGlobal -= OnCharacterDeathGlobal;
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
		base.OnUninstall();
	}
}
