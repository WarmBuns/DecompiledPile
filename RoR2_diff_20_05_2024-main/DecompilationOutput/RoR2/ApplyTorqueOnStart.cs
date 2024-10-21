using UnityEngine;

namespace RoR2;

public class ApplyTorqueOnStart : MonoBehaviour
{
	public Vector3 localTorque;

	public bool randomize;

	private void Start()
	{
		Rigidbody component = GetComponent<Rigidbody>();
		if ((bool)component)
		{
			Vector3 torque = localTorque;
			if (randomize)
			{
				torque.x = Random.Range((0f - torque.x) / 2f, torque.x / 2f);
				torque.y = Random.Range((0f - torque.y) / 2f, torque.y / 2f);
				torque.z = Random.Range((0f - torque.z) / 2f, torque.z / 2f);
			}
			component.AddRelativeTorque(torque);
		}
	}
}
