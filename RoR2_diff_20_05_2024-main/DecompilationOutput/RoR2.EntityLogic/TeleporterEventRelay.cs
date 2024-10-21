using UnityEngine;
using UnityEngine.Events;

namespace RoR2.EntityLogic;

public class TeleporterEventRelay : MonoBehaviour
{
	public UnityEvent onTeleporterBeginCharging;

	public UnityEvent onTeleporterCharged;

	public UnityEvent onTeleporterFinish;

	private TeleporterInteraction.ActivationState recordedActivationState;

	public void OnEnable()
	{
		TeleporterInteraction.onTeleporterBeginChargingGlobal += CheckTeleporterBeginCharging;
		TeleporterInteraction.onTeleporterChargedGlobal += CheckTeleporterCharged;
		TeleporterInteraction.onTeleporterFinishGlobal += CheckTeleporterFinish;
		CheckTeleporterBeginCharging(TeleporterInteraction.instance);
		CheckTeleporterCharged(TeleporterInteraction.instance);
		CheckTeleporterFinish(TeleporterInteraction.instance);
	}

	public void OnDisable()
	{
		TeleporterInteraction.onTeleporterBeginChargingGlobal -= CheckTeleporterBeginCharging;
		TeleporterInteraction.onTeleporterChargedGlobal -= CheckTeleporterCharged;
		TeleporterInteraction.onTeleporterFinishGlobal -= CheckTeleporterFinish;
	}

	private void CheckTeleporterBeginCharging(TeleporterInteraction teleporter)
	{
		if ((bool)teleporter && teleporter.activationState >= TeleporterInteraction.ActivationState.Charging && recordedActivationState < TeleporterInteraction.ActivationState.Charging)
		{
			onTeleporterBeginCharging?.Invoke();
			recordedActivationState = TeleporterInteraction.ActivationState.Charging;
		}
	}

	private void CheckTeleporterCharged(TeleporterInteraction teleporter)
	{
		if ((bool)teleporter && teleporter.activationState >= TeleporterInteraction.ActivationState.Charged && recordedActivationState < TeleporterInteraction.ActivationState.Charged)
		{
			onTeleporterCharged?.Invoke();
			recordedActivationState = TeleporterInteraction.ActivationState.Charged;
		}
	}

	private void CheckTeleporterFinish(TeleporterInteraction teleporter)
	{
		if ((bool)teleporter && teleporter.activationState >= TeleporterInteraction.ActivationState.Finished && recordedActivationState < TeleporterInteraction.ActivationState.Finished)
		{
			onTeleporterFinish?.Invoke();
			recordedActivationState = TeleporterInteraction.ActivationState.Finished;
		}
	}
}
