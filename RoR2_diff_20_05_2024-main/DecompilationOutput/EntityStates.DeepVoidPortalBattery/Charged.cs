using RoR2;
using UnityEngine.Networking;

namespace EntityStates.DeepVoidPortalBattery;

public class Charged : BaseDeepVoidPortalBatteryState
{
	public override void OnEnter()
	{
		base.OnEnter();
		if ((bool)VoidStageMissionController.instance && NetworkServer.active)
		{
			VoidStageMissionController.instance.OnBatteryActivated();
		}
	}
}
