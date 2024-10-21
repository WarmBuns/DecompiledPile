namespace RoR2.Achievements.Toolbot;

[RegisterAchievement("ToolbotKillImpBossWithBfg", "Skills.Toolbot.Buzzsaw", "RepeatFirstTeleporter", 3u, typeof(ToolbotKillImpBossWithBfgServerAchievement))]
public class ToolbotKillImpBossWithBfgAchievement : BaseAchievement
{
	private class ToolbotKillImpBossWithBfgServerAchievement : BaseServerAchievement
	{
		private BodyIndex impBossBodyIndex = BodyIndex.None;

		private int bfgProjectileIndex = -1;

		public override void OnInstall()
		{
			base.OnInstall();
			impBossBodyIndex = BodyCatalog.FindBodyIndex("ImpBossBody");
			bfgProjectileIndex = ProjectileCatalog.FindProjectileIndex("BeamSphere");
			GlobalEventManager.onCharacterDeathGlobal += OnCharacterDeath;
		}

		public override void OnUninstall()
		{
			GlobalEventManager.onCharacterDeathGlobal -= OnCharacterDeath;
			base.OnUninstall();
		}

		private void OnCharacterDeath(DamageReport damageReport)
		{
			if ((bool)damageReport.victimBody && damageReport.victimBody.bodyIndex == impBossBodyIndex && IsCurrentBody(damageReport.damageInfo.attacker) && ProjectileCatalog.GetProjectileIndex(damageReport.damageInfo.inflictor) == bfgProjectileIndex)
			{
				Grant();
			}
		}
	}

	protected override BodyIndex LookUpRequiredBodyIndex()
	{
		return BodyCatalog.FindBodyIndex("ToolbotBody");
	}

	protected override void OnBodyRequirementMet()
	{
		base.OnBodyRequirementMet();
		SetServerTracked(shouldTrack: true);
	}

	protected override void OnBodyRequirementBroken()
	{
		SetServerTracked(shouldTrack: false);
		base.OnBodyRequirementBroken();
	}
}
