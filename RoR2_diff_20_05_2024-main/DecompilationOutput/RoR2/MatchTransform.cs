using UnityEngine;

namespace RoR2;

[ExecuteAlways]
public class MatchTransform : MonoBehaviour
{
	public Transform targetTransform;

	public void LateUpdate()
	{
		UpdateNow();
	}

	[ContextMenu("Update Now")]
	public void UpdateNow()
	{
		if ((bool)targetTransform)
		{
			base.transform.SetPositionAndRotation(targetTransform.position, targetTransform.rotation);
		}
	}
}
