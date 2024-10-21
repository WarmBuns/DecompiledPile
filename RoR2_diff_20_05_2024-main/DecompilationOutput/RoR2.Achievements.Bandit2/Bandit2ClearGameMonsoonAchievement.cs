namespace RoR2.Achievements.Bandit2;

[RegisterAchievement("Bandit2ClearGameMonsoon", "Skins.Bandit2.Alt1", "CompleteThreeStages", 10u, null)]
public class Bandit2ClearGameMonsoonAchievement : BasePerSurvivorClearGameMonsoonAchievement
{
	protected override BodyIndex LookUpRequiredBodyIndex()
	{
		return BodyCatalog.FindBodyIndex("Bandit2Body");
	}
}
