using Assets.RoR2.Scripts.Platform;

namespace RoR2.Achievements.Railgunner;

[RegisterAchievement("RailgunnerDealMassiveDamage", "Skills.Railgunner.UtilityAlt1", null, 3u, null)]
public class RailgunnerDealMassiveDamageAchievement : BaseAchievement
{
	private const float minimumDamage = 1000000f;

	protected override BodyIndex LookUpRequiredBodyIndex()
	{
		return BodyCatalog.FindBodyIndex("RailgunnerBody");
	}

	protected override void OnBodyRequirementMet()
	{
		GlobalEventManager.onClientDamageNotified += onClientDamageNotified;
	}

	protected override void OnBodyRequirementBroken()
	{
		GlobalEventManager.onClientDamageNotified -= onClientDamageNotified;
	}

	private void onClientDamageNotified(DamageDealtMessage message)
	{
		if ((object)message.attacker == base.localUser.cachedBodyObject && message.damage >= 1000000f)
		{
			Grant();
			TryToCompleteActivity();
		}
	}

	public override void TryToCompleteActivity()
	{
		if (base.localUser.id == LocalUserManager.GetFirstLocalUser().id && shouldGrant)
		{
			BaseActivitySelector baseActivitySelector = new BaseActivitySelector();
			baseActivitySelector.activityAchievementID = "RailgunnerDealMassiveDamage";
			PlatformSystems.activityManager.TryToCompleteActivity(baseActivitySelector);
		}
	}
}
