using UnityEngine;

namespace RoR2;

public class PreGameShakeController : MonoBehaviour
{
	public ShakeEmitter shakeEmitter;

	public float minInterval = 0.5f;

	public float maxInterval = 7f;

	public Rigidbody[] physicsBodies;

	public float physicsForce;

	private float timer;

	private void ResetTimer()
	{
		timer = Random.Range(minInterval, maxInterval);
	}

	private void DoShake()
	{
		shakeEmitter.StartShake();
		Vector3 onUnitSphere = Random.onUnitSphere;
		Rigidbody[] array = physicsBodies;
		foreach (Rigidbody rigidbody in array)
		{
			if ((bool)rigidbody)
			{
				Vector3 force = onUnitSphere * ((0.75f + Random.value * 0.25f) * physicsForce);
				float y = rigidbody.GetComponent<Collider>().bounds.min.y;
				Vector3 centerOfMass = rigidbody.centerOfMass;
				centerOfMass.y = y;
				rigidbody.AddForceAtPosition(force, centerOfMass);
			}
		}
	}

	private void Awake()
	{
		ResetTimer();
	}

	private void Update()
	{
		timer -= Time.deltaTime;
		if (timer <= 0f)
		{
			ResetTimer();
			DoShake();
		}
	}
}
