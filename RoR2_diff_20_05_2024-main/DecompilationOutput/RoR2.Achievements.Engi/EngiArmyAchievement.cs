using Assets.RoR2.Scripts.Platform;
using UnityEngine.Networking;

namespace RoR2.Achievements.Engi;

[RegisterAchievement("EngiArmy", "Skills.Engi.WalkerTurret", "Complete30StagesCareer", 3u, null)]
public class EngiArmyAchievement : BaseAchievement
{
	private static readonly int requirement = 12;

	private ToggleAction monitorMinions;

	protected override BodyIndex LookUpRequiredBodyIndex()
	{
		return BodyCatalog.FindBodyIndex("EngiBody");
	}

	private void SubscribeToMinionChanges()
	{
		MinionOwnership.onMinionGroupChangedGlobal += OnMinionGroupChangedGlobal;
	}

	private void UnsubscribeFromMinionChanges()
	{
		MinionOwnership.onMinionGroupChangedGlobal -= OnMinionGroupChangedGlobal;
	}

	private void OnMinionGroupChangedGlobal(MinionOwnership minion)
	{
		if (requirement > (minion.group?.memberCount ?? 0))
		{
			return;
		}
		CharacterMaster master = base.localUser.cachedMasterController.master;
		if ((bool)master)
		{
			NetworkInstanceId netId = master.netId;
			if (minion.group.ownerId == netId)
			{
				Grant();
				TryToCompleteActivity();
			}
		}
	}

	public override void TryToCompleteActivity()
	{
		if (base.localUser.id == LocalUserManager.GetFirstLocalUser().id && shouldGrant)
		{
			BaseActivitySelector baseActivitySelector = new BaseActivitySelector();
			baseActivitySelector.activityAchievementID = "EngiArmy";
			PlatformSystems.activityManager.TryToCompleteActivity(baseActivitySelector);
		}
	}

	public override void OnInstall()
	{
		base.OnInstall();
		monitorMinions = new ToggleAction(SubscribeToMinionChanges, UnsubscribeFromMinionChanges);
	}

	protected override void OnBodyRequirementMet()
	{
		base.OnBodyRequirementMet();
		monitorMinions.SetActive(newActive: true);
	}

	protected override void OnBodyRequirementBroken()
	{
		monitorMinions.SetActive(newActive: false);
		base.OnBodyRequirementBroken();
	}
}
