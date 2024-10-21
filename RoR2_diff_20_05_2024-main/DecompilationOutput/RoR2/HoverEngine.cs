using UnityEngine;

namespace RoR2;

public class HoverEngine : MonoBehaviour
{
	public Rigidbody engineRigidbody;

	public Transform wheelVisual;

	public float hoverForce = 65f;

	public float hoverHeight = 3.5f;

	public float hoverDamping = 0.1f;

	public float hoverRadius;

	[HideInInspector]
	public float forceStrength;

	private Ray castRay;

	private Vector3 castPosition;

	[HideInInspector]
	public RaycastHit raycastHit;

	public float compression;

	public Vector3 offsetVector = Vector3.zero;

	public bool isGrounded;

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(base.transform.TransformPoint(offsetVector), hoverRadius);
		Gizmos.DrawRay(castRay);
		if (isGrounded)
		{
			Gizmos.DrawSphere(raycastHit.point, hoverRadius);
		}
	}

	private void FixedUpdate()
	{
		float num = Mathf.Clamp01(Vector3.Dot(-base.transform.up, Vector3.down));
		castPosition = base.transform.TransformPoint(offsetVector);
		castRay = new Ray(castPosition, -base.transform.up);
		isGrounded = false;
		forceStrength = 0f;
		compression = 0f;
		Vector3 position = castRay.origin + castRay.direction * hoverHeight;
		if (Physics.SphereCast(castRay, hoverRadius, out raycastHit, hoverHeight, LayerIndex.world.mask))
		{
			isGrounded = true;
			float num2 = (hoverHeight - raycastHit.distance) / hoverHeight;
			Vector3 vector = Vector3.up * (num2 * hoverForce);
			Vector3 vector2 = Vector3.Project(engineRigidbody.GetPointVelocity(castPosition), -base.transform.up) * hoverDamping;
			forceStrength = (vector - vector2).magnitude;
			engineRigidbody.AddForceAtPosition(Vector3.Project(vector - vector2, -base.transform.up), castPosition, ForceMode.Acceleration);
			compression = Mathf.Clamp01(num2 * num);
			position = raycastHit.point;
		}
		wheelVisual.position = position;
		_ = isGrounded;
	}
}
