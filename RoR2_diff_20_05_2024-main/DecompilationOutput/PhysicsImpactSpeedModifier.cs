using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PhysicsImpactSpeedModifier : MonoBehaviour
{
	public float normalSpeedModifier;

	public float perpendicularSpeedModifier;

	private Rigidbody rigid;

	private void Awake()
	{
		rigid = GetComponent<Rigidbody>();
	}

	private void OnCollisionEnter(Collision collision)
	{
		Vector3 normal = collision.contacts[0].normal;
		Vector3 velocity = rigid.velocity;
		Vector3 vector = Vector3.Project(velocity, normal);
		Vector3 vector2 = velocity - vector;
		vector *= normalSpeedModifier;
		vector2 *= perpendicularSpeedModifier;
		rigid.velocity = vector + vector2;
	}
}
