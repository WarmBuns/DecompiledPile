using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.DeepVoidPortalBattery;

public class Charging : BaseDeepVoidPortalBatteryState
{
	[SerializeField]
	public GameObject chargingPositionIndicator;

	private CombatDirector combatDirector;

	private HoldoutZoneController holdoutZoneController;

	private VoidStageMissionController.FogRequest fogRequest;

	private ChargeIndicatorController chargeIndicatorController;

	public override void OnEnter()
	{
		base.OnEnter();
		holdoutZoneController = GetComponent<HoldoutZoneController>();
		if ((bool)holdoutZoneController)
		{
			holdoutZoneController.enabled = true;
		}
		Transform targetTransform = FindModelChild("PositionIndicatorPosition").transform;
		PositionIndicator component = Object.Instantiate(chargingPositionIndicator, base.transform.position, Quaternion.identity).GetComponent<PositionIndicator>();
		component.targetTransform = targetTransform;
		chargeIndicatorController = component.GetComponent<ChargeIndicatorController>();
		chargeIndicatorController.holdoutZoneController = holdoutZoneController;
		if (NetworkServer.active)
		{
			combatDirector = GetComponent<CombatDirector>();
			if ((bool)combatDirector)
			{
				combatDirector.enabled = true;
				combatDirector.SetNextSpawnAsBoss();
			}
			if ((bool)holdoutZoneController && (bool)VoidStageMissionController.instance)
			{
				fogRequest = VoidStageMissionController.instance.RequestFog(holdoutZoneController);
			}
		}
	}

	public override void OnExit()
	{
		if ((bool)chargeIndicatorController)
		{
			EntityState.Destroy(chargeIndicatorController.gameObject);
		}
		if ((bool)holdoutZoneController)
		{
			holdoutZoneController.enabled = false;
		}
		if (NetworkServer.active)
		{
			if ((bool)combatDirector)
			{
				combatDirector.enabled = false;
			}
			fogRequest?.Dispose();
			fogRequest = null;
		}
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && holdoutZoneController.charge >= 1f)
		{
			outer.SetNextState(new Charged());
		}
	}
}
