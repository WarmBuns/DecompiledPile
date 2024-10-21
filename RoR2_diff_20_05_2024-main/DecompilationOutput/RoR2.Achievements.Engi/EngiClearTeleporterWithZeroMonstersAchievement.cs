using Assets.RoR2.Scripts.Platform;

namespace RoR2.Achievements.Engi;

[RegisterAchievement("EngiClearTeleporterWithZeroMonsters", "Skills.Engi.Harpoon", "Complete30StagesCareer", 3u, null)]
public class EngiClearTeleporterWithZeroMonstersAchievement : BaseAchievement
{
	protected override BodyIndex LookUpRequiredBodyIndex()
	{
		return BodyCatalog.FindBodyIndex("EngiBody");
	}

	protected override void OnBodyRequirementMet()
	{
		base.OnBodyRequirementMet();
		TeleporterInteraction.onTeleporterChargedGlobal += OnTeleporterChargedGlobal;
	}

	protected override void OnBodyRequirementBroken()
	{
		TeleporterInteraction.onTeleporterChargedGlobal -= OnTeleporterChargedGlobal;
		base.OnBodyRequirementBroken();
	}

	private void OnTeleporterChargedGlobal(TeleporterInteraction teleporterInteraction)
	{
		if (!base.isUserAlive)
		{
			return;
		}
		foreach (TeamComponent teamMember in TeamComponent.GetTeamMembers(TeamIndex.Monster))
		{
			if (teamMember.body.healthComponent.alive)
			{
				return;
			}
		}
		Grant();
		TryToCompleteActivity();
	}

	public override void TryToCompleteActivity()
	{
		if (base.localUser.id == LocalUserManager.GetFirstLocalUser().id && shouldGrant)
		{
			BaseActivitySelector baseActivitySelector = new BaseActivitySelector();
			baseActivitySelector.activityAchievementID = "EngiClearTeleporterWithZeroMonsters";
			PlatformSystems.activityManager.TryToCompleteActivity(baseActivitySelector);
		}
	}
}
