namespace RoR2.Achievements.VoidSurvivor;

[RegisterAchievement("VoidSurvivorClearGameMonsoon", "Skins.VoidSurvivor.Alt1", "CompleteVoidEnding", 10u, null)]
public class VoidSurvivorClearGameMonsoonAchievement : BasePerSurvivorClearGameMonsoonAchievement
{
	protected override BodyIndex LookUpRequiredBodyIndex()
	{
		return BodyCatalog.FindBodyIndex("VoidSurvivorBody");
	}
}
