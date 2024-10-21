namespace RoR2.Achievements.Commando;

[RegisterAchievement("CommandoFastFirstStageClear", "Skills.Commando.SlideJet", null, 3u, null)]
public class CommandoFastFirstStageClearAchievement : BaseAchievement
{
	private static readonly float timeRequirement = 300f;

	protected override BodyIndex LookUpRequiredBodyIndex()
	{
		return BodyCatalog.FindBodyIndex("CommandoBody");
	}

	protected override void OnBodyRequirementMet()
	{
		base.OnBodyRequirementMet();
		TeleporterInteraction.onTeleporterChargedGlobal += OnTeleporterChargedGlobal;
	}

	private void OnTeleporterChargedGlobal(TeleporterInteraction teleporterInteraction)
	{
		if (Run.instance.GetRunStopwatch() < timeRequirement && Run.instance.stageClearCount == 0 && base.isUserAlive)
		{
			Grant();
		}
	}

	protected override void OnBodyRequirementBroken()
	{
		TeleporterInteraction.onTeleporterChargedGlobal -= OnTeleporterChargedGlobal;
		base.OnBodyRequirementBroken();
	}
}
