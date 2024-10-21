namespace RoR2.Achievements;

[RegisterAchievement("NeverBackDown", "Items.FocusConvergence", null, 2u, typeof(NeverBackDown))]
public class NeverBackDown : BaseAchievement
{
	private bool _checkingForFailure;

	private static readonly int requirement = 4;

	private bool hasLeftRadius;

	private int levels;

	private Run.FixedTimeStamp teleporterStartChargingTime = Run.FixedTimeStamp.negativeInfinity;

	private static readonly float gracePeriod = 2f;

	private bool checkingForFailure
	{
		get
		{
			return _checkingForFailure;
		}
		set
		{
			if (_checkingForFailure != value)
			{
				_checkingForFailure = value;
				if (_checkingForFailure)
				{
					RoR2Application.onFixedUpdate += CheckForFailure;
				}
				else
				{
					RoR2Application.onFixedUpdate -= CheckForFailure;
				}
			}
		}
	}

	public override void OnInstall()
	{
		base.OnInstall();
		TeleporterInteraction.onTeleporterBeginChargingGlobal += OnTeleporterBeginCharging;
		TeleporterInteraction.onTeleporterChargedGlobal += OnTeleporterCharged;
		Run.onRunStartGlobal += OnRunStartGlobal;
	}

	public override void OnUninstall()
	{
		checkingForFailure = false;
		Run.onRunStartGlobal -= OnRunStartGlobal;
		TeleporterInteraction.onTeleporterBeginChargingGlobal -= OnTeleporterBeginCharging;
		TeleporterInteraction.onTeleporterChargedGlobal -= OnTeleporterCharged;
		base.OnUninstall();
	}

	private void OnRunStartGlobal(Run run)
	{
		levels = 0;
	}

	private void OnTeleporterBeginCharging(TeleporterInteraction teleporterInteraction)
	{
		hasLeftRadius = false;
		teleporterStartChargingTime = Run.FixedTimeStamp.now;
		checkingForFailure = true;
	}

	private void OnTeleporterCharged(TeleporterInteraction teleporterInteraction)
	{
		if (!hasLeftRadius)
		{
			levels++;
		}
		if (levels >= requirement)
		{
			Grant();
		}
	}

	private void CheckForFailure()
	{
		if (!TeleporterInteraction.instance || TeleporterInteraction.instance.isCharged)
		{
			checkingForFailure = false;
		}
		else if ((bool)base.localUser.cachedBody && (teleporterStartChargingTime + gracePeriod).hasPassed && !TeleporterInteraction.instance.holdoutZoneController.IsBodyInChargingRadius(base.localUser.cachedBody))
		{
			hasLeftRadius = true;
			levels = 0;
			checkingForFailure = false;
		}
	}
}
