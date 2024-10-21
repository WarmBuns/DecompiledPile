using UnityEngine;

namespace RoR2;

public class ObjectTransformCurve : MonoBehaviour
{
	public bool useRotationCurves;

	public bool useTranslationCurves;

	public bool loop;

	public bool randomizeInitialTime;

	public AnimationCurve rotationCurveX;

	public AnimationCurve rotationCurveY;

	public AnimationCurve rotationCurveZ;

	public AnimationCurve translationCurveX;

	public AnimationCurve translationCurveY;

	public AnimationCurve translationCurveZ;

	public float timeMax = 5f;

	public float time { get; set; }

	public Vector3 basePosition { get; private set; }

	public Quaternion baseRotation { get; private set; }

	private void Awake()
	{
		basePosition = base.transform.localPosition;
		baseRotation = base.transform.localRotation;
		Reset();
	}

	private void OnEnable()
	{
		Reset();
	}

	public void Reset()
	{
		time = 0f;
		if (randomizeInitialTime)
		{
			time = Random.Range(0f, timeMax);
		}
		UpdateTransform(time);
	}

	private void Update()
	{
		time += Time.deltaTime;
		if (loop && time > timeMax)
		{
			time %= timeMax;
		}
		UpdateTransform(time);
	}

	private void UpdateTransform(float time)
	{
		Vector3 vector = basePosition;
		Quaternion quaternion = baseRotation;
		float num = ((timeMax > 0f) ? Mathf.Clamp01(time / timeMax) : 0f);
		if (useRotationCurves)
		{
			if (rotationCurveX == null || rotationCurveY == null || rotationCurveZ == null)
			{
				return;
			}
			quaternion = Quaternion.Euler(rotationCurveX.Evaluate(num), rotationCurveY.Evaluate(num), rotationCurveZ.Evaluate(num));
			base.transform.localRotation = quaternion;
		}
		if (useTranslationCurves && translationCurveX != null && translationCurveY != null && translationCurveZ != null)
		{
			vector = new Vector3(translationCurveX.Evaluate(num), translationCurveY.Evaluate(num), translationCurveZ.Evaluate(num));
			base.transform.localPosition = vector;
		}
	}
}
