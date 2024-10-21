using UnityEngine;

public class HoverOverHead : MonoBehaviour
{
	private Transform parentTransform;

	private Collider bodyCollider;

	public Vector3 bonusOffset;

	private void Start()
	{
		Reset();
	}

	public void Reset()
	{
		if ((bool)base.transform)
		{
			parentTransform = base.transform.parent;
			bodyCollider = ((parentTransform == null) ? null : parentTransform.GetComponent<Collider>());
		}
	}

	private void Update()
	{
		if ((bool)parentTransform)
		{
			Vector3 vector = parentTransform.position;
			if ((bool)bodyCollider)
			{
				vector = bodyCollider.bounds.center + new Vector3(0f, bodyCollider.bounds.extents.y, 0f);
			}
			base.transform.position = vector + bonusOffset;
		}
	}
}
