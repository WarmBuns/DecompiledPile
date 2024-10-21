namespace RoR2.Achievements.Toolbot;

[RegisterAchievement("ToolbotClearGameMonsoon", "Skins.Toolbot.Alt1", "RepeatFirstTeleporter", 10u, null)]
public class ToolbotClearGameMonsoonAchievement : BasePerSurvivorClearGameMonsoonAchievement
{
	protected override BodyIndex LookUpRequiredBodyIndex()
	{
		return BodyCatalog.FindBodyIndex("ToolbotBody");
	}
}
