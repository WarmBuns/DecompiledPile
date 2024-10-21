using System;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;

namespace RoR2.Projectile;

public class ProjectileSimple : MonoBehaviour
{
	[Header("Lifetime")]
	public float lifetime = 5f;

	public GameObject lifetimeExpiredEffect;

	[FormerlySerializedAs("velocity")]
	[Header("Velocity")]
	public float desiredForwardSpeed;

	public bool updateAfterFiring;

	public bool enableVelocityOverLifetime;

	public AnimationCurve velocityOverLifetime;

	[Header("Oscillation")]
	public bool oscillate;

	public float oscillateMagnitude = 20f;

	public float oscillateSpeed;

	private float deltaHeight;

	private float oscillationStopwatch;

	private float stopwatch;

	private Rigidbody rigidbody;

	private new Transform transform;

	[Obsolete("Use 'desiredForwardSpeed' instead.", false)]
	public float velocity
	{
		get
		{
			return desiredForwardSpeed;
		}
		set
		{
			desiredForwardSpeed = value;
		}
	}

	protected void Awake()
	{
		transform = base.transform;
		rigidbody = GetComponent<Rigidbody>();
	}

	protected void OnEnable()
	{
		SetForwardSpeed(desiredForwardSpeed);
	}

	protected void Start()
	{
		SetForwardSpeed(desiredForwardSpeed);
	}

	protected void OnDisable()
	{
		SetForwardSpeed(0f);
	}

	protected void FixedUpdate()
	{
		if (oscillate)
		{
			deltaHeight = Mathf.Sin(oscillationStopwatch * oscillateSpeed);
		}
		if (updateAfterFiring || enableVelocityOverLifetime)
		{
			SetForwardSpeed(desiredForwardSpeed);
		}
		oscillationStopwatch += Time.deltaTime;
		stopwatch += Time.deltaTime;
		if (NetworkServer.active && stopwatch > lifetime)
		{
			UnityEngine.Object.Destroy(base.gameObject);
			if ((bool)lifetimeExpiredEffect && NetworkServer.active)
			{
				EffectManager.SimpleEffect(lifetimeExpiredEffect, transform.position, transform.rotation, transmit: true);
			}
		}
	}

	public void SetLifetime(float newLifetime)
	{
		lifetime = newLifetime;
		stopwatch = 0f;
	}

	protected void SetForwardSpeed(float speed)
	{
		if ((bool)rigidbody)
		{
			if (enableVelocityOverLifetime)
			{
				rigidbody.velocity = speed * velocityOverLifetime.Evaluate(stopwatch / lifetime) * transform.forward;
			}
			else
			{
				rigidbody.velocity = transform.forward * speed + transform.right * (deltaHeight * oscillateMagnitude);
			}
		}
	}
}
