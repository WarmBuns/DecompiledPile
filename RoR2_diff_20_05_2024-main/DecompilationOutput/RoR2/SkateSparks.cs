using System;
using UnityEngine;

namespace RoR2;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CharacterModel))]
public class SkateSparks : MonoBehaviour
{
	[Serializable]
	private struct FootState
	{
		public float? speed;

		public Vector3? position;

		public float stressAccumulator;

		public float emissionTimer;

		public float debugAcceleration;

		public float debugOverspeed;
	}

	public float maxStress = 4f;

	public float minStressForEmission = 1f;

	public float overspeedStressCoefficient = 1f;

	public float accelerationStressCoefficient = 1f;

	public float perpendicularTravelStressCoefficient = 1f;

	public float maxEmissionRate = 10f;

	public int landingStress = 4;

	public ParticleSystem leftParticleSystem;

	public ParticleSystem rightParticleSystem;

	private Animator animator;

	private Transform leftFoot;

	private Transform rightFoot;

	private CharacterModel characterModel;

	private static readonly int isGroundedParam = Animator.StringToHash("isGrounded");

	private bool previousIsGrounded = true;

	private FootState leftFootState;

	private FootState rightFootState;

	private float debugWalkSpeed;

	private void Awake()
	{
		animator = GetComponent<Animator>();
		characterModel = GetComponent<CharacterModel>();
		leftFoot = (leftParticleSystem ? leftParticleSystem.transform : null);
		rightFoot = (rightParticleSystem ? rightParticleSystem.transform : null);
	}

	private static void UpdateFoot(ref FootState footState, ParticleSystem particleSystem, Transform transform, float walkSpeed, float overspeedStressCoefficient, float accelerationStressCoefficient, float perpendicularTravelStressCoefficient, bool isGrounded, float maxStress, float minStressForEmission, float maxEmissionRate, float deltaTime)
	{
		Vector3? position = (transform ? new Vector3?(transform.position) : null);
		float? speed = null;
		Vector3 vector = Vector3.zero;
		if (position.HasValue && footState.position.HasValue)
		{
			vector = position.Value - footState.position.Value;
			vector.y = 0f;
		}
		float magnitude = vector.magnitude;
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		if (deltaTime != 0f)
		{
			speed = magnitude / deltaTime;
			if (footState.speed.HasValue)
			{
				num = speed.Value - footState.speed.Value;
			}
			num2 = Mathf.Max(speed.Value - walkSpeed, 0f);
			if ((bool)transform && magnitude > 0f)
			{
				Vector3 rhs = vector / magnitude;
				num3 = Mathf.Abs(Vector3.Dot(transform.right, rhs)) * magnitude;
			}
			footState.stressAccumulator += num2 * overspeedStressCoefficient * deltaTime + num * accelerationStressCoefficient + num3 * perpendicularTravelStressCoefficient;
			footState.debugAcceleration = num;
			footState.debugOverspeed = num2;
		}
		if (!isGrounded)
		{
			footState.stressAccumulator = 0f;
		}
		footState.stressAccumulator = Mathf.Clamp(footState.stressAccumulator, 0f, maxStress);
		footState.emissionTimer -= deltaTime * maxEmissionRate;
		if (footState.emissionTimer <= 0f)
		{
			footState.emissionTimer = 1f;
			footState.stressAccumulator -= 1f;
			if (footState.stressAccumulator >= minStressForEmission && (bool)particleSystem)
			{
				particleSystem.Emit(1);
			}
		}
		footState.position = position;
		footState.speed = speed;
	}

	private void Update()
	{
		bool flag = false;
		float num = 0f;
		CharacterBody body = characterModel.body;
		if ((bool)body)
		{
			num = body.moveSpeed;
			if (body.isSprinting)
			{
				num /= body.sprintingSpeedMultiplier;
			}
			CharacterMotor characterMotor = body.characterMotor;
			if ((bool)characterMotor)
			{
				flag = characterMotor.isGrounded;
			}
		}
		if (flag != previousIsGrounded)
		{
			float num2 = landingStress;
			leftFootState.stressAccumulator += num2;
			rightFootState.stressAccumulator += num2;
		}
		float deltaTime = Time.deltaTime;
		UpdateFoot(ref leftFootState, leftParticleSystem, leftFoot, num, overspeedStressCoefficient, accelerationStressCoefficient, perpendicularTravelStressCoefficient, flag, maxStress, minStressForEmission, maxEmissionRate, deltaTime);
		UpdateFoot(ref rightFootState, rightParticleSystem, rightFoot, num, overspeedStressCoefficient, accelerationStressCoefficient, perpendicularTravelStressCoefficient, flag, maxStress, minStressForEmission, maxEmissionRate, deltaTime);
		debugWalkSpeed = num;
		previousIsGrounded = flag;
	}
}
