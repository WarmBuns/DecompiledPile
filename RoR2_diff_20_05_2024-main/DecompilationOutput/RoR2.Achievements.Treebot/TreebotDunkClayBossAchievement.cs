namespace RoR2.Achievements.Treebot;

[RegisterAchievement("TreebotDunkClayBoss", "Skills.Treebot.PlantSonicBoom", "RescueTreebot", 3u, typeof(TreebotDunkClayBossServerAchievement))]
public class TreebotDunkClayBossAchievement : BaseAchievement
{
	private class TreebotDunkClayBossServerAchievement : BaseServerAchievement
	{
		private BodyIndex clayBossBodyIndex;

		private SceneDef requiredSceneDef;

		public override void OnInstall()
		{
			base.OnInstall();
			clayBossBodyIndex = BodyCatalog.FindBodyIndex("ClayBossBody");
			requiredSceneDef = SceneCatalog.GetSceneDefFromSceneName("goolake");
			GlobalEventManager.onCharacterDeathGlobal += OnCharacterDeathGlobal;
		}

		private void OnCharacterDeathGlobal(DamageReport damageReport)
		{
			if (damageReport.victimBodyIndex == clayBossBodyIndex && IsCurrentBody(damageReport.attackerBody) && (bool)damageReport.damageInfo.inflictor && (bool)damageReport.damageInfo.inflictor.GetComponent<MapZone>() && SceneCatalog.mostRecentSceneDef == requiredSceneDef)
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

	protected override BodyIndex LookUpRequiredBodyIndex()
	{
		return BodyCatalog.FindBodyIndex("TreebotBody");
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
