using System;
using UnityEngine;
using UnityEngine.Serialization;

[ExecuteAlways]
[RequireComponent(typeof(LineRenderer))]
public class LineBetweenTransforms : MonoBehaviour
{
	[FormerlySerializedAs("transformNodes")]
	[SerializeField]
	[Tooltip("The list of transforms whose positions will drive the vertex positions of the sibling LineRenderer component.")]
	private Transform[] _transformNodes = Array.Empty<Transform>();

	private LineRenderer lineRenderer;

	private Vector3[] vertexList = Array.Empty<Vector3>();

	public Transform[] transformNodes
	{
		get
		{
			return _transformNodes;
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			_transformNodes = value;
			UpdateVertexBufferSize();
		}
	}

	private void PushPositionsToLineRenderer()
	{
		Vector3[] array = vertexList;
		Transform[] array2 = transformNodes;
		for (int i = 0; i < array.Length; i++)
		{
			Transform transform = array2[i];
			if ((bool)transform)
			{
				array[i] = transform.position;
			}
		}
		lineRenderer.SetPositions(array);
	}

	private void UpdateVertexBufferSize()
	{
		Array.Resize(ref vertexList, transformNodes.Length);
	}

	private void Awake()
	{
		lineRenderer = GetComponent<LineRenderer>();
		UpdateVertexBufferSize();
	}

	private void LateUpdate()
	{
		PushPositionsToLineRenderer();
	}
}
