using System;
using UnityEngine;

namespace RoR2.UI;

public class RectTransformDimensionsChangeEvent : MonoBehaviour
{
	public event Action onRectTransformDimensionsChange;

	private void OnRectTransformDimensionsChange()
	{
		if (this.onRectTransformDimensionsChange != null)
		{
			this.onRectTransformDimensionsChange();
		}
	}
}
