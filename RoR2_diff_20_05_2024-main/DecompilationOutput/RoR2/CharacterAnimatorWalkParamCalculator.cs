using System;
using UnityEngine;

namespace RoR2;

public struct CharacterAnimatorWalkParamCalculator
{
	private float animatorReferenceMagnitudeVelocity;

	private float animatorReferenceAngleVelocity;

	public Vector2 animatorWalkSpeed { get; private set; }

	public float remainingTurnAngle { get; private set; }

	public void Update(Vector3 worldMoveVector, Vector3 animatorForward, in BodyAnimatorSmoothingParameters.SmoothingParameters smoothingParameters, float deltaTime)
	{
		Vector3 rhs = Vector3.Cross(Vector3.up, animatorForward);
		float x = Vector3.Dot(worldMoveVector, animatorForward);
		float y = Vector3.Dot(worldMoveVector, rhs);
		Vector2 to = new Vector2(x, y);
		float magnitude = to.magnitude;
		float num = ((magnitude > 0f) ? Vector2.SignedAngle(Vector2.right, to) : 0f);
		float magnitude2 = animatorWalkSpeed.magnitude;
		float current = ((magnitude2 > 0f) ? Vector2.SignedAngle(Vector2.right, animatorWalkSpeed) : 0f);
		float num2 = Mathf.SmoothDamp(magnitude2, magnitude, ref animatorReferenceMagnitudeVelocity, smoothingParameters.walkMagnitudeSmoothDamp, float.PositiveInfinity, deltaTime);
		float num3 = Mathf.SmoothDampAngle(current, num, ref animatorReferenceAngleVelocity, smoothingParameters.walkAngleSmoothDamp, float.PositiveInfinity, deltaTime);
		remainingTurnAngle = num3 - num;
		animatorWalkSpeed = new Vector2(Mathf.Cos(num3 * (MathF.PI / 180f)), Mathf.Sin(num3 * (MathF.PI / 180f))) * num2;
	}
}
