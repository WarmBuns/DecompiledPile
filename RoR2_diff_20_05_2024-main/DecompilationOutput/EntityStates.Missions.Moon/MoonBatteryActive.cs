using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Missions.Moon;

public class MoonBatteryActive : MoonBatteryBaseState
{
	public static string soundEntryEvent;

	public static string soundLoopStartEvent;

	public static string soundLoopEndEvent;

	public static string soundExitEvent;

	public static string activeTriggerName;

	public static string completeTriggerName;

	private HoldoutZoneController holdoutZoneController;

	private ChargeIndicatorController chargeIndicatorController;

	private static int ActivecycleOffsetParamHash = Animator.StringToHash("Active.cycleOffset");

	public override void OnEnter()
	{
		base.OnEnter();
		holdoutZoneController = GetComponent<HoldoutZoneController>();
		holdoutZoneController.enabled = true;
		if (NetworkServer.active)
		{
			GetComponent<CombatDirector>().enabled = true;
		}
		Animator[] array = animators;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetTrigger(activeTriggerName);
		}
		Util.PlaySound(soundEntryEvent, base.gameObject);
		Util.PlaySound(soundLoopStartEvent, base.gameObject);
		FindModelChild("ChargingFX").gameObject.SetActive(value: true);
		Transform targetTransform = FindModelChild("PositionIndicatorPosition").transform;
		PositionIndicator component = Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/PositionIndicators/PillarChargingPositionIndicator"), base.transform.position, Quaternion.identity).GetComponent<PositionIndicator>();
		component.targetTransform = targetTransform;
		chargeIndicatorController = component.GetComponent<ChargeIndicatorController>();
		chargeIndicatorController.holdoutZoneController = holdoutZoneController;
	}

	public override void Update()
	{
		base.Update();
		if ((bool)holdoutZoneController)
		{
			Animator[] array = animators;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].SetFloat(ActivecycleOffsetParamHash, holdoutZoneController.charge * 0.99f);
			}
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && holdoutZoneController.charge >= 1f)
		{
			outer.SetNextState(new MoonBatteryComplete());
		}
	}

	public override void OnExit()
	{
		FindModelChild("ChargingFX").gameObject.SetActive(value: false);
		if ((bool)holdoutZoneController)
		{
			holdoutZoneController.enabled = false;
		}
		Animator[] array = animators;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetTrigger(completeTriggerName);
		}
		Util.PlaySound(soundLoopEndEvent, base.gameObject);
		Util.PlaySound(soundExitEvent, base.gameObject);
		if ((bool)chargeIndicatorController)
		{
			EntityState.Destroy(chargeIndicatorController.gameObject);
		}
		base.OnExit();
	}
}
