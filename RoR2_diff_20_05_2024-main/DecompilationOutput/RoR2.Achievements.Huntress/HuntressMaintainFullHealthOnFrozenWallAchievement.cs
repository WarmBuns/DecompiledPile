using System;

namespace RoR2.Achievements.Huntress;

[RegisterAchievement("HuntressMaintainFullHealthOnFrozenWall", "Skills.Huntress.Snipe", null, 3u, null)]
public class HuntressMaintainFullHealthOnFrozenWallAchievement : BaseAchievement
{
	private static readonly string[] requiredScenes = new string[2] { "frozenwall", "wispgraveyard" };

	private HealthComponent healthComponent;

	private bool failed;

	private bool sceneOk;

	private bool characterOk;

	private ToggleAction healthCheck;

	private ToggleAction teleporterCheck;

	protected override BodyIndex LookUpRequiredBodyIndex()
	{
		return BodyCatalog.FindBodyIndex("HuntressBody");
	}

	private void SubscribeHealthCheck()
	{
		RoR2Application.onFixedUpdate += CheckHealth;
	}

	private void UnsubscribeHealthCheck()
	{
		RoR2Application.onFixedUpdate -= CheckHealth;
	}

	private void SubscribeTeleporterCheck()
	{
		TeleporterInteraction.onTeleporterChargedGlobal += CheckTeleporter;
	}

	private void UnsubscribeTeleporterCheck()
	{
		TeleporterInteraction.onTeleporterChargedGlobal -= CheckTeleporter;
	}

	private void CheckTeleporter(TeleporterInteraction teleporterInteraction)
	{
		if (sceneOk && characterOk && !failed)
		{
			Grant();
		}
	}

	public override void OnInstall()
	{
		base.OnInstall();
		healthCheck = new ToggleAction(SubscribeHealthCheck, UnsubscribeHealthCheck);
		teleporterCheck = new ToggleAction(SubscribeTeleporterCheck, UnsubscribeTeleporterCheck);
		SceneCatalog.onMostRecentSceneDefChanged += OnMostRecentSceneDefChanged;
		base.localUser.onBodyChanged += OnBodyChanged;
	}

	public override void OnUninstall()
	{
		base.localUser.onBodyChanged -= OnBodyChanged;
		SceneCatalog.onMostRecentSceneDefChanged -= OnMostRecentSceneDefChanged;
		healthCheck.Dispose();
		teleporterCheck.Dispose();
		base.OnUninstall();
	}

	private void OnBodyChanged()
	{
		if (sceneOk && characterOk && !failed && (bool)base.localUser.cachedBody)
		{
			healthComponent = base.localUser.cachedBody.healthComponent;
			healthCheck.SetActive(newActive: true);
			teleporterCheck.SetActive(newActive: true);
		}
	}

	private void OnMostRecentSceneDefChanged(SceneDef sceneDef)
	{
		sceneOk = Array.IndexOf(requiredScenes, sceneDef.baseSceneName) != -1;
		if (sceneOk)
		{
			failed = false;
		}
	}

	protected override void OnBodyRequirementMet()
	{
		base.OnBodyRequirementMet();
		characterOk = true;
	}

	protected override void OnBodyRequirementBroken()
	{
		characterOk = false;
		Fail();
		base.OnBodyRequirementBroken();
	}

	private void Fail()
	{
		failed = true;
		healthCheck.SetActive(newActive: false);
		teleporterCheck.SetActive(newActive: false);
	}

	private void CheckHealth()
	{
		if ((bool)healthComponent && healthComponent.combinedHealth < healthComponent.fullCombinedHealth)
		{
			Fail();
		}
	}
}
