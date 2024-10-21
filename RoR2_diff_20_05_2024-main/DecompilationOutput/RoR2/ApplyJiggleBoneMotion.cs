using UnityEngine;

namespace RoR2;

public class ApplyJiggleBoneMotion : MonoBehaviour
{
	public float forceScale = 100f;

	public Transform rootTransform;

	public Rigidbody[] rigidbodies;

	private Vector3 lastRootPosition;

	private void FixedUpdate()
	{
		Vector3 position = rootTransform.position;
		Rigidbody[] array = rigidbodies;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].AddForce((lastRootPosition - position) * forceScale * Time.fixedDeltaTime);
		}
		lastRootPosition = position;
	}
}
