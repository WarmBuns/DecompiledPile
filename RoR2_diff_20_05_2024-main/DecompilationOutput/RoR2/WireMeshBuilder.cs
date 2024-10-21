using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoR2;

public class WireMeshBuilder : IDisposable
{
	private struct LineVertex
	{
		public Vector3 position;

		public Color color;
	}

	private int uniqueVertexCount;

	private Dictionary<LineVertex, int> uniqueVertexToIndex = new Dictionary<LineVertex, int>();

	private List<int> indices = new List<int>();

	private List<Vector3> positions = new List<Vector3>();

	private List<Color> colors = new List<Color>();

	private int GetVertexIndex(LineVertex vertex)
	{
		if (!uniqueVertexToIndex.TryGetValue(vertex, out var value))
		{
			value = uniqueVertexCount++;
			positions.Add(vertex.position);
			colors.Add(vertex.color);
			uniqueVertexToIndex.Add(vertex, value);
		}
		return value;
	}

	public void Clear()
	{
		uniqueVertexToIndex.Clear();
		indices.Clear();
		positions.Clear();
		colors.Clear();
		uniqueVertexCount = 0;
	}

	public void AddLine(Vector3 p1, Color c1, Vector3 p2, Color c2)
	{
		LineVertex lineVertex = default(LineVertex);
		lineVertex.position = p1;
		lineVertex.color = c1;
		LineVertex vertex = lineVertex;
		lineVertex = default(LineVertex);
		lineVertex.position = p2;
		lineVertex.color = c2;
		LineVertex vertex2 = lineVertex;
		int vertexIndex = GetVertexIndex(vertex);
		int vertexIndex2 = GetVertexIndex(vertex2);
		indices.Add(vertexIndex);
		indices.Add(vertexIndex2);
	}

	public Mesh GenerateMesh()
	{
		Mesh mesh = new Mesh();
		GenerateMesh(mesh);
		return mesh;
	}

	public void GenerateMesh(Mesh dest)
	{
		dest.SetTriangles(Array.Empty<int>(), 0);
		dest.SetVertices(positions);
		dest.SetColors(colors);
		dest.SetIndices(indices.ToArray(), MeshTopology.Lines, 0);
	}

	public void Dispose()
	{
		uniqueVertexToIndex = null;
		indices = null;
		positions = null;
		colors = null;
	}
}
