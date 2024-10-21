using System;
using UnityEngine;

namespace RoR2.UI;

public class PickerPanelSizeAdjuster : MonoBehaviour
{
	[SerializeField]
	private PickupPickerPanel pickerPanel;

	[SerializeField]
	private RectTransform rectToAdjust;

	[SerializeField]
	[Tooltip("How many rows can we have before we begin to adjust")]
	private int minRows = 5;

	[SerializeField]
	[Tooltip("After this many rows, we will not adjust the size any higher")]
	private int maxRows = 7;

	[SerializeField]
	[Tooltip("How much should we adjust the window for each new row")]
	private float adjustmentPerRow;

	[SerializeField]
	[Tooltip("This will be set automatically")]
	private float objectHeight;

	private bool capturedDefaults;

	private void Awake()
	{
		PickupPickerPanel pickupPickerPanel = pickerPanel;
		pickupPickerPanel.OnPanelSetupComplete = (Action)Delegate.Combine(pickupPickerPanel.OnPanelSetupComplete, new Action(AdjustPickerPanelSize));
	}

	private void CaptureDefaults()
	{
		if (capturedDefaults)
		{
			return;
		}
		RectTransform buttonContainer = pickerPanel.buttonContainer;
		if (buttonContainer != null && buttonContainer.childCount > 0)
		{
			RectTransform component = buttonContainer.GetChild(0).GetComponent<RectTransform>();
			if (component != null)
			{
				objectHeight = component.rect.height;
			}
		}
		capturedDefaults = true;
	}

	private void AdjustPickerPanelSize()
	{
		int num = Mathf.Max(1, pickerPanel.maxColumnCount);
		int value = pickerPanel.buttonContainer.childCount / num;
		value = Mathf.Clamp(value, minRows, maxRows) - minRows;
		rectToAdjust.ForceUpdateRectTransforms();
		CaptureDefaults();
		if (value > 0)
		{
			float num2 = adjustmentPerRow / 2f * (float)value;
			Vector2 offsetMin = rectToAdjust.offsetMin;
			rectToAdjust.offsetMin = new Vector2(offsetMin.x, offsetMin.y - num2);
			offsetMin = rectToAdjust.offsetMax;
			rectToAdjust.offsetMax = new Vector2(offsetMin.x, offsetMin.y + num2);
		}
	}
}
