namespace RoR2.Achievements;

[RegisterAchievement("KillEliteMonster", "Items.Medkit", null, 1u, typeof(KillEliteMonsterServerAchievement))]
public class KillEliteMonsterAchievement : BaseAchievement
{
	private class KillEliteMonsterServerAchievement : BaseServerAchievement
	{
		public override void OnInstall()
		{
			base.OnInstall();
			GlobalEventManager.onCharacterDeathGlobal += OnCharacterDeath;
		}

		public override void OnUninstall()
		{
			GlobalEventManager.onCharacterDeathGlobal -= OnCharacterDeath;
			base.OnUninstall();
		}

		private void OnCharacterDeath(DamageReport damageReport)
		{
			if (damageReport.victimIsElite && (bool)damageReport.attackerMaster && damageReport.attackerMaster.gameObject == serverAchievementTracker.networkUser.masterObject)
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
