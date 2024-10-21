using System;
using EntityStates.InfiniteTowerSafeWard;
using RoR2.UI;
using UnityEngine;

namespace RoR2;

public class InfiniteTowerSafeWardController : MonoBehaviour
{
	[SerializeField]
	private EntityStateMachine wardStateMachine;

	[SerializeField]
	private VerticalTubeZone _safeZone;

	[SerializeField]
	private GameObject positionIndicatorPrefab;

	[SerializeField]
	private HoldoutZoneController holdoutZoneController;

	private PositionIndicator positionIndicator;

	private ChargeIndicatorController chargeIndicatorController;

	public IZone safeZone => _safeZone;

	public bool isAwaitingInteraction
	{
		get
		{
			if ((bool)wardStateMachine)
			{
				return wardStateMachine.state is AwaitingActivation;
			}
			return false;
		}
	}

	public event Action<InfiniteTowerSafeWardController> onActivated;

	private void Awake()
	{
		if (!positionIndicatorPrefab)
		{
			return;
		}
		GameObject gameObject = UnityEngine.Object.Instantiate(positionIndicatorPrefab, base.transform.position, Quaternion.identity);
		if ((bool)gameObject)
		{
			positionIndicator = gameObject.GetComponent<PositionIndicator>();
			if ((bool)positionIndicator)
			{
				positionIndicator.targetTransform = base.transform;
			}
			chargeIndicatorController = gameObject.GetComponent<ChargeIndicatorController>();
			if ((bool)chargeIndicatorController)
			{
				chargeIndicatorController.holdoutZoneController = holdoutZoneController;
			}
			gameObject.SetActive(value: false);
		}
	}

	public void Activate()
	{
		if ((bool)wardStateMachine && wardStateMachine.state is AwaitingActivation awaitingActivation)
		{
			awaitingActivation.Activate();
			this.onActivated?.Invoke(this);
		}
	}

	public void SelfDestruct()
	{
		if ((bool)wardStateMachine && wardStateMachine.state is Active active)
		{
			active.SelfDestruct();
		}
	}

	public void RandomizeLocation(Xoroshiro128Plus rng)
	{
		if ((bool)wardStateMachine)
		{
			wardStateMachine.SetNextState(new Unburrow(rng));
		}
	}

	public void WaitForPortal()
	{
		if ((bool)wardStateMachine)
		{
			wardStateMachine.SetNextState(new AwaitingPortalUse());
		}
	}

	public void SetIndicatorEnabled(bool enabled)
	{
		if ((bool)positionIndicator)
		{
			positionIndicator.gameObject.SetActive(enabled);
		}
	}
}
