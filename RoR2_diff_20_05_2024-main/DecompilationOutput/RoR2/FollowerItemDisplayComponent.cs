using UnityEngine;

namespace RoR2;

public class FollowerItemDisplayComponent : MonoBehaviour
{
	public Transform target;

	public Vector3 localPosition;

	public Quaternion localRotation;

	public Vector3 localScale;

	private new Transform transform;

	private void Awake()
	{
		transform = base.transform;
	}

	private void LateUpdate()
	{
		if (!target)
		{
			Object.Destroy(base.gameObject);
			return;
		}
		Quaternion rotation = target.rotation;
		transform.position = target.position + rotation * localPosition;
		transform.rotation = rotation * localRotation;
		transform.localScale = localScale;
	}
}
