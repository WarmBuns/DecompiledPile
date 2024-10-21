using System;
using UnityEngine;

namespace RoR2;

public class IKTargetPlant : MonoBehaviour, IIKTargetBehavior
{
	public enum IKState
	{
		Plant,
		Reset
	}

	[Tooltip("The max offset to step up")]
	public float minHeight = -0.3f;

	[Tooltip("The max offset to step down")]
	public float maxHeight = 1f;

	[Tooltip("The strength of the IK as a lerp (0-1)")]
	public float ikWeight = 1f;

	[Tooltip("The time to restep")]
	public float timeToReset = 0.6f;

	[Tooltip("The max positional IK error before restepping")]
	public float maxXZPositionalError = 4f;

	public GameObject plantEffect;

	public Animator animator;

	[Tooltip("The IK weight float parameter if used")]
	public string animatorIKWeightFloat;

	[Tooltip("The lift animation trigger string if used")]
	public string animatorLiftTrigger;

	[Tooltip("The scale of the leg for calculating if the leg is too short to reach the IK target")]
	public float legScale = 1f;

	[Tooltip("The height of the step arc")]
	public float arcHeight = 1f;

	[Tooltip("The smoothing duration for the IK. Higher will be smoother but will be delayed.")]
	public float smoothDampTime = 0.1f;

	[Tooltip("Spherecasts will have more hits but take higher performance.")]
	public bool useSpherecast;

	public float spherecastRadius = 0.5f;

	public IKState ikState;

	private bool isPlanted;

	private Vector3 lastTransformPosition;

	private Vector3 smoothDampRefVelocity;

	private Vector3 targetPosition;

	private Vector3 plantPosition;

	private IKSimpleChain ikChain;

	private float resetTimer;

	private void Awake()
	{
		ikChain = GetComponent<IKSimpleChain>();
	}

	public void UpdateIKState(int targetState)
	{
		if (ikState != IKState.Reset)
		{
			ikState = (IKState)targetState;
		}
	}

	public Vector3 GetArcPosition(Vector3 start, Vector3 end, float arcHeight, float t)
	{
		return Vector3.Lerp(start, end, Mathf.Sin(t * MathF.PI * 0.5f)) + new Vector3(0f, Mathf.Sin(t * MathF.PI) * arcHeight, 0f);
	}

	public void UpdateIKTargetPosition()
	{
		if ((bool)animator)
		{
			ikWeight = animator.GetFloat(animatorIKWeightFloat);
		}
		else
		{
			ikWeight = 1f;
		}
		switch (ikState)
		{
		case IKState.Reset:
			resetTimer += Time.deltaTime;
			isPlanted = false;
			RaycastIKTarget(base.transform.position);
			base.transform.position = GetArcPosition(plantPosition, targetPosition, arcHeight, resetTimer / timeToReset);
			if (resetTimer >= timeToReset)
			{
				ikState = IKState.Plant;
				isPlanted = true;
				plantPosition = targetPosition;
				UnityEngine.Object.Instantiate(plantEffect, plantPosition, Quaternion.identity);
			}
			break;
		case IKState.Plant:
		{
			Vector3 position = base.transform.position;
			RaycastIKTarget(position);
			if (!isPlanted)
			{
				plantPosition = targetPosition;
				base.transform.position = plantPosition;
				isPlanted = true;
				if ((bool)plantEffect)
				{
					UnityEngine.Object.Instantiate(plantEffect, plantPosition, Quaternion.identity);
				}
			}
			else
			{
				base.transform.position = Vector3.Lerp(position, plantPosition, ikWeight);
			}
			Vector3 vector = position - base.transform.position;
			vector.y = 0f;
			if (ikChain.LegTooShort(legScale) || vector.sqrMagnitude >= maxXZPositionalError * maxXZPositionalError)
			{
				plantPosition = base.transform.position;
				ikState = IKState.Reset;
				if ((bool)animator)
				{
					animator.SetTrigger(animatorLiftTrigger);
				}
				resetTimer = 0f;
			}
			break;
		}
		}
		base.transform.position = Vector3.SmoothDamp(lastTransformPosition, base.transform.position, ref smoothDampRefVelocity, smoothDampTime);
		lastTransformPosition = base.transform.position;
	}

	public void RaycastIKTarget(Vector3 position)
	{
		RaycastHit hitInfo;
		if (useSpherecast)
		{
			Physics.SphereCast(position + Vector3.up * (0f - minHeight), spherecastRadius, Vector3.down, out hitInfo, maxHeight - minHeight, LayerIndex.world.mask);
		}
		else
		{
			Physics.Raycast(position + Vector3.up * (0f - minHeight), Vector3.down, out hitInfo, maxHeight - minHeight, LayerIndex.world.mask);
		}
		if ((bool)hitInfo.collider)
		{
			targetPosition = hitInfo.point;
		}
		else
		{
			targetPosition = position;
		}
	}
}
