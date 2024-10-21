using System.Collections.Generic;

namespace RoR2.Achievements;

[RegisterAchievement("LoaderKillLoaders", "Skills.Loader.Thunderslam", "DefeatSuperRoboBallBoss", 3u, typeof(LoaderKillLoadersServerAchievement))]
public class LoaderKillLoadersAchievement : BaseAchievement
{
	private class LoaderKillLoadersServerAchievement : BaseServerAchievement
	{
		private BodyIndex bodyIndex;

		private List<SceneDef> requiredSceneDefs = new List<SceneDef>();

		private int numKills;

		public override void OnInstall()
		{
			base.OnInstall();
			bodyIndex = BodyCatalog.FindBodyIndex("LoaderBody");
			requiredSceneDefs.Add(SceneCatalog.GetSceneDefFromSceneName("artifactworld"));
			requiredSceneDefs.Add(SceneCatalog.GetSceneDefFromSceneName("artifactworld01"));
			requiredSceneDefs.Add(SceneCatalog.GetSceneDefFromSceneName("artifactworld02"));
			requiredSceneDefs.Add(SceneCatalog.GetSceneDefFromSceneName("artifactworld03"));
			GlobalEventManager.onCharacterDeathGlobal += OnCharacterDeath;
		}

		public override void OnUninstall()
		{
			GlobalEventManager.onCharacterDeathGlobal -= OnCharacterDeath;
			base.OnUninstall();
		}

		private void OnCharacterDeath(DamageReport damageReport)
		{
			if ((bool)damageReport.victimBody)
			{
				if (damageReport.victimBody.bodyIndex == bodyIndex && IsCurrentBody(damageReport.attackerBody) && CheckForArtifactWorldScene())
				{
					numKills++;
				}
				if (numKills >= 3)
				{
					Grant();
				}
			}
		}

		private bool CheckForArtifactWorldScene()
		{
			bool result = false;
			for (int i = 0; i < requiredSceneDefs.Count; i++)
			{
				if (SceneCatalog.mostRecentSceneDef == requiredSceneDefs[i])
				{
					result = true;
					break;
				}
			}
			return result;
		}
	}

	private const int requirement = 3;

	protected override BodyIndex LookUpRequiredBodyIndex()
	{
		return BodyCatalog.FindBodyIndex("LoaderBody");
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
