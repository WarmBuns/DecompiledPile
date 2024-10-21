namespace RoR2.Achievements.FalseSon;

[RegisterAchievement("FalseSonClearGameMonsoon", "Skins.FalseSon.Alt1", "UnlockFalseSon", 10u, null)]
public class FalseSonClearGameMonsoonAchievement : BasePerSurvivorClearGameMonsoonAchievement
{
	protected override BodyIndex LookUpRequiredBodyIndex()
	{
		return BodyCatalog.FindBodyIndex("FalseSonBody");
	}
}
