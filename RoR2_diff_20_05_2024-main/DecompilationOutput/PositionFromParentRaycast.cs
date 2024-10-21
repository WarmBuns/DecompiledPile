using UnityEngine;

public class PositionFromParentRaycast : MonoBehaviour
{
	public float maxLength;

	public LayerMask mask;

	private void Update()
	{
		RaycastHit hitInfo = default(RaycastHit);
		if (Physics.Raycast(base.transform.parent.position, base.transform.parent.forward, out hitInfo, maxLength, mask))
		{
			base.transform.position = hitInfo.point;
		}
		else
		{
			base.transform.position = base.transform.parent.position + base.transform.parent.forward * maxLength;
		}
	}
}
