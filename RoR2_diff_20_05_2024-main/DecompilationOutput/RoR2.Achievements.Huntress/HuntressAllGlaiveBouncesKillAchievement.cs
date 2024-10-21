using Assets.RoR2.Scripts.Platform;
using RoR2.Orbs;

namespace RoR2.Achievements.Huntress;

[RegisterAchievement("HuntressAllGlaiveBouncesKill", "Skills.Huntress.FlurryArrow", null, 3u, typeof(HuntressAllGlaiveBouncesKillServerAchievement))]
public class HuntressAllGlaiveBouncesKillAchievement : BaseAchievement
{
	private class HuntressAllGlaiveBouncesKillServerAchievement : BaseServerAchievement
	{
		public override void OnInstall()
		{
			base.OnInstall();
			LightningOrb.onLightningOrbKilledOnAllBounces += OnLightningOrbKilledOnAllBounces;
		}

		private void OnLightningOrbKilledOnAllBounces(LightningOrb lightningOrb)
		{
			CharacterBody currentBody = base.networkUser.GetCurrentBody();
			if ((bool)currentBody && lightningOrb.attacker == currentBody.gameObject && lightningOrb.lightningType == LightningOrb.LightningType.HuntressGlaive)
			{
				Grant();
				ServerTryToCompleteActivity();
			}
		}

		public override void OnUninstall()
		{
			LightningOrb.onLightningOrbKilledOnAllBounces -= OnLightningOrbKilledOnAllBounces;
			base.OnUninstall();
		}
	}

	protected override BodyIndex LookUpRequiredBodyIndex()
	{
		return BodyCatalog.FindBodyIndex("HuntressBody");
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
		bool flag = base.localUser.id == LocalUserManager.GetFirstLocalUser().id;
		if (shouldGrant && flag)
		{
			BaseActivitySelector baseActivitySelector = new BaseActivitySelector();
			baseActivitySelector.activityAchievementID = "HuntressAllGlaiveBouncesKill";
			PlatformSystems.activityManager.TryToCompleteActivity(baseActivitySelector);
		}
	}
}
