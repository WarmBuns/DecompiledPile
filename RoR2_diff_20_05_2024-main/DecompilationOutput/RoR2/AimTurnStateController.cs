using EntityStates;
using UnityEngine;

namespace RoR2;

public class AimTurnStateController : MonoBehaviour
{
	[SerializeField]
	[Tooltip("The component we use to determine the current orientation")]
	private CharacterDirection characterDirection;

	[SerializeField]
	[Tooltip("The component we use to determine the current aim")]
	private InputBankTest inputBank;

	[SerializeField]
	[Tooltip("The state machine we should modify")]
	private EntityStateMachine targetStateMachine;

	[SerializeField]
	[Tooltip("The state we should push")]
	private SerializableEntityStateType turnStateType;

	[SerializeField]
	[Tooltip("The priority of the new state")]
	private InterruptPriority interruptPriority;

	[SerializeField]
	[Tooltip("The minimum difference between the current orientation and the aim before we should push the state")]
	private float minTriggerDegrees;

	[SerializeField]
	[Tooltip("The minimum time before we should push the state again")]
	private float retriggerDelaySeconds;

	[SerializeField]
	[Tooltip("The aim/direction vectors are multiplied by this vector and normalized before comparison.  This can be used to exclude a dimension from the calculation.")]
	private Vector3 aimScale = new Vector3(1f, 1f, 1f);

	private float lastTriggerTime;

	private void FixedUpdate()
	{
		if (!(Run.instance.fixedTime - lastTriggerTime > retriggerDelaySeconds))
		{
			return;
		}
		Vector3 aimDirection = inputBank.aimDirection;
		aimDirection.Scale(aimScale);
		aimDirection.Normalize();
		Vector3 forward = characterDirection.forward;
		forward.Scale(aimScale);
		forward.Normalize();
		if (Vector3.Angle(aimDirection, forward) > minTriggerDegrees)
		{
			lastTriggerTime = Run.instance.fixedTime;
			if ((bool)targetStateMachine)
			{
				EntityState newNextState = EntityStateCatalog.InstantiateState(ref turnStateType);
				targetStateMachine.SetInterruptState(newNextState, interruptPriority);
			}
		}
	}
}
