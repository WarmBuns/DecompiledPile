using System;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class RootMotionAccumulator : MonoBehaviour
{
	private Animator animator;

	[NonSerialized]
	public Vector3 accumulatedRootMotion;

	public Quaternion accumulatedRootRotation;

	public bool accumulateRotation;

	public Vector3 ExtractRootMotion()
	{
		Vector3 result = accumulatedRootMotion;
		accumulatedRootMotion = Vector3.zero;
		return result;
	}

	public Quaternion ExtractRootRotation()
	{
		Quaternion result = accumulatedRootRotation;
		accumulatedRootRotation = Quaternion.identity;
		return result;
	}

	private void Awake()
	{
		animator = GetComponent<Animator>();
		accumulatedRootRotation = Quaternion.identity;
	}

	private void OnAnimatorMove()
	{
		accumulatedRootMotion += animator.deltaPosition;
		if (accumulateRotation)
		{
			accumulatedRootRotation *= animator.deltaRotation;
		}
	}
}
