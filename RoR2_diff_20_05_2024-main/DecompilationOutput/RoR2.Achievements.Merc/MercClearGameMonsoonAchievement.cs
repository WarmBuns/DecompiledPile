namespace RoR2.Achievements.Merc;

[RegisterAchievement("MercClearGameMonsoon", "Skins.Merc.Alt1", "CompleteUnknownEnding", 10u, null)]
public class MercClearGameMonsoonAchievement : BasePerSurvivorClearGameMonsoonAchievement
{
	protected override BodyIndex LookUpRequiredBodyIndex()
	{
		return BodyCatalog.FindBodyIndex("MercBody");
	}
}
