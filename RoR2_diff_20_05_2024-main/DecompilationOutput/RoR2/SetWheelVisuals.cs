using UnityEngine;

namespace RoR2;

[RequireComponent(typeof(WheelCollider))]
public class SetWheelVisuals : MonoBehaviour
{
	public Transform visualTransform;

	private WheelCollider wheelCollider;

	private void Start()
	{
		wheelCollider = GetComponent<WheelCollider>();
	}

	private void FixedUpdate()
	{
		wheelCollider.GetWorldPose(out var pos, out var quat);
		visualTransform.position = pos;
		visualTransform.rotation = quat;
	}
}
