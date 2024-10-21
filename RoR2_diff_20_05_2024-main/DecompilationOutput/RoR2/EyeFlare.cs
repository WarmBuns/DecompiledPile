using System.Collections.Generic;
using UnityEngine;

namespace RoR2;

public class EyeFlare : MonoBehaviour
{
	[Tooltip("The transform whose forward vector will be tested against the camera angle to determine scaling. This is usually the parent, and never this object since billboarding will affect the direction.")]
	public Transform directionSource;

	public float localScale = 1f;

	private static List<EyeFlare> instancesList;

	private new Transform transform;

	static EyeFlare()
	{
		instancesList = new List<EyeFlare>();
		SceneCamera.onSceneCameraPreCull += OnSceneCameraPreCull;
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
		Transform obj = Camera.current.transform;
		Quaternion rotation = obj.rotation;
		Vector3 forward = obj.forward;
		for (int i = 0; i < instancesList.Count; i++)
		{
			EyeFlare eyeFlare = instancesList[i];
			float num = eyeFlare.localScale;
			if ((bool)eyeFlare.directionSource)
			{
				float num2 = Vector3.Dot(forward, eyeFlare.directionSource.forward) * -0.5f + 0.5f;
				num *= num2 * num2;
			}
			eyeFlare.transform.localScale = new Vector3(num, num, num);
			eyeFlare.transform.rotation = rotation;
		}
	}

	private void Awake()
	{
		transform = base.transform;
	}
}
