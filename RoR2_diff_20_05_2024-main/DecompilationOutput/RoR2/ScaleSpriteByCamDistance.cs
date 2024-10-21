using System.Collections.Generic;
using UnityEngine;

namespace RoR2;

public class ScaleSpriteByCamDistance : MonoBehaviour
{
	public enum ScalingMode
	{
		Direct,
		Square,
		Sqrt,
		Cube,
		CubeRoot
	}

	private static List<ScaleSpriteByCamDistance> instancesList;

	private new Transform transform;

	[Tooltip("The amount by which to scale.")]
	public float scaleFactor = 1f;

	public ScalingMode scalingMode;

	static ScaleSpriteByCamDistance()
	{
		instancesList = new List<ScaleSpriteByCamDistance>();
		SceneCamera.onSceneCameraPreCull += OnSceneCameraPreCull;
	}

	private void Awake()
	{
		transform = base.transform;
	}

	private void OnEnable()
	{
		instancesList.Add(this);
	}

	private void OnDisable()
	{
		instancesList.Remove(this);
	}

	private static void OnSceneCameraPreCull(SceneCamera sceneCamera)
	{
		Vector3 position = sceneCamera.transform.position;
		for (int i = 0; i < instancesList.Count; i++)
		{
			ScaleSpriteByCamDistance scaleSpriteByCamDistance = instancesList[i];
			Transform transform = scaleSpriteByCamDistance.transform;
			float num = 1f;
			float num2 = Vector3.Distance(position, transform.position);
			switch (scaleSpriteByCamDistance.scalingMode)
			{
			case ScalingMode.Direct:
				num = num2;
				break;
			case ScalingMode.Square:
				num = num2 * num2;
				break;
			case ScalingMode.Sqrt:
				num = Mathf.Sqrt(num2);
				break;
			case ScalingMode.Cube:
				num = num2 * num2 * num2;
				break;
			case ScalingMode.CubeRoot:
				num = Mathf.Pow(num2, 1f / 3f);
				break;
			}
			num *= scaleSpriteByCamDistance.scaleFactor;
			transform.localScale = new Vector3(num, num, num);
		}
	}
}
