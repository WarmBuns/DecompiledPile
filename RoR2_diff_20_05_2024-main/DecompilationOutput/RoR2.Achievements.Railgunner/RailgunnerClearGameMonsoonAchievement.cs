namespace RoR2.Achievements.Railgunner;

[RegisterAchievement("RailgunnerClearGameMonsoon", "Skins.RailGunner.Alt1", null, 10u, null)]
public class RailgunnerClearGameMonsoonAchievement : BasePerSurvivorClearGameMonsoonAchievement
{
	protected override BodyIndex LookUpRequiredBodyIndex()
	{
		return BodyCatalog.FindBodyIndex("RailgunnerBody");
	}
}
