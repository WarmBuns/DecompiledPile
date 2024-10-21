namespace RoR2.Achievements.Loader;

[RegisterAchievement("LoaderSpeedRun", "Skills.Loader.YankHook", "DefeatSuperRoboBallBoss", 3u, null)]
public class LoaderSpeedRunAchievement : BaseAchievement
{
	private SceneDef requiredSceneDef;

	private static readonly float requirement = 1500f;

	public override void OnInstall()
	{
		base.OnInstall();
		requiredSceneDef = SceneCatalog.GetSceneDefFromSceneName("mysteryspace");
	}

	protected override BodyIndex LookUpRequiredBodyIndex()
	{
		return BodyCatalog.FindBodyIndex("LoaderBody");
	}

	protected override void OnBodyRequirementMet()
	{
		base.OnBodyRequirementMet();
		SceneCatalog.onMostRecentSceneDefChanged += OnMostRecentSceneDefChanged;
	}

	private void OnMostRecentSceneDefChanged(SceneDef sceneDef)
	{
		if (sceneDef == requiredSceneDef && Run.instance.GetRunStopwatch() <= requirement)
		{
			Grant();
		}
	}

	protected override void OnBodyRequirementBroken()
	{
		SceneCatalog.onMostRecentSceneDefChanged -= OnMostRecentSceneDefChanged;
		base.OnBodyRequirementBroken();
	}
}
