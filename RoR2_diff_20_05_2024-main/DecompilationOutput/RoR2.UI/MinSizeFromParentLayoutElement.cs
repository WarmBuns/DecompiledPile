using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI;

[RequireComponent(typeof(RectTransform))]
public class MinSizeFromParentLayoutElement : MonoBehaviour, ILayoutElement
{
	public bool useParentWidthAsMinWidth = true;

	public bool useParentHeightAsMinHeight = true;

	[SerializeField]
	private int _layoutPriority = 1;

	public float minWidth { get; protected set; } = -1f;

	public float preferredWidth { get; } = -1f;

	public float flexibleWidth { get; } = -1f;

	public float minHeight { get; protected set; } = -1f;

	public float preferredHeight { get; } = -1f;

	public float flexibleHeight { get; } = -1f;

	public int layoutPriority
	{
		get
		{
			return _layoutPriority;
		}
		set
		{
			_layoutPriority = value;
		}
	}

	public void CalculateLayoutInputHorizontal()
	{
		if (!useParentWidthAsMinWidth)
		{
			minWidth = -1f;
			return;
		}
		RectTransform rectTransform = base.transform.parent as RectTransform;
		minWidth = (rectTransform ? rectTransform.rect.width : (-1f));
	}

	public void CalculateLayoutInputVertical()
	{
		if (!useParentHeightAsMinHeight)
		{
			minHeight = -1f;
			return;
		}
		RectTransform rectTransform = base.transform.parent as RectTransform;
		minHeight = (rectTransform ? rectTransform.rect.height : (-1f));
	}
}
