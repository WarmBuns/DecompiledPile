using UnityEngine;

namespace RoR2;

[RequireComponent(typeof(SphereCollider))]
public class DisableCollisionsIfInTrigger : MonoBehaviour
{
	public Collider colliderToIgnore;

	private SphereCollider trigger;

	public void Awake()
	{
		trigger = GetComponent<SphereCollider>();
	}

	private void OnTriggerEnter(Collider other)
	{
		if ((bool)trigger)
		{
			Vector3 position = base.transform.position;
			Vector3 position2 = other.transform.position;
			float num = trigger.radius * Mathf.Max(base.transform.lossyScale.x, base.transform.lossyScale.y, base.transform.lossyScale.z);
			float num2 = num * num;
			if ((position - position2).sqrMagnitude < num2)
			{
				Physics.IgnoreCollision(colliderToIgnore, other);
			}
		}
	}
}
