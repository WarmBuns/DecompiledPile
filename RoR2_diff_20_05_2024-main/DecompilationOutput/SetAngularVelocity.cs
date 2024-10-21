using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SetAngularVelocity : MonoBehaviour
{
	public Vector3 angularVelocity;

	private Rigidbody rigidBody;

	private void Start()
	{
		rigidBody = GetComponent<Rigidbody>();
	}

	private void FixedUpdate()
	{
		rigidBody.maxAngularVelocity = angularVelocity.magnitude;
		rigidBody.angularVelocity = base.transform.TransformVector(angularVelocity);
	}
}
