namespace RoR2.Achievements.Commando;

[RegisterAchievement("CaptainClearGameMonsoon", "Skins.Captain.Alt1", "CompleteMainEnding", 10u, null)]
public class CaptainClearGameMonsoonAchievement : BasePerSurvivorClearGameMonsoonAchievement
{
	protected override BodyIndex LookUpRequiredBodyIndex()
	{
		return BodyCatalog.FindBodyIndex("CaptainBody");
	}
}
