using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoR2;

public class SmoothTrailMesh : MonoBehaviour
{
	[Serializable]
	private struct Point
	{
		public Vector3 vertex1;

		public Vector3 vertex2;

		public float time;
	}

	private MeshFilter meshFilter;

	private MeshRenderer meshRenderer;

	private Mesh mesh;

	public float timeStep = 1f / 180f;

	public float width = 1f;

	public Material[] sharedMaterials;

	public float trailLifetime = 1f;

	public bool fadeVertexAlpha = true;

	private Vector3 previousPosition;

	private Vector3 previousUp;

	private float previousTime;

	private Queue<Point> pointsQueue = new Queue<Point>();

	private void Awake()
	{
		mesh = new Mesh();
		mesh.MarkDynamic();
		GameObject gameObject = new GameObject("SmoothTrailMeshRenderer");
		meshFilter = gameObject.AddComponent<MeshFilter>();
		meshFilter.mesh = mesh;
		meshRenderer = gameObject.AddComponent<MeshRenderer>();
		meshRenderer.sharedMaterials = sharedMaterials;
	}

	private void AddCurrentPoint()
	{
		float time = Time.time;
		Vector3 position = base.transform.position;
		Vector3 vector = base.transform.up * width * 0.5f;
		pointsQueue.Enqueue(new Point
		{
			vertex1 = position + vector,
			vertex2 = position - vector,
			time = time
		});
	}

	private void OnEnable()
	{
		AddCurrentPoint();
	}

	private void OnDisable()
	{
		pointsQueue.Clear();
		mesh.Clear();
	}

	private void OnDestroy()
	{
		if ((bool)meshFilter)
		{
			meshFilter.mesh = null;
			UnityEngine.Object.Destroy(meshFilter.gameObject);
		}
		UnityEngine.Object.Destroy(mesh);
	}

	private void Simulate()
	{
		float time = Time.time;
		Vector3 position = base.transform.position;
		Vector3 b = base.transform.up * width * 0.5f;
		float num = time - previousTime;
		if (num > 0f)
		{
			float num2 = 1f / num;
			for (float num3 = previousTime; num3 <= time; num3 += timeStep)
			{
				float t = (num3 - previousTime) * num2;
				Vector3 vector = Vector3.LerpUnclamped(previousPosition, position, t);
				Vector3 vector2 = Vector3.SlerpUnclamped(previousUp, b, t);
				pointsQueue.Enqueue(new Point
				{
					vertex1 = vector + vector2,
					vertex2 = vector - vector2,
					time = num3
				});
			}
		}
		float num4 = time - trailLifetime;
		while (pointsQueue.Count > 0 && pointsQueue.Peek().time < num4)
		{
			pointsQueue.Dequeue();
		}
		previousTime = time;
		previousPosition = position;
		previousUp = b;
	}

	private void LateUpdate()
	{
		Simulate();
		GenerateMesh();
	}

	private void GenerateMesh()
	{
		Vector3[] array = new Vector3[pointsQueue.Count * 2];
		Vector2[] array2 = new Vector2[pointsQueue.Count * 2];
		Color[] array3 = new Color[pointsQueue.Count * 2];
		_ = 1f / (float)pointsQueue.Count;
		int num = 0;
		if (pointsQueue.Count > 0)
		{
			float time = pointsQueue.Peek().time;
			float time2 = Time.time;
			float num2 = time2 - time;
			float num3 = 1f / num2;
			foreach (Point item in pointsQueue)
			{
				float num4 = (time2 - item.time) * num3;
				array[num] = item.vertex1;
				array2[num] = new Vector2(1f, num4);
				array3[num] = new Color(1f, 1f, 1f, fadeVertexAlpha ? (1f - num4) : 1f);
				num++;
				array[num] = item.vertex2;
				array2[num] = new Vector2(0f, num4);
				num++;
			}
		}
		int num5 = pointsQueue.Count - 1;
		int[] array4 = new int[num5 * 2 * 3];
		int num6 = 0;
		int num7 = 0;
		for (int i = 0; i < num5; i++)
		{
			array4[num6] = num7;
			array4[num6 + 1] = num7 + 1;
			array4[num6 + 2] = num7 + 2;
			array4[num6 + 3] = num7 + 3;
			array4[num6 + 4] = num7 + 1;
			array4[num6 + 5] = num7 + 2;
			num6 += 6;
			num7 += 2;
		}
		mesh.Clear();
		mesh.vertices = array;
		mesh.uv = array2;
		mesh.triangles = array4;
		mesh.colors = array3;
		mesh.RecalculateBounds();
		mesh.UploadMeshData(markNoLongerReadable: false);
	}
}
