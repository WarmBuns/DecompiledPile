using Assets.RoR2.Scripts.Platform;
using UnityEngine;

namespace RoR2.Achievements.Merc;

[RegisterAchievement("MercDontTouchGround", "Skills.Merc.Uppercut", "CompleteUnknownEnding", 3u, null)]
public class MercDontTouchGroundAchievement : BaseAchievement
{
	private static readonly float requirement = 30f;

	private CharacterMotor motor;

	private CharacterBody body;

	private float stopwatch;

	protected override BodyIndex LookUpRequiredBodyIndex()
	{
		return BodyCatalog.FindBodyIndex("MercBody");
	}

	protected override void OnBodyRequirementMet()
	{
		base.OnBodyRequirementMet();
		RoR2Application.onFixedUpdate += MercFixedUpdate;
		base.localUser.onBodyChanged += OnBodyChanged;
		OnBodyChanged();
	}

	protected override void OnBodyRequirementBroken()
	{
		base.localUser.onBodyChanged -= OnBodyChanged;
		RoR2Application.onFixedUpdate -= MercFixedUpdate;
		base.OnBodyRequirementBroken();
	}

	private void OnBodyChanged()
	{
		body = base.localUser.cachedBody;
		motor = (body ? body.characterMotor : null);
	}

	public override void TryToCompleteActivity()
	{
		if (base.localUser.id == LocalUserManager.GetFirstLocalUser().id && shouldGrant)
		{
			BaseActivitySelector baseActivitySelector = new BaseActivitySelector();
			baseActivitySelector.activityAchievementID = "MercDontTouchGround";
			PlatformSystems.activityManager.TryToCompleteActivity(baseActivitySelector);
		}
	}

	private void MercFixedUpdate()
	{
		bool flag = (bool)motor && !motor.isGrounded && !body.currentVehicle;
		stopwatch = (flag ? (stopwatch + Time.fixedDeltaTime) : 0f);
		if (requirement <= stopwatch)
		{
			Grant();
			TryToCompleteActivity();
		}
	}
}
