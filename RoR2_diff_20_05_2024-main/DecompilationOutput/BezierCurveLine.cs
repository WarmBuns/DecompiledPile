using System;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(LineRenderer))]
public class BezierCurveLine : MonoBehaviour
{
	private Vector3[] vertexList = Array.Empty<Vector3>();

	private Vector3 p0 = Vector3.zero;

	public Vector3 v0 = Vector3.zero;

	public Vector3 p1 = Vector3.zero;

	public Vector3 v1 = Vector3.zero;

	public Transform endTransform;

	public bool animateBezierWind;

	public Vector3 windMagnitude;

	public Vector3 windFrequency;

	private Vector3 windPhaseShift;

	private Vector3 lastWind;

	private Vector3 finalv0;

	private Vector3 finalv1;

	private float windTime;

	public LineRenderer lineRenderer { get; private set; }

	private void Awake()
	{
		lineRenderer = GetComponent<LineRenderer>();
		windPhaseShift = UnityEngine.Random.insideUnitSphere * 360f;
		Array.Resize(ref vertexList, lineRenderer.positionCount + 1);
		UpdateBezier(0f);
	}

	public void OnEnable()
	{
		Array.Resize(ref vertexList, lineRenderer.positionCount + 1);
	}

	private void LateUpdate()
	{
		UpdateBezier(Time.deltaTime);
	}

	public void UpdateBezier(float deltaTime)
	{
		windTime += deltaTime;
		p0 = base.transform.position;
		if ((bool)endTransform)
		{
			p1 = endTransform.position;
		}
		if (animateBezierWind)
		{
			finalv0 = v0 + new Vector3(Mathf.Sin(MathF.PI / 180f * (windTime * 360f + windPhaseShift.x) * windFrequency.x) * windMagnitude.x, Mathf.Sin(MathF.PI / 180f * (windTime * 360f + windPhaseShift.y) * windFrequency.y) * windMagnitude.y, Mathf.Sin(MathF.PI / 180f * (windTime * 360f + windPhaseShift.z) * windFrequency.z) * windMagnitude.z);
			finalv1 = v1 + new Vector3(Mathf.Sin(MathF.PI / 180f * (windTime * 360f + windPhaseShift.x + p1.x) * windFrequency.x) * windMagnitude.x, Mathf.Sin(MathF.PI / 180f * (windTime * 360f + windPhaseShift.y + p1.z) * windFrequency.y) * windMagnitude.y, Mathf.Sin(MathF.PI / 180f * (windTime * 360f + windPhaseShift.z + p1.y) * windFrequency.z) * windMagnitude.z);
		}
		else
		{
			finalv0 = v0;
			finalv1 = v1;
		}
		for (int i = 0; i < vertexList.Length; i++)
		{
			float t = (float)i / (float)(vertexList.Length - 2);
			vertexList[i] = EvaluateBezier(t);
		}
		lineRenderer.SetPositions(vertexList);
	}

	private Vector3 EvaluateBezier(float t)
	{
		Vector3 a = Vector3.Lerp(p0, p0 + finalv0, t);
		Vector3 b = Vector3.Lerp(p1, p1 + finalv1, 1f - t);
		return Vector3.Lerp(a, b, t);
	}
}
