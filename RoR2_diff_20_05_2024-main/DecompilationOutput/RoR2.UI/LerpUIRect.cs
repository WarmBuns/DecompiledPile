using UnityEngine;

namespace RoR2.UI;

[RequireComponent(typeof(RectTransform))]
public class LerpUIRect : MonoBehaviour
{
	public enum LerpState
	{
		Entering,
		Holding,
		Leaving
	}

	public Vector3 startLocalPosition;

	public Vector3 finalLocalPosition;

	public LerpState lerpState;

	public AnimationCurve enterCurve;

	public float enterDuration;

	public AnimationCurve leavingCurve;

	public float leaveDuration;

	private float stopwatch;

	private RectTransform rectTransform;

	private void Start()
	{
		rectTransform = GetComponent<RectTransform>();
	}

	private void OnDisable()
	{
		lerpState = LerpState.Entering;
		stopwatch = 0f;
		UpdateLerp();
	}

	private void Update()
	{
		stopwatch += Time.deltaTime;
		UpdateLerp();
	}

	private void UpdateLerp()
	{
		switch (lerpState)
		{
		case LerpState.Entering:
		{
			float num = stopwatch / enterDuration;
			float t = enterCurve.Evaluate(num);
			rectTransform.anchoredPosition = Vector3.LerpUnclamped(startLocalPosition, finalLocalPosition, t);
			if (num >= 1f)
			{
				lerpState = LerpState.Holding;
				stopwatch = 0f;
			}
			break;
		}
		case LerpState.Leaving:
		{
			float num = stopwatch / enterDuration;
			float t = leavingCurve.Evaluate(num);
			rectTransform.anchoredPosition = Vector3.LerpUnclamped(finalLocalPosition, startLocalPosition, t);
			if (num >= 1f)
			{
				lerpState = LerpState.Holding;
				stopwatch = 0f;
			}
			break;
		}
		}
	}
}
