using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace RoR2.UI;

[RequireComponent(typeof(PositionIndicator))]
public class DistanceIndicator : MonoBehaviour
{
	public PositionIndicator positionIndicator;

	public TextMeshPro tmp;

	private static readonly List<DistanceIndicator> instancesList;

	private void OnEnable()
	{
		instancesList.Add(this);
	}

	private void OnDisable()
	{
		instancesList.Remove(this);
	}

	static DistanceIndicator()
	{
		instancesList = new List<DistanceIndicator>();
		UICamera.onUICameraPreCull += UpdateText;
	}

	private static void UpdateText(UICamera uiCamera)
	{
		CameraRigController cameraRigController = uiCamera.cameraRigController;
		Transform transform = null;
		if ((bool)cameraRigController && (bool)cameraRigController.target)
		{
			CharacterBody component = cameraRigController.target.GetComponent<CharacterBody>();
			transform = ((!component) ? cameraRigController.target.transform : component.coreTransform);
		}
		if ((bool)transform)
		{
			for (int i = 0; i < instancesList.Count; i++)
			{
				DistanceIndicator distanceIndicator = instancesList[i];
				string text = (distanceIndicator.positionIndicator.targetTransform.position - transform.position).magnitude.ToString("0.0") + "m";
				distanceIndicator.tmp.text = text;
			}
		}
	}
}
