using UnityEngine;

namespace RoR2;

[RequireComponent(typeof(Collider))]
public class VehicleForceZone : MonoBehaviour
{
	public Rigidbody vehicleRigidbody;

	public float impactMultiplier;

	private Collider collider;

	private void Start()
	{
		collider = GetComponent<Collider>();
	}

	public void OnTriggerEnter(Collider other)
	{
		CharacterMotor component = other.GetComponent<CharacterMotor>();
		HealthComponent component2 = other.GetComponent<HealthComponent>();
		if ((bool)component && (bool)component2)
		{
			Vector3 position = base.transform.position;
			_ = vehicleRigidbody.velocity.normalized;
			Vector3 pointVelocity = vehicleRigidbody.GetPointVelocity(position);
			Vector3 vector = pointVelocity * vehicleRigidbody.mass * impactMultiplier;
			_ = vehicleRigidbody.mass;
			Mathf.Pow(pointVelocity.magnitude, 2f);
			float num = component.mass / (component.mass + vehicleRigidbody.mass);
			vehicleRigidbody.AddForceAtPosition(-vector * num, position);
			Debug.LogFormat("Impulse: {0}, Ratio: {1}", vector.magnitude, num);
			DamageInfo damageInfo = new DamageInfo();
			damageInfo.attacker = base.gameObject;
			damageInfo.force = vector;
			damageInfo.position = position;
			component2.TakeDamageForce(damageInfo, alwaysApply: true);
		}
	}

	public void OnCollisionEnter(Collision collision)
	{
		Debug.LogFormat("Hit {0}", collision.gameObject);
		Rigidbody component = collision.collider.GetComponent<Rigidbody>();
		if ((bool)component)
		{
			HealthComponent component2 = component.GetComponent<HealthComponent>();
			if ((bool)component2)
			{
				Vector3 point = collision.contacts[0].point;
				_ = collision.contacts[0].normal;
				vehicleRigidbody.GetPointVelocity(point);
				Vector3 impulse = collision.impulse;
				float num = 0f;
				vehicleRigidbody.AddForceAtPosition(impulse * num, point);
				Debug.LogFormat("Impulse: {0}, Ratio: {1}", impulse, num);
				DamageInfo damageInfo = new DamageInfo();
				damageInfo.attacker = base.gameObject;
				damageInfo.force = -impulse * (1f - num);
				damageInfo.position = point;
				component2.TakeDamageForce(damageInfo, alwaysApply: true);
			}
		}
	}

	private void Update()
	{
	}
}
