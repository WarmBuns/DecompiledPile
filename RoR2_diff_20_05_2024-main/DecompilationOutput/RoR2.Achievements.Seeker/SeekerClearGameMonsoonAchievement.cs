namespace RoR2.Achievements.Seeker;

[RegisterAchievement("SeekerClearGameMonsoon", "Skins.Seeker.Alt1", null, 10u, null)]
public class SeekerClearGameMonsoonAchievement : BasePerSurvivorClearGameMonsoonAchievement
{
	protected override BodyIndex LookUpRequiredBodyIndex()
	{
		return BodyCatalog.FindBodyIndex("SeekerBody");
	}
}
