using System;
using UnityEngine;

namespace RoR2;

public class BasicBezierSpline : MonoBehaviour, ISerializationCallbackReceiver
{
	private struct KeyFrame
	{
		public CubicBezier3 curve;

		public float approximateLength;
	}

	public BasicBezierSplineControlPoint[] controlPoints = Array.Empty<BasicBezierSplineControlPoint>();

	public int samplesPerSegment = 20;

	private KeyFrame[] keyFrames = Array.Empty<KeyFrame>();

	private Vector3[] samplesBuffer = Array.Empty<Vector3>();

	private float totalLength;

	private bool keyFramesDirty = true;

	private void BuildKeyFrames()
	{
		totalLength = 0f;
		Array.Resize(ref keyFrames, controlPoints.Length);
		Array.Resize(ref samplesBuffer, samplesPerSegment);
		int i = 0;
		for (int num = controlPoints.Length - 1; i < num; i++)
		{
			BasicBezierSplineControlPoint basicBezierSplineControlPoint = controlPoints[i];
			BasicBezierSplineControlPoint basicBezierSplineControlPoint2 = controlPoints[i + 1];
			if (!basicBezierSplineControlPoint || !basicBezierSplineControlPoint2)
			{
				break;
			}
			CubicBezier3 curveSegment = GetCurveSegment(basicBezierSplineControlPoint, basicBezierSplineControlPoint2);
			float num2 = curveSegment.ApproximateLength(samplesPerSegment);
			keyFrames[i] = new KeyFrame
			{
				curve = curveSegment,
				approximateLength = num2
			};
			totalLength += num2;
		}
	}

	public Vector3 Evaluate(float normalizedPosition)
	{
		if (keyFramesDirty)
		{
			if (Application.isPlaying)
			{
				keyFramesDirty = false;
			}
			BuildKeyFrames();
		}
		float num = normalizedPosition * totalLength;
		float num2 = 0f;
		int i = 0;
		for (int num3 = keyFrames.Length; i < num3; i++)
		{
			ref KeyFrame reference = ref keyFrames[i];
			float a = num2;
			num2 += reference.approximateLength;
			if (num2 >= num)
			{
				float t = Mathf.InverseLerp(a, num2, num);
				return EvaluateKeyFrame(t, in reference);
			}
		}
		if (keyFrames.Length != 0)
		{
			return keyFrames[keyFrames.Length - 1].curve.p1;
		}
		return Vector3.zero;
	}

	private Vector3 EvaluateKeyFrame(float t, in KeyFrame keyFrame)
	{
		float num = t * keyFrame.approximateLength;
		keyFrame.curve.ToVertices(samplesBuffer);
		float a = 0f;
		float num2 = 0f;
		Vector3 vector = samplesBuffer[0];
		for (int i = 1; i < samplesBuffer.Length; i++)
		{
			Vector3 vector2 = samplesBuffer[i];
			float num3 = Vector3.Distance(vector2, vector);
			num2 += num3;
			if (num2 >= num)
			{
				return Vector3.Lerp(vector, vector2, Mathf.InverseLerp(a, num2, num));
			}
			vector = vector2;
			a = num2;
		}
		return keyFrame.curve.p1;
	}

	private void DrawKeyFrame(in KeyFrame keyFrame)
	{
		Gizmos.color = Color.Lerp(Color.green, Color.black, 0.5f);
		Gizmos.DrawRay(keyFrame.curve.p0, keyFrame.curve.v0);
		Gizmos.color = Color.Lerp(Color.red, Color.black, 0.5f);
		Gizmos.DrawRay(keyFrame.curve.p1, keyFrame.curve.v1);
		for (int i = 1; i <= 20; i++)
		{
			float num = (float)i * 0.05f;
			Gizmos.color = Color.Lerp(Color.red, Color.green, num);
			Vector3 vector = keyFrame.curve.Evaluate(num - 0.05f);
			Vector3 vector2 = keyFrame.curve.Evaluate(num);
			Gizmos.DrawRay(vector, vector2 - vector);
		}
	}

	public void OnDrawGizmos()
	{
		if (keyFramesDirty)
		{
			if (Application.isPlaying)
			{
				keyFramesDirty = false;
			}
			BuildKeyFrames();
		}
		for (int i = 0; i < keyFrames.Length; i++)
		{
			DrawKeyFrame(in keyFrames[i]);
		}
	}

	public static CubicBezier3 GetCurveSegment(BasicBezierSplineControlPoint startControlPoint, BasicBezierSplineControlPoint endControlPoint)
	{
		Transform obj = startControlPoint.transform;
		Transform transform = endControlPoint.transform;
		Vector3 position = obj.position;
		Vector3 v = obj.rotation * startControlPoint.forwardVelocity;
		Vector3 position2 = transform.position;
		Vector3 v2 = transform.rotation * endControlPoint.backwardVelocity;
		return CubicBezier3.FromVelocities(position, v, position2, v2);
	}

	public void OnBeforeSerialize()
	{
	}

	public void OnAfterDeserialize()
	{
		keyFramesDirty = true;
	}
}
