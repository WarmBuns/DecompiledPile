using UnityEngine;
using UnityEngine.UI;

namespace RoR2;

public class ShrinkWrapLayoutElement : MonoBehaviour, ILayoutElement
{
	public float minWidth { get; private set; }

	public float preferredWidth { get; private set; }

	public float flexibleWidth { get; private set; }

	public float minHeight { get; private set; }

	public float preferredHeight { get; private set; }

	public float flexibleHeight { get; private set; }

	public int layoutPriority { get; private set; }

	public void CalculateLayoutInputHorizontal()
	{
		preferredWidth = 0f;
		if (base.transform.childCount != 0)
		{
			Rect rect = ((RectTransform)base.transform.GetChild(0)).rect;
			int i = 1;
			for (int childCount = base.transform.childCount; i < childCount; i++)
			{
				Rect rect2 = ((RectTransform)base.transform.GetChild(i)).rect;
				rect.xMin = Mathf.Min(rect.xMin, rect2.xMin);
				rect.xMax = Mathf.Max(rect.xMax, rect2.xMax);
			}
			minWidth = rect.width;
			preferredWidth = rect.width;
			flexibleWidth = 0f;
		}
	}

	public void CalculateLayoutInputVertical()
	{
		preferredHeight = 0f;
		if (base.transform.childCount != 0)
		{
			Rect rect = ((RectTransform)base.transform.GetChild(0)).rect;
			int i = 1;
			for (int childCount = base.transform.childCount; i < childCount; i++)
			{
				Rect rect2 = ((RectTransform)base.transform.GetChild(i)).rect;
				rect.yMin = Mathf.Min(rect.yMin, rect2.yMin);
				rect.yMax = Mathf.Max(rect.yMax, rect2.yMax);
			}
			minHeight = rect.height;
			preferredHeight = rect.height;
			flexibleHeight = 0f;
		}
	}
}
