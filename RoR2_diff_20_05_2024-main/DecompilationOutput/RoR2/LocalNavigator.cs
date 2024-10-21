using RoR2.ConVar;
using UnityEngine;

namespace RoR2;

public class LocalNavigator
{
	private readonly struct BodyComponents
	{
		public readonly CharacterBody body;

		public readonly Transform transform;

		public readonly CharacterMotor motor;

		public readonly Collider bodyCollider;

		public BodyComponents(CharacterBody body)
		{
			this = default(BodyComponents);
			this.body = body;
			if ((bool)body)
			{
				transform = body.transform;
				motor = body.characterMotor;
				if ((bool)motor)
				{
					bodyCollider = body.characterMotor.Motor.Capsule;
				}
			}
		}
	}

	private readonly struct BodySnapshot
	{
		public readonly Vector3 position;

		public readonly Vector3 chestPosition;

		public readonly Vector3 footPosition;

		public readonly Vector3 motorMoveDirection;

		public readonly Vector3 motorVelocity;

		public readonly float maxMoveSpeed;

		public readonly float acceleration;

		public readonly float maxJumpHeight;

		public readonly float maxJumpSpeed;

		public readonly bool isGrounded;

		public readonly float time;

		public readonly float bodyRadius;

		public readonly bool isFlying;

		public readonly bool isJumping;

		private readonly bool hasBody;

		private readonly bool hasBodyCollider;

		private readonly bool hasMotor;

		public bool isValid => hasBody;

		public bool canJump
		{
			get
			{
				if (isGrounded)
				{
					return maxJumpSpeed > 0f;
				}
				return false;
			}
		}

		public BodySnapshot(in BodyComponents bodyComponents, float time)
		{
			this = default(BodySnapshot);
			this.time = time;
			hasBody = bodyComponents.body;
			hasBodyCollider = bodyComponents.bodyCollider;
			hasMotor = bodyComponents.motor;
			if ((bool)bodyComponents.body)
			{
				position = bodyComponents.transform.position;
				chestPosition = position;
				footPosition = position;
				maxMoveSpeed = bodyComponents.body.moveSpeed;
				acceleration = bodyComponents.body.acceleration;
				maxJumpHeight = bodyComponents.body.maxJumpHeight;
				maxJumpSpeed = bodyComponents.body.jumpPower;
				bodyRadius = bodyComponents.body.radius;
				isFlying = bodyComponents.body.isFlying;
			}
			if ((bool)bodyComponents.bodyCollider)
			{
				Bounds bounds = bodyComponents.bodyCollider.bounds;
				bounds.center = Vector3.zero;
				chestPosition.y += bounds.max.y * 0.5f;
				footPosition.y += bounds.min.y;
			}
			if ((bool)bodyComponents.motor)
			{
				isGrounded = bodyComponents.motor.isGrounded;
				motorVelocity = bodyComponents.motor.velocity;
				isJumping = !isGrounded;
			}
		}
	}

	private readonly struct SnapshotDelta
	{
		public readonly float deltaTime;

		public readonly Vector3 estimatedVelocity;

		public readonly bool isValid;

		public SnapshotDelta(in BodySnapshot oldSnapshot, in BodySnapshot newSnapshot)
		{
			this = default(SnapshotDelta);
			if (oldSnapshot.isValid && newSnapshot.isValid)
			{
				deltaTime = newSnapshot.time - oldSnapshot.time;
				isValid = deltaTime > 0f;
				if (isValid)
				{
					estimatedVelocity = (newSnapshot.position - oldSnapshot.position) / deltaTime;
				}
			}
		}
	}

	private struct RaycastResults
	{
		public bool forwardObstructed;

		public bool leftObstructed;

		public bool rightObstructed;
	}

	private Vector3 previousMoveVector;

	public Vector3 targetPosition;

	public float avoidanceDuration = 0.5f;

	private float avoidanceTimer;

	public bool allowWalkOffCliff;

	public bool wasObstructedLastUpdate;

	private float localTime;

	private float raycastTimer;

	private static readonly float raycastUpdateInterval = 0.2f;

	private static readonly float avoidanceAngle = 45f;

	private const bool enableFrustration = true;

	private const bool enableWhiskers = true;

	private const bool enableCliffAvoidance = true;

	private BodyComponents bodyComponents;

	private float walkFrustration;

	private const float frustrationLimit = 1f;

	private const float frustrationDecayRate = 0.25f;

	private const float frustrationMinimumSpeed = 0.1f;

	private float aiStopwatch;

	private Vector3 velocityDelta;

	private Vector3 estimatedAcceleration;

	private float accelerationAccuracy;

	private float moveDirectionAccuracy;

	private bool hasMadeSharpTurn;

	private float speedAsFractionOfTopSpeed;

	private bool isAlreadyMovingAtSufficientSpeed;

	private BodySnapshot currentSnapshot;

	private BodySnapshot previousSnapshot;

	private SnapshotDelta currentSnapshotDelta;

	private SnapshotDelta previousSnapshotDelta;

	private RaycastResults raycastResults;

	private static readonly BoolConVar cvLocalNavigatorDebugDraw = new BoolConVar("local_navigator_debug_draw", ConVarFlags.None, "0", "Enables debug drawing of LocalNavigator (drawing visible in editor only).\n  Orange Line: Current position to target position\n  Yellow Line: Raycasts\n  Red Point: Raycast hit position\n  Green Line: Final chosen move vector");

	public Vector3 moveVector { get; private set; }

	public float jumpSpeed { get; private set; }

	public void SetBody(CharacterBody newBody)
	{
		bodyComponents = new BodyComponents(newBody);
		PushSnapshot();
		PushSnapshot();
	}

	private void PushSnapshot()
	{
		previousSnapshot = currentSnapshot;
		currentSnapshot = new BodySnapshot(in bodyComponents, localTime);
		previousSnapshotDelta = currentSnapshotDelta;
		currentSnapshotDelta = new SnapshotDelta(in previousSnapshot, in currentSnapshot);
	}

	public void Update(float deltaTime)
	{
		localTime += deltaTime;
		PushSnapshot();
		if (cvLocalNavigatorDebugDraw.value)
		{
			Debug.DrawLine(currentSnapshot.position, targetPosition, new Color32(byte.MaxValue, 127, 39, 127), deltaTime);
		}
		wasObstructedLastUpdate = false;
		jumpSpeed = 0f;
		previousMoveVector = moveVector;
		Vector3 vector = targetPosition - currentSnapshot.position;
		if (!currentSnapshot.isFlying)
		{
			vector.y = 0f;
		}
		float magnitude = vector.magnitude;
		Vector3 positionToTargetNormalized = vector / magnitude;
		Vector3 vector2 = moveVector;
		CompensateForVelocityByRefinement(deltaTime, ref vector2, in positionToTargetNormalized);
		moveVector = vector2;
		if (magnitude == 0f)
		{
			moveVector = Vector3.zero;
		}
		else
		{
			raycastResults = GetRaycasts(in currentSnapshot, moveVector, deltaTime);
			if (raycastResults.forwardObstructed)
			{
				int num = ((!raycastResults.leftObstructed) ? (-1) : 0) + ((!raycastResults.rightObstructed) ? 1 : 0);
				if (num == 0)
				{
					num = ((Random.Range(0, 1) != 1) ? 1 : (-1));
				}
				moveVector = Quaternion.Euler(0f, (0f - avoidanceAngle) * (float)num, 0f) * moveVector;
				wasObstructedLastUpdate = raycastResults.leftObstructed || raycastResults.rightObstructed;
			}
			float deltaTime2 = deltaTime + 0.4f;
			if (CheckCliffAhead(in currentSnapshot, in currentSnapshotDelta, moveVector, deltaTime2))
			{
				wasObstructedLastUpdate = true;
				moveVector = -moveVector;
				if (CheckCliffAhead(in currentSnapshot, in currentSnapshotDelta, moveVector, deltaTime2))
				{
					moveVector *= 0.25f;
				}
			}
			CalculateFrustration(deltaTime, ref walkFrustration);
			if (walkFrustration >= 1f)
			{
				jumpSpeed = currentSnapshot.maxJumpSpeed;
			}
		}
		avoidanceTimer -= deltaTime;
		walkFrustration = Mathf.Clamp(walkFrustration - deltaTime * 0.25f, 0f, 1f);
		if (cvLocalNavigatorDebugDraw.value)
		{
			Debug.DrawRay(currentSnapshot.position, moveVector * 5f, Color.green, deltaTime, depthTest: false);
		}
	}

	private void CompensateForVelocityByRefinement(float nextExpectedDeltaTime, ref Vector3 moveVector, in Vector3 positionToTargetNormalized)
	{
		Vector3 estimatedVelocity = currentSnapshotDelta.estimatedVelocity;
		_ = currentSnapshot.acceleration;
		float num = Vector3.Dot(estimatedVelocity, positionToTargetNormalized);
		Vector3 vector = positionToTargetNormalized;
		Vector3 vector2 = vector;
		int num2 = 8;
		if (!currentSnapshot.isJumping)
		{
			for (int i = 0; i < num2; i++)
			{
				EstimateNextMovement(in currentSnapshot, in currentSnapshotDelta, in vector2, nextExpectedDeltaTime, out var nextPosition, out var nextVelocity);
				Vector3 vector3 = targetPosition - nextPosition;
				if (!currentSnapshot.isFlying)
				{
					vector3.y = 0f;
				}
				Vector3 normalized = vector3.normalized;
				vector2 = normalized;
				if (Vector3.Dot(nextVelocity, normalized) > num)
				{
					vector = normalized;
				}
			}
		}
		moveVector = vector;
	}

	private void CompensateForVelocityByBruteForce(float nextExpectedDeltaTime, ref Vector3 moveVector, in Vector3 positionToTargetNormalized)
	{
		Vector3 estimatedVelocity = currentSnapshotDelta.estimatedVelocity;
		_ = currentSnapshot;
		float num = Vector3.Dot(estimatedVelocity.normalized, positionToTargetNormalized);
		Vector3 vector = moveVector;
		int num2 = 16;
		float num3 = 360f / (float)num2;
		for (int i = 0; i < num2; i++)
		{
			Vector3 vector2 = Quaternion.AngleAxis((float)i * num3, Vector3.up) * Vector3.forward;
			EstimateNextMovement(in currentSnapshot, in currentSnapshotDelta, in vector2, nextExpectedDeltaTime, out var nextPosition, out var nextVelocity);
			float num4 = Vector3.Dot((targetPosition - nextPosition).normalized, nextVelocity.normalized);
			if (num4 > num)
			{
				num = num4;
				vector = vector2;
			}
		}
		moveVector = vector;
	}

	private static void EstimateNextMovement(in BodySnapshot currentSnapshot, in SnapshotDelta currentSnapshotDelta, in Vector3 moveVector, float nextDeltaTime, out Vector3 nextPosition, out Vector3 nextVelocity)
	{
		Vector3 estimatedVelocity = currentSnapshotDelta.estimatedVelocity;
		float acceleration = currentSnapshot.acceleration;
		nextVelocity = Vector3.MoveTowards(estimatedVelocity, moveVector * currentSnapshot.maxMoveSpeed, acceleration * nextDeltaTime);
		float num = Mathf.Min((nextVelocity - estimatedVelocity).magnitude / acceleration, nextDeltaTime);
		float num2 = nextDeltaTime - num;
		Vector3 vector = (nextVelocity - estimatedVelocity) / num;
		Vector3 vector2 = nextVelocity * num + vector * (0.5f * num * num);
		Vector3 vector3 = nextVelocity * num2;
		nextPosition = currentSnapshot.position + vector2 + vector3;
	}

	private RaycastResults GetRaycasts(in BodySnapshot currentSnapshot, Vector3 positionToTargetNormalized, float lookaheadTime)
	{
		RaycastResults result = default(RaycastResults);
		Vector3 origin = currentSnapshot.chestPosition;
		LayerMask layerMask = (int)LayerIndex.world.mask | (int)LayerIndex.CommonMasks.characterBodiesOrDefault;
		float maxDistance = currentSnapshot.bodyRadius + currentSnapshot.maxMoveSpeed * lookaheadTime;
		result.forwardObstructed = Raycast(in origin, in positionToTargetNormalized, out var hitInfo, maxDistance, layerMask);
		if (result.forwardObstructed)
		{
			result.leftObstructed = Raycast(in origin, Quaternion.Euler(0f, avoidanceAngle, 0f) * positionToTargetNormalized, out hitInfo, maxDistance, layerMask);
			result.rightObstructed = Raycast(in origin, Quaternion.Euler(0f, 0f - avoidanceAngle, 0f) * positionToTargetNormalized, out hitInfo, maxDistance, layerMask);
		}
		return result;
	}

	private bool Raycast(in Vector3 origin, in Vector3 direction, out RaycastHit hitInfo, float maxDistance, LayerMask layerMask)
	{
		bool flag = Physics.Raycast(origin, direction, out hitInfo, maxDistance, layerMask);
		if (cvLocalNavigatorDebugDraw.value)
		{
			Debug.DrawRay(origin, direction.normalized * maxDistance, Color.yellow, raycastUpdateInterval);
			if (flag)
			{
				Util.DebugCross(hitInfo.point, 1f, Color.red, raycastUpdateInterval);
			}
		}
		return flag;
	}

	private bool Linecast(in Vector3 start, in Vector3 end, out RaycastHit hitInfo, LayerMask layerMask)
	{
		bool flag = Physics.Linecast(start, end, out hitInfo, layerMask);
		if (cvLocalNavigatorDebugDraw.value)
		{
			Debug.DrawLine(start, end, Color.yellow, raycastUpdateInterval);
			if (flag)
			{
				Util.DebugCross(hitInfo.point, 1f, Color.red, raycastUpdateInterval);
			}
		}
		return flag;
	}

	private bool CheckCliffAhead(in BodySnapshot currentSnapshot, in SnapshotDelta currentSnapshotDelta, in Vector3 currentMoveVector, float deltaTime)
	{
		if (!allowWalkOffCliff && currentSnapshot.isGrounded)
		{
			EstimateNextMovement(in currentSnapshot, in currentSnapshotDelta, in currentMoveVector, deltaTime, out var nextPosition, out var _);
			float num = currentSnapshot.chestPosition.y - currentSnapshot.position.y;
			float num2 = currentSnapshot.footPosition.y - currentSnapshot.position.y;
			Vector3 start = nextPosition;
			start.y += num;
			Vector3 end = start;
			end.y += num2;
			end.y -= 4f;
			RaycastHit hitInfo;
			return !Linecast(in start, in end, out hitInfo, LayerIndex.world.mask);
		}
		return false;
	}

	private void CalculateFrustration(float deltaTime, ref float frustration)
	{
		if (!currentSnapshot.canJump)
		{
			frustration = 0f;
		}
		else
		{
			if (!currentSnapshot.isValid || !currentSnapshotDelta.isValid || !currentSnapshotDelta.isValid || !previousSnapshotDelta.isValid)
			{
				return;
			}
			if (true)
			{
				Vector3 rhs = FlattenDirection(moveVector);
				Vector3 vector2 = FlattenDirection(currentSnapshotDelta.estimatedVelocity);
				Vector3 vector3 = FlattenDirection(previousSnapshotDelta.estimatedVelocity);
				float num = Vector3.Dot(vector2, rhs);
				velocityDelta = vector2 - vector3;
				estimatedAcceleration = velocityDelta / deltaTime;
				float num2 = Vector3.Dot(estimatedAcceleration, rhs);
				float num3 = estimatedAcceleration.magnitude - num2;
				speedAsFractionOfTopSpeed = num / currentSnapshot.maxMoveSpeed;
				isAlreadyMovingAtSufficientSpeed = speedAsFractionOfTopSpeed >= 0.45f;
				if (isAlreadyMovingAtSufficientSpeed)
				{
					frustration = 0f;
				}
				else if (num3 > num2 * 2f)
				{
					frustration += 1.25f * deltaTime;
				}
				return;
			}
			float num4 = currentSnapshot.motorVelocity.magnitude + bodyComponents.motor.rootMotion.magnitude / deltaTime;
			float magnitude = (targetPosition - currentSnapshot.position).magnitude;
			if (currentSnapshot.maxMoveSpeed != 0f && num4 != 0f && magnitude > Mathf.Epsilon)
			{
				float magnitude2 = currentSnapshotDelta.estimatedVelocity.magnitude;
				if (magnitude2 <= num4 || magnitude2 <= 0.1f)
				{
					frustration += 1.25f * deltaTime;
				}
			}
		}
		static Vector3 FlattenDirection(Vector3 vector)
		{
			if (Mathf.Abs(vector.y) > 0f)
			{
				vector.y = 0f;
			}
			return vector;
		}
	}
}
