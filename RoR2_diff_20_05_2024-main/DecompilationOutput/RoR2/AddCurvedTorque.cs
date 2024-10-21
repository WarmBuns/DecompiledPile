using UnityEngine;

namespace RoR2;

public class AddCurvedTorque : MonoBehaviour
{
	public AnimationCurve torqueCurve;

	public Vector3 localTorqueVector;

	public float lifetime;

	public Rigidbody[] rigidbodies;

	private float stopwatch;

	private void FixedUpdate()
	{
		stopwatch += Time.fixedDeltaTime;
		float num = torqueCurve.Evaluate(stopwatch / lifetime);
		Rigidbody[] array = rigidbodies;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].AddRelativeTorque(localTorqueVector * num);
		}
	}
}
