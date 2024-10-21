namespace RoR2.Achievements.Chef;

[RegisterAchievement("ChefClearGameMonsoon", "Skins.Chef.Alt1", "ActivateChef", 10u, null)]
public class ChefClearGameMonsoonAchievement : BasePerSurvivorClearGameMonsoonAchievement
{
	protected override BodyIndex LookUpRequiredBodyIndex()
	{
		return BodyCatalog.FindBodyIndex("ChefBody");
	}
}
