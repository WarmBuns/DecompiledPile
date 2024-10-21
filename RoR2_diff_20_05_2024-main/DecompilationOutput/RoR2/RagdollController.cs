using UnityEngine;

namespace RoR2;

public class RagdollController : MonoBehaviour
{
	public Transform[] bones;

	public MonoBehaviour[] componentsToDisableOnRagdoll;

	private Animator animator;

	private void Start()
	{
		animator = GetComponent<Animator>();
		Transform[] array = bones;
		foreach (Transform transform in array)
		{
			Collider component = transform.GetComponent<Collider>();
			Rigidbody component2 = transform.GetComponent<Rigidbody>();
			if (!component)
			{
				Debug.LogWarningFormat("Bone {0} is missing a collider!", transform);
			}
			else
			{
				component.enabled = false;
				component2.interpolation = RigidbodyInterpolation.None;
				component2.isKinematic = true;
			}
		}
	}

	public void BeginRagdoll(Vector3 force)
	{
		if ((bool)animator)
		{
			animator.enabled = false;
		}
		Transform[] array = bones;
		foreach (Transform transform in array)
		{
			if (transform.gameObject.layer == LayerIndex.ragdoll.intVal)
			{
				transform.parent = base.transform;
				Rigidbody component = transform.GetComponent<Rigidbody>();
				transform.GetComponent<Collider>().enabled = true;
				component.isKinematic = false;
				component.interpolation = RigidbodyInterpolation.Interpolate;
				component.collisionDetectionMode = CollisionDetectionMode.Continuous;
				component.AddForce(force * Random.Range(0.9f, 1.2f), ForceMode.VelocityChange);
			}
		}
		MonoBehaviour[] array2 = componentsToDisableOnRagdoll;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].enabled = false;
		}
	}
}
