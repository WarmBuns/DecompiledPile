using System;
using UnityEngine;

namespace RoR2;

public class BodyAnimatorSmoothingParameters : MonoBehaviour
{
	[Serializable]
	public struct SmoothingParameters
	{
		public float walkMagnitudeSmoothDamp;

		public float walkAngleSmoothDamp;

		public float forwardSpeedSmoothDamp;

		public float rightSpeedSmoothDamp;

		public float intoJumpTransitionTime;

		public float turnAngleSmoothDamp;
	}

	public SmoothingParameters smoothingParameters;

	public static SmoothingParameters defaultParameters = new SmoothingParameters
	{
		walkMagnitudeSmoothDamp = 0.2f,
		walkAngleSmoothDamp = 0.2f,
		forwardSpeedSmoothDamp = 0.1f,
		rightSpeedSmoothDamp = 0.1f,
		intoJumpTransitionTime = 0.05f,
		turnAngleSmoothDamp = 1f
	};
}
