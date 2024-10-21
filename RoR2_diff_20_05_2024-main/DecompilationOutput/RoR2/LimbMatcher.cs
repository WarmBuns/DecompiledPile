using System;
using UnityEngine;

namespace RoR2;

public class LimbMatcher : MonoBehaviour
{
	[Serializable]
	public struct LimbPair
	{
		public Transform originalTransform;

		public string targetChildLimb;

		public float originalLimbLength;

		[NonSerialized]
		public Transform targetTransform;
	}

	public bool scaleLimbs = true;

	private bool valid;

	public LimbPair[] limbPairs;

	public void SetChildLocator(ChildLocator childLocator)
	{
		valid = true;
		for (int i = 0; i < limbPairs.Length; i++)
		{
			LimbPair limbPair = limbPairs[i];
			Transform transform = childLocator.FindChild(limbPair.targetChildLimb);
			if (!transform)
			{
				valid = false;
				break;
			}
			limbPairs[i].targetTransform = transform;
		}
	}

	private void LateUpdate()
	{
		UpdateLimbs();
	}

	private void UpdateLimbs()
	{
		if (!valid)
		{
			return;
		}
		for (int i = 0; i < limbPairs.Length; i++)
		{
			LimbPair limbPair = limbPairs[i];
			Transform targetTransform = limbPair.targetTransform;
			if (!targetTransform || !limbPair.originalTransform)
			{
				continue;
			}
			limbPair.originalTransform.position = targetTransform.position;
			limbPair.originalTransform.rotation = targetTransform.rotation;
			if (i < limbPairs.Length - 1)
			{
				float num = Vector3.Magnitude(limbPairs[i + 1].targetTransform.position - targetTransform.position);
				float originalLimbLength = limbPair.originalLimbLength;
				if (scaleLimbs)
				{
					Vector3 localScale = limbPair.originalTransform.localScale;
					localScale.y = num / originalLimbLength;
					limbPair.originalTransform.localScale = localScale;
				}
			}
		}
	}
}
