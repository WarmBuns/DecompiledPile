using UnityEngine;

namespace RoR2;

public class ModelPanelParameters : MonoBehaviour
{
	public Transform focusPointTransform;

	public Transform cameraPositionTransform;

	public Quaternion modelRotation = Quaternion.identity;

	public float minDistance = 1f;

	public float maxDistance = 10f;

	public Vector3 cameraDirection => focusPointTransform.position - cameraPositionTransform.position;

	public void OnDrawGizmos()
	{
		if ((bool)focusPointTransform)
		{
			Gizmos.color = Color.yellow;
			Gizmos.DrawWireSphere(focusPointTransform.position, 0.1f);
			Gizmos.color = Color.gray;
			Gizmos.DrawWireSphere(focusPointTransform.position, minDistance);
			Gizmos.DrawWireSphere(focusPointTransform.position, maxDistance);
		}
		if ((bool)cameraPositionTransform)
		{
			Gizmos.color = Color.yellow;
			Gizmos.DrawWireSphere(cameraPositionTransform.position, 0.2f);
		}
		if ((bool)focusPointTransform && (bool)cameraPositionTransform)
		{
			Gizmos.DrawLine(focusPointTransform.position, cameraPositionTransform.position);
		}
	}
}
