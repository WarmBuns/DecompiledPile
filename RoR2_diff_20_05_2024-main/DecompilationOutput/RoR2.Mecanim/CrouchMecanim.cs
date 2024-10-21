using UnityEngine;

namespace RoR2.Mecanim;

public class CrouchMecanim : MonoBehaviour
{
	public float duckHeight;

	public Animator animator;

	public float smoothdamp;

	public float initialVerticalOffset;

	public Transform crouchOriginOverride;

	private float crouchCycle;

	private const float crouchRaycastFrequency = 2f;

	private float crouchStopwatch;

	private static readonly int crouchCycleOffsetParamNameHash = Animator.StringToHash("crouchCycleOffset");

	private void FixedUpdate()
	{
		crouchStopwatch -= Time.fixedDeltaTime;
		if (crouchStopwatch <= 0f)
		{
			crouchStopwatch = 0.5f;
			Transform transform = (crouchOriginOverride ? crouchOriginOverride : base.transform);
			Vector3 up = transform.up;
			RaycastHit hitInfo;
			bool flag = Physics.Raycast(new Ray(transform.position - up * initialVerticalOffset, up), out hitInfo, duckHeight + initialVerticalOffset, LayerIndex.world.mask, QueryTriggerInteraction.Ignore);
			crouchCycle = (flag ? Mathf.Clamp01(1f - (hitInfo.distance - initialVerticalOffset) / duckHeight) : 0f);
		}
	}

	private void Update()
	{
		if ((bool)animator)
		{
			animator.SetFloat(crouchCycleOffsetParamNameHash, crouchCycle, smoothdamp, Time.deltaTime);
		}
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawLine(base.transform.position, base.transform.position + base.transform.up * duckHeight);
		Gizmos.color = Color.red;
		Gizmos.DrawLine(base.transform.position, base.transform.position + -base.transform.up * initialVerticalOffset);
	}
}
