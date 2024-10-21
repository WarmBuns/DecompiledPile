namespace RoR2.Achievements.Huntress;

[RegisterAchievement("HuntressClearGameMonsoon", "Skins.Huntress.Alt1", null, 10u, null)]
public class HuntressClearGameMonsoonAchievement : BasePerSurvivorClearGameMonsoonAchievement
{
	protected override BodyIndex LookUpRequiredBodyIndex()
	{
		return BodyCatalog.FindBodyIndex("HuntressBody");
	}
}
