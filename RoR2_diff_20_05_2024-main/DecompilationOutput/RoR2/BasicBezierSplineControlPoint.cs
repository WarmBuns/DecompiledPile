using UnityEngine;

namespace RoR2;

public class BasicBezierSplineControlPoint : MonoBehaviour
{
	public Vector3 forwardVelocity = Vector3.forward;

	public Vector3 backwardVelocity = Vector3.back;

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.yellow;
		Matrix4x4 identity = Matrix4x4.identity;
		identity.SetTRS(base.transform.position, base.transform.rotation, Vector3.one);
		Gizmos.matrix = identity;
		Gizmos.DrawRay(Vector3.zero, forwardVelocity);
		Gizmos.DrawRay(Vector3.zero, backwardVelocity);
		Gizmos.DrawFrustum(Vector3.zero, 60f, -0.2f, 0f, 1f);
	}
}
