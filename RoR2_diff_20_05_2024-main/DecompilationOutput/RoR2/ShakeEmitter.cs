using System.Collections.Generic;
using UnityEngine;

namespace RoR2;

public class ShakeEmitter : MonoBehaviour
{
	public struct MotorBias
	{
		public float deepLeftBias;

		public float quickRightBias;
	}

	private static readonly List<ShakeEmitter> instances = new List<ShakeEmitter>();

	[Tooltip("Whether or not to begin shaking as soon as this instance becomes active.")]
	public bool shakeOnStart = true;

	[Tooltip("Whether or not to begin shaking every time this instance is enabled.")]
	public bool shakeOnEnable;

	[Tooltip("The wave description of this motion.")]
	public Wave wave = new Wave
	{
		amplitude = 1f,
		frequency = 1f,
		cycleOffset = 0f
	};

	[Tooltip("How long the shake lasts, in seconds.")]
	public float duration = 1f;

	[Tooltip("How far the wave reaches.")]
	public float radius = 10f;

	[Tooltip("Whether or not the radius should be multiplied with local scale.")]
	public bool scaleShakeRadiusWithLocalScale;

	[Tooltip("Whether or not the ampitude should decay with time.")]
	public bool amplitudeTimeDecay = true;

	private float stopwatch = float.PositiveInfinity;

	private float halfPeriodTimer;

	private Vector3 halfPeriodVector;

	private Vector3 currentOffset;

	private const float deepRumbleFactor = 5f;

	public void StartShake()
	{
		stopwatch = 0f;
		halfPeriodVector = Random.onUnitSphere;
		halfPeriodTimer = wave.period * 0.5f;
	}

	private void Start()
	{
		if (scaleShakeRadiusWithLocalScale)
		{
			radius *= base.transform.localScale.x;
		}
		if (shakeOnStart)
		{
			StartShake();
		}
	}

	private void OnEnable()
	{
		instances.Add(this);
		if (shakeOnEnable)
		{
			StartShake();
		}
	}

	private void OnDisable()
	{
		instances.Remove(this);
	}

	private void OnValidate()
	{
		if (wave.frequency == 0f)
		{
			wave.frequency = 1f;
		}
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		RoR2Application.onUpdate += UpdateAll;
	}

	public static void UpdateAll()
	{
		float deltaTime = Time.deltaTime;
		if (deltaTime != 0f)
		{
			for (int i = 0; i < instances.Count; i++)
			{
				instances[i].ManualUpdate(deltaTime);
			}
		}
	}

	public float CurrentShakeFade()
	{
		if (!amplitudeTimeDecay)
		{
			return 1f;
		}
		return 1f - stopwatch / duration;
	}

	public void ManualUpdate(float deltaTime)
	{
		stopwatch += deltaTime;
		if (stopwatch < duration)
		{
			float num = CurrentShakeFade();
			halfPeriodTimer -= deltaTime;
			if (halfPeriodTimer < 0f)
			{
				halfPeriodVector = Vector3.Slerp(Random.onUnitSphere, -halfPeriodVector, 0.5f);
				halfPeriodTimer += wave.period * 0.5f;
			}
			currentOffset = halfPeriodVector * wave.Evaluate(halfPeriodTimer) * num;
		}
		else
		{
			currentOffset = Vector3.zero;
		}
	}

	public static void ApplySpacialRumble(LocalUser localUser, Transform cameraTransform)
	{
		Vector3 right = cameraTransform.right;
		Vector3 position = cameraTransform.position;
		float num = 0f;
		float num2 = 0f;
		int i = 0;
		for (int count = instances.Count; i < count; i++)
		{
			ShakeEmitter shakeEmitter = instances[i];
			Vector3 position2 = shakeEmitter.transform.position;
			float value = Vector3.Dot(position2 - position, right);
			float sqrMagnitude = (position - position2).sqrMagnitude;
			float num3 = shakeEmitter.radius;
			float num4 = 0f;
			if (sqrMagnitude < num3 * num3)
			{
				float num5 = 1f - Mathf.Sqrt(sqrMagnitude) / num3;
				num4 = shakeEmitter.CurrentShakeFade() * shakeEmitter.wave.amplitude * num5;
			}
			float num6 = Mathf.Clamp01(Util.Remap(value, -1f, 1f, 0f, 1f));
			float num7 = num4;
			num += num7 * (1f - num6);
			num2 += num7 * num6;
		}
	}

	public static Vector3 ComputeTotalShakeAtPoint(Vector3 position)
	{
		Vector3 zero = Vector3.zero;
		int i = 0;
		for (int count = instances.Count; i < count; i++)
		{
			ShakeEmitter shakeEmitter = instances[i];
			float sqrMagnitude = (position - shakeEmitter.transform.position).sqrMagnitude;
			float num = shakeEmitter.radius;
			if (sqrMagnitude < num * num)
			{
				float num2 = 1f - Mathf.Sqrt(sqrMagnitude) / num;
				zero += shakeEmitter.currentOffset * num2;
			}
		}
		return zero;
	}

	public static ShakeEmitter CreateSimpleShakeEmitter(Vector3 position, Wave wave, float duration, float radius, bool amplitudeTimeDecay)
	{
		if (wave.frequency == 0f)
		{
			wave.frequency = 1f;
		}
		GameObject obj = new GameObject("ShakeEmitter", typeof(ShakeEmitter), typeof(DestroyOnTimer));
		ShakeEmitter component = obj.GetComponent<ShakeEmitter>();
		DestroyOnTimer component2 = obj.GetComponent<DestroyOnTimer>();
		obj.transform.position = position;
		component.wave = wave;
		component.duration = duration;
		component.radius = radius;
		component.amplitudeTimeDecay = amplitudeTimeDecay;
		component2.duration = duration;
		return component;
	}

	private void OnDrawGizmosSelected()
	{
		Matrix4x4 matrix = Gizmos.matrix;
		Color color = Gizmos.color;
		Gizmos.matrix = Matrix4x4.TRS(base.transform.position, base.transform.rotation, Vector3.one);
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(Vector3.zero, radius);
		Gizmos.color = color;
		Gizmos.matrix = matrix;
	}
}
