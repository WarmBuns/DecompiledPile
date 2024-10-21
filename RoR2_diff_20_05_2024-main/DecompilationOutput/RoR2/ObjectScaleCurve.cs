using UnityEngine;

namespace RoR2;

public class ObjectScaleCurve : MonoBehaviour
{
	public bool useOverallCurveOnly;

	public bool resetOnAwake = true;

	public bool useUnscaledTime;

	public AnimationCurve curveX;

	public AnimationCurve curveY;

	public AnimationCurve curveZ;

	public AnimationCurve overallCurve;

	public float timeMax = 5f;

	public float time { get; set; }

	public Vector3 baseScale { get; set; }

	private void Awake()
	{
		baseScale = base.transform.localScale;
		if (resetOnAwake)
		{
			Reset();
		}
	}

	private void OnEnable()
	{
		Reset();
	}

	public void Reset()
	{
		time = 0f;
		UpdateScale(0f);
	}

	private void Update()
	{
		time += (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime);
		UpdateScale(time);
	}

	private void UpdateScale(float time)
	{
		float num = ((timeMax > 0f) ? Mathf.Clamp01(time / timeMax) : 0f);
		float num2 = 1f;
		if (overallCurve != null)
		{
			num2 = overallCurve.Evaluate(num);
		}
		Vector3 vector;
		if (useOverallCurveOnly)
		{
			vector = baseScale * num2;
		}
		else
		{
			if (curveX == null || curveY == null || curveZ == null)
			{
				return;
			}
			vector = new Vector3(curveX.Evaluate(num) * baseScale.x, curveY.Evaluate(num) * baseScale.y, curveZ.Evaluate(num) * baseScale.z);
		}
		base.transform.localScale = vector * num2;
	}
}
