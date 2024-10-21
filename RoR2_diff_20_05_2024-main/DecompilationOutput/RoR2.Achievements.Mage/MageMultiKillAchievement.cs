using Assets.RoR2.Scripts.Platform;

namespace RoR2.Achievements.Mage;

[RegisterAchievement("MageMultiKill", "Skills.Mage.LightningBolt", "FreeMage", 3u, typeof(MageMultiKillServerAchievement))]
public class MageMultiKillAchievement : BaseAchievement
{
	private class MageMultiKillServerAchievement : BaseServerAchievement
	{
		public override void OnInstall()
		{
			base.OnInstall();
			RoR2Application.onFixedUpdate += OnFixedUpdate;
		}

		public override void OnUninstall()
		{
			RoR2Application.onFixedUpdate -= OnFixedUpdate;
			base.OnUninstall();
		}

		private void OnFixedUpdate()
		{
			CharacterBody currentBody = GetCurrentBody();
			if ((bool)currentBody && requirement <= currentBody.multiKillCount)
			{
				Grant();
				ServerTryToCompleteActivity();
			}
		}
	}

	private static readonly int requirement = 20;

	protected override BodyIndex LookUpRequiredBodyIndex()
	{
		return BodyCatalog.FindBodyIndex("MageBody");
	}

	protected override void OnBodyRequirementMet()
	{
		base.OnBodyRequirementMet();
		SetServerTracked(shouldTrack: true);
	}

	protected override void OnBodyRequirementBroken()
	{
		SetServerTracked(shouldTrack: false);
		base.OnBodyRequirementBroken();
	}

	public override void TryToCompleteActivity()
	{
		if (base.localUser.id == LocalUserManager.GetFirstLocalUser().id && shouldGrant)
		{
			BaseActivitySelector baseActivitySelector = new BaseActivitySelector();
			baseActivitySelector.activityAchievementID = "MageMultiKill";
			PlatformSystems.activityManager.TryToCompleteActivity(baseActivitySelector);
		}
	}
}
