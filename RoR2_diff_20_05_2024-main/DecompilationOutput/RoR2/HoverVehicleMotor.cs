using System;
using UnityEngine;

namespace RoR2;

[RequireComponent(typeof(Rigidbody))]
public class HoverVehicleMotor : MonoBehaviour
{
	private enum WheelLateralAxis
	{
		Left,
		Right
	}

	public enum WheelLongitudinalAxis
	{
		Front,
		Back
	}

	[Serializable]
	public struct AxleGroup
	{
		public HoverEngine leftWheel;

		public HoverEngine rightWheel;

		public WheelLongitudinalAxis wheelLongitudinalAxis;

		public bool isDriven;

		public AnimationCurve slidingTractionCurve;
	}

	[HideInInspector]
	public Vector3 targetSteerVector;

	public Vector3 centerOfMassOffset;

	public AxleGroup[] staticAxles;

	public AxleGroup[] steerAxles;

	public float wheelWellDepth;

	public float wheelBase;

	public float trackWidth;

	public float rollingFrictionCoefficient;

	public float slidingTractionCoefficient;

	public float motorForce;

	public float maxSteerAngle;

	public float maxTurningRadius;

	public float hoverForce = 33f;

	public float hoverHeight = 2f;

	public float hoverDamping = 0.5f;

	public float hoverRadius = 0.5f;

	public Vector3 hoverOffsetVector = Vector3.up;

	private InputBankTest inputBank;

	private Vector3 steerVector = Vector3.forward;

	private Rigidbody rigidbody;

	private void Start()
	{
		inputBank = GetComponent<InputBankTest>();
		rigidbody = GetComponent<Rigidbody>();
	}

	private void ApplyWheelForces(HoverEngine wheel, float gas, bool driveWheel, AnimationCurve slidingWheelTractionCurve)
	{
		if (wheel.isGrounded)
		{
			float num = 0.005f;
			Transform transform = wheel.transform;
			float num2 = 1f;
			Vector3 position = transform.position;
			Vector3 pointVelocity = rigidbody.GetPointVelocity(position);
			Vector3 vector = Vector3.Project(pointVelocity, transform.right);
			Vector3 vector2 = Vector3.Project(pointVelocity, transform.forward);
			Vector3 up = Vector3.up;
			Debug.DrawRay(position, pointVelocity, Color.blue);
			Vector3 vector3 = Vector3.zero;
			if (driveWheel)
			{
				vector3 = transform.forward * gas * motorForce;
				rigidbody.AddForceAtPosition(transform.forward * gas * motorForce * num2, position);
				Debug.DrawRay(position, vector3 * num, Color.yellow);
			}
			Vector3 vector4 = Vector3.ProjectOnPlane(-vector2 * rollingFrictionCoefficient * num2, up);
			rigidbody.AddForceAtPosition(vector4, position);
			Debug.DrawRay(position, vector4 * num, Color.red);
			Vector3 vector5 = Vector3.ProjectOnPlane(-vector * slidingWheelTractionCurve.Evaluate(pointVelocity.magnitude) * slidingTractionCoefficient * num2, up);
			rigidbody.AddForceAtPosition(vector5, position);
			Debug.DrawRay(position, vector5 * num, Color.red);
			Debug.DrawRay(position, (vector3 + vector4 + vector5) * num, Color.green);
		}
	}

	private void UpdateCenterOfMass()
	{
		rigidbody.ResetCenterOfMass();
		rigidbody.centerOfMass += centerOfMassOffset;
	}

	private void UpdateWheelParameter(HoverEngine wheel, WheelLateralAxis wheelLateralAxis, WheelLongitudinalAxis wheelLongitudinalAxis)
	{
		wheel.hoverForce = hoverForce;
		wheel.hoverDamping = hoverDamping;
		wheel.hoverHeight = hoverHeight;
		wheel.offsetVector = hoverOffsetVector;
		wheel.hoverRadius = hoverRadius;
		Vector3 zero = Vector3.zero;
		zero.y = 0f - wheelWellDepth;
		switch (wheelLateralAxis)
		{
		case WheelLateralAxis.Left:
			zero.x = (0f - trackWidth) / 2f;
			break;
		case WheelLateralAxis.Right:
			zero.x = trackWidth / 2f;
			break;
		}
		switch (wheelLongitudinalAxis)
		{
		case WheelLongitudinalAxis.Front:
			zero.z = wheelBase / 2f;
			break;
		case WheelLongitudinalAxis.Back:
			zero.z = (0f - wheelBase) / 2f;
			break;
		}
		wheel.transform.localPosition = zero;
	}

	private void UpdateAllWheelParameters()
	{
		AxleGroup[] array = staticAxles;
		for (int i = 0; i < array.Length; i++)
		{
			AxleGroup axleGroup = array[i];
			HoverEngine leftWheel = axleGroup.leftWheel;
			HoverEngine rightWheel = axleGroup.rightWheel;
			UpdateWheelParameter(leftWheel, WheelLateralAxis.Left, axleGroup.wheelLongitudinalAxis);
			UpdateWheelParameter(rightWheel, WheelLateralAxis.Right, axleGroup.wheelLongitudinalAxis);
		}
		array = steerAxles;
		for (int i = 0; i < array.Length; i++)
		{
			AxleGroup axleGroup2 = array[i];
			HoverEngine leftWheel2 = axleGroup2.leftWheel;
			HoverEngine rightWheel2 = axleGroup2.rightWheel;
			UpdateWheelParameter(leftWheel2, WheelLateralAxis.Left, axleGroup2.wheelLongitudinalAxis);
			UpdateWheelParameter(rightWheel2, WheelLateralAxis.Right, axleGroup2.wheelLongitudinalAxis);
		}
	}

	private void FixedUpdate()
	{
		UpdateCenterOfMass();
		UpdateAllWheelParameters();
		if (!inputBank)
		{
			return;
		}
		Vector3 moveVector = inputBank.moveVector;
		Vector3 normalized = Vector3.ProjectOnPlane(inputBank.aimDirection, base.transform.up).normalized;
		float num = Mathf.Clamp(Util.AngleSigned(base.transform.forward, normalized, base.transform.up), 0f - maxSteerAngle, maxSteerAngle);
		float magnitude = moveVector.magnitude;
		AxleGroup[] array = staticAxles;
		for (int i = 0; i < array.Length; i++)
		{
			AxleGroup axleGroup = array[i];
			HoverEngine leftWheel = axleGroup.leftWheel;
			HoverEngine rightWheel = axleGroup.rightWheel;
			ApplyWheelForces(leftWheel, magnitude, axleGroup.isDriven, axleGroup.slidingTractionCurve);
			ApplyWheelForces(rightWheel, magnitude, axleGroup.isDriven, axleGroup.slidingTractionCurve);
		}
		array = steerAxles;
		for (int i = 0; i < array.Length; i++)
		{
			AxleGroup axleGroup2 = array[i];
			HoverEngine leftWheel2 = axleGroup2.leftWheel;
			HoverEngine rightWheel2 = axleGroup2.rightWheel;
			float num2 = maxTurningRadius / Mathf.Abs(num / maxSteerAngle);
			float num3 = Mathf.Atan(wheelBase / (num2 - trackWidth / 2f)) * 57.29578f;
			float num4 = Mathf.Atan(wheelBase / (num2 + trackWidth / 2f)) * 57.29578f;
			Quaternion localRotation = Quaternion.Euler(0f, num3 * Mathf.Sign(num), 0f);
			Quaternion localRotation2 = Quaternion.Euler(0f, num4 * Mathf.Sign(num), 0f);
			if (num <= 0f)
			{
				leftWheel2.transform.localRotation = localRotation;
				rightWheel2.transform.localRotation = localRotation2;
			}
			else
			{
				leftWheel2.transform.localRotation = localRotation2;
				rightWheel2.transform.localRotation = localRotation;
			}
			ApplyWheelForces(leftWheel2, magnitude, axleGroup2.isDriven, axleGroup2.slidingTractionCurve);
			ApplyWheelForces(rightWheel2, magnitude, axleGroup2.isDriven, axleGroup2.slidingTractionCurve);
		}
		Debug.DrawRay(base.transform.position, normalized * 5f, Color.blue);
	}

	private void OnDrawGizmos()
	{
		if ((bool)rigidbody)
		{
			Gizmos.color = Color.red;
			Gizmos.DrawSphere(base.transform.TransformPoint(rigidbody.centerOfMass), 0.3f);
		}
	}
}
