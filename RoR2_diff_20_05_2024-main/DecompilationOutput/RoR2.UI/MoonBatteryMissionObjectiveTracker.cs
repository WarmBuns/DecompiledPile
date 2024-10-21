namespace RoR2.UI;

public class MoonBatteryMissionObjectiveTracker : ObjectivePanelController.ObjectiveTracker
{
	private int numChargedBatteries = -1;

	protected override string GenerateString()
	{
		MoonBatteryMissionController moonBatteryMissionController = (MoonBatteryMissionController)sourceDescriptor.source;
		numChargedBatteries = moonBatteryMissionController.numChargedBatteries;
		return string.Format(Language.GetString(moonBatteryMissionController.objectiveToken), numChargedBatteries, moonBatteryMissionController.numRequiredBatteries);
	}

	protected override bool IsDirty()
	{
		return ((MoonBatteryMissionController)sourceDescriptor.source).numChargedBatteries != numChargedBatteries;
	}
}
