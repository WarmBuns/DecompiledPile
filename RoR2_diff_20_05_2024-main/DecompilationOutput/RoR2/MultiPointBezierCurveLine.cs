using System;
using UnityEngine;

namespace RoR2;

[ExecuteAlways]
[RequireComponent(typeof(LineRenderer))]
public class MultiPointBezierCurveLine : MonoBehaviour
{
	[Serializable]
	public struct Vertex
	{
		public Transform vertexTransform;

		public Vector3 position;

		public Vector3 localVelocity;
	}

	public Vertex[] vertexList;

	public Vector3[] linePositionList;

	[HideInInspector]
	public LineRenderer lineRenderer;

	private void Start()
	{
		lineRenderer = GetComponent<LineRenderer>();
	}

	private void LateUpdate()
	{
		for (int i = 0; i < linePositionList.Length; i++)
		{
			float globalT = (float)i / (float)(linePositionList.Length - 1);
			linePositionList[i] = EvaluateBezier(globalT);
		}
		lineRenderer.SetPositions(linePositionList);
	}

	private Vector3 EvaluateBezier(float globalT)
	{
		int num = vertexList.Length - 1;
		int num2;
		int num3 = Mathf.Min((num2 = Mathf.FloorToInt((float)num * globalT)) + 1, num);
		Vertex vertex = vertexList[num2];
		Vertex vertex2 = vertexList[num3];
		Vector3 vector = (vertex.vertexTransform ? vertex.vertexTransform.position : vertex.position);
		Vector3 vector2 = (vertex2.vertexTransform ? vertex2.vertexTransform.position : vertex2.position);
		Vector3 vector3 = (vertex.vertexTransform ? vertex.vertexTransform.TransformVector(vertex.localVelocity) : vertex.localVelocity);
		Vector3 vector4 = (vertex2.vertexTransform ? vertex2.vertexTransform.TransformVector(vertex2.localVelocity) : vertex2.localVelocity);
		if (num2 == num3)
		{
			return vector;
		}
		float inMin = (float)num2 / (float)num;
		float inMax = (float)num3 / (float)num;
		float num4 = Util.Remap(globalT, inMin, inMax, 0f, 1f);
		Vector3 a = Vector3.Lerp(vector, vector + vector3, num4);
		Vector3 b = Vector3.Lerp(vector2, vector2 + vector4, 1f - num4);
		return Vector3.Lerp(a, b, num4);
	}
}
