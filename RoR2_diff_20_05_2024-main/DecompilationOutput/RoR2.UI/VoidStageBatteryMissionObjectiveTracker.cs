namespace RoR2.UI;

public class VoidStageBatteryMissionObjectiveTracker : ObjectivePanelController.ObjectiveTracker
{
	private int numBatteriesActivated = -1;

	protected override string GenerateString()
	{
		VoidStageMissionController voidStageMissionController = (VoidStageMissionController)sourceDescriptor.source;
		numBatteriesActivated = voidStageMissionController.numBatteriesActivated;
		return string.Format(Language.GetString(voidStageMissionController.batteryObjectiveToken), numBatteriesActivated, voidStageMissionController.numBatteriesSpawned);
	}

	protected override bool IsDirty()
	{
		return ((VoidStageMissionController)sourceDescriptor.source).numBatteriesActivated != numBatteriesActivated;
	}
}
