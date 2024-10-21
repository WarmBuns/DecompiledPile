using UnityEngine;

namespace RoR2;

public class ExplodeRigidbodiesOnStart : MonoBehaviour
{
	public Rigidbody[] bodies;

	public float force;

	public float explosionRadius;

	private void Start()
	{
		Vector3 position = base.transform.position;
		for (int i = 0; i < bodies.Length; i++)
		{
			bodies[i].AddExplosionForce(force, position, explosionRadius);
		}
	}
}
