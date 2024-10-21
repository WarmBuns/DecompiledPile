using System;
using UnityEngine;

namespace RoR2;

public class StriderLegController : MonoBehaviour
{
	[Serializable]
	public struct FootInfo
	{
		public Transform transform;

		public Transform referenceTransform;

		[HideInInspector]
		public Vector3 velocity;

		[HideInInspector]
		public FootState footState;

		[HideInInspector]
		public Vector3 plantPosition;

		[HideInInspector]
		public Vector3 trailingTargetPosition;

		[HideInInspector]
		public float stopwatch;

		[HideInInspector]
		public float currentYOffsetFromRaycast;

		[HideInInspector]
		public float lastYOffsetFromRaycast;

		[HideInInspector]
		public float footRaycastTimer;
	}

	public enum FootState
	{
		Planted,
		Replanting
	}

	[Header("Foot Settings")]
	public Transform centerOfGravity;

	public FootInfo[] feet;

	public Vector3 footRaycastDirection;

	public float raycastVerticalOffset;

	public float maxRaycastDistance;

	public float footDampTime;

	public float stabilityRadius;

	public float replantDuration;

	public float replantHeight;

	public float overstepDistance;

	public AnimationCurve lerpCurve;

	public GameObject footPlantEffect;

	public string footPlantString;

	public string footMoveString;

	public float footRaycastFrequency = 0.2f;

	public int maxFeetReplantingAtOnce = 9999;

	[Header("Root Settings")]
	public Transform rootTransform;

	public float rootSpringConstant;

	public float rootDampingConstant;

	public float rootOffsetHeight;

	public float rootSmoothDamp;

	private float rootVelocity;

	public Vector3 GetCenterOfStance()
	{
		Vector3 zero = Vector3.zero;
		for (int i = 0; i < feet.Length; i++)
		{
			zero += feet[i].transform.position;
		}
		return zero / feet.Length;
	}

	private void Awake()
	{
		for (int i = 0; i < feet.Length; i++)
		{
			feet[i].footState = FootState.Planted;
			feet[i].plantPosition = feet[i].referenceTransform.position;
			feet[i].trailingTargetPosition = feet[i].plantPosition;
			feet[i].footRaycastTimer = UnityEngine.Random.Range(0f, 1f / footRaycastFrequency);
		}
	}

	private void Update()
	{
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < feet.Length; i++)
		{
			if (feet[i].footState == FootState.Replanting)
			{
				num2++;
			}
		}
		for (int j = 0; j < feet.Length; j++)
		{
			feet[j].footRaycastTimer -= Time.deltaTime;
			Transform transform = feet[j].transform;
			Transform referenceTransform = feet[j].referenceTransform;
			_ = transform.position;
			Vector3 vector = Vector3.zero;
			float num3 = 0f;
			switch (feet[j].footState)
			{
			case FootState.Planted:
				num++;
				vector = feet[j].plantPosition;
				if ((referenceTransform.position - vector).sqrMagnitude > stabilityRadius * stabilityRadius && num2 < maxFeetReplantingAtOnce)
				{
					feet[j].footState = FootState.Replanting;
					Util.PlaySound(footMoveString, transform.gameObject);
					num2++;
				}
				break;
			case FootState.Replanting:
			{
				feet[j].stopwatch += Time.deltaTime;
				Vector3 plantPosition = feet[j].plantPosition;
				Vector3 position = referenceTransform.position;
				position += Vector3.ProjectOnPlane(position - plantPosition, Vector3.up).normalized * overstepDistance;
				float num4 = lerpCurve.Evaluate(feet[j].stopwatch / replantDuration);
				vector = Vector3.Lerp(plantPosition, position, num4);
				num3 = Mathf.Sin(num4 * MathF.PI) * replantHeight;
				if (feet[j].stopwatch >= replantDuration)
				{
					feet[j].plantPosition = position;
					feet[j].stopwatch = 0f;
					feet[j].footState = FootState.Planted;
					Util.PlaySound(footPlantString, transform.gameObject);
					if ((bool)footPlantEffect)
					{
						EffectManager.SimpleEffect(footPlantEffect, transform.position, Quaternion.identity, transmit: false);
					}
				}
				break;
			}
			}
			Ray ray = default(Ray);
			ray.direction = transform.TransformDirection(footRaycastDirection.normalized);
			ray.origin = vector - ray.direction * raycastVerticalOffset;
			if (feet[j].footRaycastTimer <= 0f)
			{
				feet[j].footRaycastTimer = 1f / footRaycastFrequency;
				feet[j].lastYOffsetFromRaycast = feet[j].currentYOffsetFromRaycast;
				if (Physics.Raycast(ray, out var hitInfo, maxRaycastDistance + raycastVerticalOffset, LayerIndex.world.mask))
				{
					feet[j].currentYOffsetFromRaycast = hitInfo.point.y - vector.y;
				}
				else
				{
					feet[j].currentYOffsetFromRaycast = 0f;
				}
			}
			float num5 = Mathf.Lerp(feet[j].currentYOffsetFromRaycast, feet[j].lastYOffsetFromRaycast, feet[j].footRaycastTimer / (1f / footRaycastFrequency));
			vector.y += num3 + num5;
			feet[j].trailingTargetPosition = Vector3.SmoothDamp(feet[j].trailingTargetPosition, vector, ref feet[j].velocity, footDampTime);
			transform.position = feet[j].trailingTargetPosition;
		}
		if ((bool)rootTransform)
		{
			Vector3 localPosition = rootTransform.localPosition;
			float num6 = (1f - (float)num / (float)feet.Length) * rootOffsetHeight;
			float target = localPosition.z - num6;
			float z = Mathf.SmoothDamp(localPosition.z, target, ref rootVelocity, rootSmoothDamp);
			rootTransform.localPosition = new Vector3(localPosition.x, localPosition.y, z);
		}
	}

	public Vector3 GetArcPosition(Vector3 start, Vector3 end, float arcHeight, float t)
	{
		return Vector3.Lerp(start, end, Mathf.Sin(t * MathF.PI * 0.5f)) + new Vector3(0f, Mathf.Sin(t * MathF.PI) * arcHeight, 0f);
	}

	public void OnDrawGizmos()
	{
		for (int i = 0; i < feet.Length; i++)
		{
			Gizmos.color = Color.green;
			Gizmos.DrawRay(feet[i].transform.position, feet[i].transform.TransformVector(footRaycastDirection));
		}
	}
}
