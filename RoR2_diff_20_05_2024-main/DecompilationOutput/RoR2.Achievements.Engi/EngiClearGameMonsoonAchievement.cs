namespace RoR2.Achievements.Engi;

[RegisterAchievement("EngiClearGameMonsoon", "Skins.Engi.Alt1", "Complete30StagesCareer", 10u, null)]
public class EngiClearGameMonsoonAchievement : BasePerSurvivorClearGameMonsoonAchievement
{
	protected override BodyIndex LookUpRequiredBodyIndex()
	{
		return BodyCatalog.FindBodyIndex("EngiBody");
	}
}
