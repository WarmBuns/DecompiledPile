using System;
using HG;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(WormBodyPositions2))]
public class WormBodyPositionsDriver : MonoBehaviour
{
	public Transform referenceTransform;

	public Transform chasePositionVisualizer;

	public float maxTurnSpeed = 180f;

	public float verticalTurnSquashFactor = 2f;

	public float ySpringConstant = 100f;

	public float yDamperConstant = 1f;

	public bool allowShoving;

	public float yShoveVelocityThreshold;

	public float yShovePositionThreshold;

	public float yShoveForce;

	public float turnRateCoefficientAboveGround;

	public float wormForceCoefficientAboveGround;

	public float keyFrameGenerationInterval = 0.25f;

	public float maxBreachSpeed = 40f;

	private WormBodyPositions2 wormBodyPositions;

	private CharacterDirection characterDirection;

	private Vector3 chaserPreviousVelocity;

	private bool chaserIsUnderground;

	private float keyFrameGenerationTimer;

	public Vector3 chaserVelocity { get; set; }

	public Vector3 chaserPosition { get; private set; }

	private void Awake()
	{
		wormBodyPositions = GetComponent<WormBodyPositions2>();
		characterDirection = GetComponent<CharacterDirection>();
	}

	private void OnEnable()
	{
		wormBodyPositions.onPredictedBreachDiscovered += OnPredictedBreachDiscovered;
	}

	private void OnDisable()
	{
		wormBodyPositions.onPredictedBreachDiscovered -= OnPredictedBreachDiscovered;
	}

	private void Start()
	{
		if (NetworkServer.active)
		{
			chaserPosition = base.transform.position;
			chaserVelocity = characterDirection.forward;
		}
	}

	private void FixedUpdate()
	{
		if (NetworkServer.active)
		{
			FixedUpdateServer();
		}
	}

	public void OnTeleport(Vector3 oldPosition, Vector3 newPosition)
	{
		Vector3 vector = newPosition - oldPosition;
		chaserPosition += vector;
	}

	private void OnPredictedBreachDiscovered(float expectedTime, Vector3 hitPosition, Vector3 hitNormal)
	{
		float magnitude = chaserVelocity.magnitude;
		if (magnitude > maxBreachSpeed)
		{
			chaserVelocity /= magnitude / maxBreachSpeed;
		}
	}

	private void FixedUpdateServer()
	{
		Vector3 position = referenceTransform.position;
		float speedMultiplier = wormBodyPositions.speedMultiplier;
		Vector3 normalized = (position - chaserPosition).normalized;
		float num = (chaserIsUnderground ? maxTurnSpeed : (maxTurnSpeed * turnRateCoefficientAboveGround)) * (MathF.PI / 180f);
		Vector3 current = new Vector3(chaserVelocity.x, 0f, chaserVelocity.z);
		Vector3 vector = new Vector3(normalized.x, 0f, normalized.z);
		current = Vector3.RotateTowards(current, vector * speedMultiplier, num * Time.fixedDeltaTime, float.PositiveInfinity).normalized * speedMultiplier;
		float num2 = position.y - chaserPosition.y;
		float num3 = (0f - chaserVelocity.y) * yDamperConstant;
		float num4 = num2 * ySpringConstant;
		if (allowShoving && Mathf.Abs(chaserVelocity.y) < yShoveVelocityThreshold && num2 > yShovePositionThreshold)
		{
			chaserVelocity = chaserVelocity.XAZ(chaserVelocity.y + yShoveForce * Time.fixedDeltaTime);
		}
		if (!chaserIsUnderground)
		{
			num4 *= wormForceCoefficientAboveGround;
			num3 *= wormForceCoefficientAboveGround;
		}
		chaserVelocity = chaserVelocity.XAZ(chaserVelocity.y + (num4 + num3) * Time.fixedDeltaTime);
		chaserVelocity += Physics.gravity * Time.fixedDeltaTime;
		chaserVelocity = new Vector3(current.x, chaserVelocity.y, current.z);
		chaserPosition += chaserVelocity * Time.fixedDeltaTime;
		chasePositionVisualizer.position = chaserPosition;
		chaserIsUnderground = 0f - num2 < wormBodyPositions.undergroundTestYOffset;
		keyFrameGenerationTimer -= Time.deltaTime;
		if (keyFrameGenerationTimer <= 0f)
		{
			keyFrameGenerationTimer = keyFrameGenerationInterval;
			wormBodyPositions.AttemptToGenerateKeyFrame(wormBodyPositions.GetSynchronizedTimeStamp() + wormBodyPositions.followDelay, chaserPosition, chaserVelocity);
		}
	}
}
