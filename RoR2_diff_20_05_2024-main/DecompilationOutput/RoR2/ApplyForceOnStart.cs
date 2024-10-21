using UnityEngine;

namespace RoR2;

public class ApplyForceOnStart : MonoBehaviour
{
	public Vector3 localForce;

	private void Start()
	{
		Rigidbody component = GetComponent<Rigidbody>();
		if ((bool)component)
		{
			component.AddRelativeForce(localForce);
		}
	}
}
