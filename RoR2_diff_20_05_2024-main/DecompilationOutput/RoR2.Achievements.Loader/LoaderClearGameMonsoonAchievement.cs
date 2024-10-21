namespace RoR2.Achievements.Loader;

[RegisterAchievement("LoaderClearGameMonsoon", "Skins.Loader.Alt1", "DefeatSuperRoboBallBoss", 10u, null)]
public class LoaderClearGameMonsoonAchievement : BasePerSurvivorClearGameMonsoonAchievement
{
	protected override BodyIndex LookUpRequiredBodyIndex()
	{
		return BodyCatalog.FindBodyIndex("LoaderBody");
	}
}
