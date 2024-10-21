using UnityEngine;

namespace RoR2;

public class WheelVehicleMotor : MonoBehaviour
{
	[HideInInspector]
	public Vector3 moveVector;

	public WheelCollider[] driveWheels;

	public WheelCollider[] steerWheels;

	public float motorTorque;

	public float maxSteerAngle;

	public float wheelMass = 20f;

	public float wheelRadius = 0.5f;

	public float wheelWellDistance = 2.7f;

	public float wheelSuspensionDistance = 0.3f;

	public float wheelForceAppPointDistance;

	public float wheelSuspensionSpringSpring = 35000f;

	public float wheelSuspensionSpringDamper = 4500f;

	public float wheelSuspensionSpringTargetPosition = 0.5f;

	public float forwardFrictionExtremumSlip = 0.4f;

	public float forwardFrictionValue = 1f;

	public float forwardFrictionAsymptoticSlip = 0.8f;

	public float forwardFrictionAsymptoticValue = 0.5f;

	public float forwardFrictionStiffness = 1f;

	public float sidewaysFrictionExtremumSlip = 0.2f;

	public float sidewaysFrictionValue = 1f;

	public float sidewaysFrictionAsymptoticSlip = 0.5f;

	public float sidewaysFrictionAsymptoticValue = 0.75f;

	public float sidewaysFrictionStiffness = 1f;

	private InputBankTest inputBank;

	private void Start()
	{
		inputBank = GetComponent<InputBankTest>();
	}

	private void UpdateWheelParameter(WheelCollider wheel)
	{
		wheel.mass = wheelMass;
		wheel.radius = wheelRadius;
		wheel.suspensionDistance = wheelSuspensionDistance;
		wheel.forceAppPointDistance = wheelForceAppPointDistance;
		wheel.transform.localPosition = new Vector3(wheel.transform.localPosition.x, 0f - wheelWellDistance, wheel.transform.localPosition.z);
		JointSpring suspensionSpring = default(JointSpring);
		suspensionSpring.spring = wheelSuspensionSpringSpring;
		suspensionSpring.damper = wheelSuspensionSpringDamper;
		suspensionSpring.targetPosition = wheelSuspensionSpringTargetPosition;
		wheel.suspensionSpring = suspensionSpring;
		WheelFrictionCurve forwardFriction = default(WheelFrictionCurve);
		forwardFriction.extremumSlip = forwardFrictionExtremumSlip;
		forwardFriction.extremumValue = forwardFrictionValue;
		forwardFriction.asymptoteSlip = forwardFrictionAsymptoticSlip;
		forwardFriction.asymptoteValue = forwardFrictionAsymptoticValue;
		forwardFriction.stiffness = forwardFrictionStiffness;
		wheel.forwardFriction = forwardFriction;
		WheelFrictionCurve sidewaysFriction = default(WheelFrictionCurve);
		sidewaysFriction.extremumSlip = sidewaysFrictionExtremumSlip;
		sidewaysFriction.extremumValue = sidewaysFrictionValue;
		sidewaysFriction.asymptoteSlip = sidewaysFrictionAsymptoticSlip;
		sidewaysFriction.asymptoteValue = sidewaysFrictionAsymptoticValue;
		sidewaysFriction.stiffness = sidewaysFrictionStiffness;
		wheel.sidewaysFriction = sidewaysFriction;
	}

	private void UpdateAllWheelParameters()
	{
		WheelCollider[] array = driveWheels;
		foreach (WheelCollider wheel in array)
		{
			UpdateWheelParameter(wheel);
		}
		array = steerWheels;
		foreach (WheelCollider wheel2 in array)
		{
			UpdateWheelParameter(wheel2);
		}
	}

	private void FixedUpdate()
	{
		UpdateAllWheelParameters();
		if ((bool)inputBank)
		{
			moveVector = inputBank.moveVector;
			float f = 0f;
			if (moveVector.sqrMagnitude > 0f)
			{
				f = Util.AngleSigned(base.transform.forward, moveVector, Vector3.up);
			}
			WheelCollider[] array = steerWheels;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].steerAngle = Mathf.Min(maxSteerAngle, Mathf.Abs(f)) * Mathf.Sign(f);
			}
			array = driveWheels;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].motorTorque = moveVector.magnitude * motorTorque;
			}
		}
	}
}
