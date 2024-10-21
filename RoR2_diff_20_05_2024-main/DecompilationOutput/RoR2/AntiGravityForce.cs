using UnityEngine;

namespace RoR2;

[RequireComponent(typeof(Rigidbody))]
public class AntiGravityForce : MonoBehaviour
{
	public Rigidbody rb;

	[Tooltip("How much to oppose gravity. A value of 1 means it is unaffected by gravity.")]
	public float antiGravityCoefficient;

	private void FixedUpdate()
	{
		rb.AddForce(-Physics.gravity * antiGravityCoefficient, ForceMode.Acceleration);
	}
}
