using UnityEngine;

namespace RoR2;

public class IKTargetPassive : MonoBehaviour, IIKTargetBehavior
{
	private float smoothedTargetHeightOffset;

	private float targetHeightOffset;

	private float smoothdampVelocity;

	public float minHeight = -0.3f;

	public float maxHeight = 1f;

	public float dampTime = 0.1f;

	public float baseOffset;

	[Tooltip("The IK weight float parameter if used")]
	public string animatorIKWeightFloat = "";

	public Animator animator;

	[Tooltip("The target transform will plant without any calls from external IK chains")]
	public bool selfPlant;

	public float selfPlantFrequency = 5f;

	[Tooltip("Whether or not to cache where the raycast begins. Used when not attached to bones, who reset themselves via animator.")]
	public bool cacheFirstPosition;

	private Vector3 cachedLocalPosition;

	private float selfPlantTimer;

	public void UpdateIKState(int targetState)
	{
	}

	private void Awake()
	{
		if (cacheFirstPosition)
		{
			cachedLocalPosition = base.transform.localPosition;
		}
	}

	private void LateUpdate()
	{
		selfPlantTimer -= Time.deltaTime;
		if (selfPlant && selfPlantTimer <= 0f)
		{
			selfPlantTimer = 1f / selfPlantFrequency;
			UpdateIKTargetPosition();
		}
		UpdateYOffset();
	}

	public void UpdateIKTargetPosition()
	{
		ResetTransformToCachedPosition();
		if (Physics.Raycast(base.transform.position + Vector3.up * (0f - minHeight), Vector3.down, out var hitInfo, maxHeight - minHeight, LayerIndex.world.mask))
		{
			targetHeightOffset = hitInfo.point.y - base.transform.position.y;
		}
		else
		{
			targetHeightOffset = 0f;
		}
		targetHeightOffset += baseOffset;
	}

	public void UpdateYOffset()
	{
		float t = 1f;
		if ((bool)animator && animatorIKWeightFloat.Length > 0)
		{
			t = animator.GetFloat(animatorIKWeightFloat);
		}
		smoothedTargetHeightOffset = Mathf.SmoothDamp(smoothedTargetHeightOffset, targetHeightOffset, ref smoothdampVelocity, dampTime, float.PositiveInfinity, Time.deltaTime);
		ResetTransformToCachedPosition();
		base.transform.position = new Vector3(base.transform.position.x, base.transform.position.y + Mathf.Lerp(0f, smoothedTargetHeightOffset, t), base.transform.position.z);
	}

	private void ResetTransformToCachedPosition()
	{
		if (cacheFirstPosition)
		{
			base.transform.localPosition = new Vector3(cachedLocalPosition.x, cachedLocalPosition.y, cachedLocalPosition.z);
		}
	}
}
