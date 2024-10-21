using System;
using UnityEngine;

[Serializable]
public struct CubicBezier3
{
	public Vector3 a;

	public Vector3 b;

	public Vector3 c;

	public Vector3 d;

	public Vector3 p0 => a;

	public Vector3 p1 => d;

	public Vector3 v0 => b - a;

	public Vector3 v1 => c - d;

	public static CubicBezier3 FromVelocities(Vector3 p0, Vector3 v0, Vector3 p1, Vector3 v1)
	{
		CubicBezier3 result = default(CubicBezier3);
		result.a = p0;
		result.b = p0 + v0;
		result.c = p1 + v1;
		result.d = p1;
		return result;
	}

	public Vector3 Evaluate(float t)
	{
		float num = t * t;
		float num2 = num * t;
		return a + 3f * t * (b - a) + 3f * num * (c - 2f * b + a) + num2 * (d - 3f * c + 3f * b - a);
	}

	public void ToVertices(Vector3[] results)
	{
		ToVertices(results, 0, results.Length);
	}

	public void ToVertices(Vector3[] results, int spanStart, int spanLength)
	{
		float num = 1f / (float)(spanLength - 1);
		for (int i = 0; i < spanLength; i++)
		{
			results[spanStart++] = Evaluate((float)i * num);
		}
	}

	public float ApproximateLength(int samples)
	{
		float num = 1f / (float)(samples - 1);
		float num2 = 0f;
		Vector3 vector = p0;
		for (int i = 1; i < samples; i++)
		{
			Vector3 vector2 = Evaluate((float)i * num);
			num2 += Vector3.Distance(vector, vector2);
			vector = vector2;
		}
		return num2;
	}
}
