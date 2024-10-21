using System;
using UnityEngine;

[ExecuteAlways]
public class BeamPointsFromTransforms : MonoBehaviour
{
	[Tooltip("Line Renderer to set the positions of.")]
	public LineRenderer target;

	[SerializeField]
	[Tooltip("Transforms to use as the points for the line renderer.")]
	private Transform[] pointTransforms = Array.Empty<Transform>();

	private void Start()
	{
		UpdateBeamPositions();
	}

	private void Update()
	{
		UpdateBeamPositions();
	}

	private void UpdateBeamPositions()
	{
		if (!target)
		{
			return;
		}
		int num = pointTransforms.Length;
		target.positionCount = num;
		for (int i = 0; i < num; i++)
		{
			Transform transform = pointTransforms[i];
			if ((bool)transform)
			{
				target.SetPosition(i, transform.position);
			}
		}
	}
}
