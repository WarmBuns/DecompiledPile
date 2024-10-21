using TMPro;
using UnityEngine;

namespace RoR2.UI;

public class HUDSpeedometer : MonoBehaviour
{
	public TextMeshProUGUI label;

	public TextMeshProUGUI fixedUpdateLabel;

	private Transform _targetTransform;

	private Vector3 lastTargetPosition;

	private Vector3 lastTargetPositionFixedUpdate;

	public Transform targetTransform
	{
		get
		{
			return _targetTransform;
		}
		set
		{
			if (!(_targetTransform == value))
			{
				_targetTransform = value;
				if ((bool)_targetTransform)
				{
					lastTargetPosition = _targetTransform.position;
					lastTargetPositionFixedUpdate = _targetTransform.position;
				}
			}
		}
	}

	private float EstimateSpeed(ref Vector3 oldPosition, Vector3 newPosition, float deltaTime)
	{
		float result = 0f;
		if (deltaTime != 0f)
		{
			result = (newPosition - oldPosition).magnitude / deltaTime;
		}
		oldPosition = newPosition;
		return result;
	}

	private void Update()
	{
		if ((bool)_targetTransform)
		{
			float num = EstimateSpeed(ref lastTargetPosition, _targetTransform.position, Time.deltaTime);
			label.text = $"{num:0.00} m/s";
		}
	}

	private void FixedUpdate()
	{
		if ((bool)_targetTransform)
		{
			float num = EstimateSpeed(ref lastTargetPositionFixedUpdate, _targetTransform.position, Time.deltaTime);
			fixedUpdateLabel.text = $"{num:0.00} m/s";
		}
	}
}
