using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RoR2.UI;

[ExecuteAlways]
[RequireComponent(typeof(LayoutElement))]
[RequireComponent(typeof(RectTransform))]
public class ColumnLayoutGroupElement : MonoBehaviour, IBeginDragHandler, IEventSystemHandler, IDragHandler
{
	private enum ClickLocation
	{
		None,
		Middle,
		RightHandle,
		LeftHandle
	}

	private RectTransform rectTransform;

	private LayoutElement layoutElement;

	public RectTransform rectTransformToLayoutInvalidate;

	private float handleWidth = 4f;

	private ClickLocation lastClickLocation;

	private void Awake()
	{
		rectTransform = (RectTransform)base.transform;
		layoutElement = GetComponent<LayoutElement>();
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		ClickLocation clickLocation = ClickLocation.None;
		if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out var localPoint))
		{
			Rect rect = rectTransform.rect;
			float width = rect.width;
			Vector2 vector = new Vector2(localPoint.x * rect.width, localPoint.y * rect.height);
			clickLocation = ClickLocation.Middle;
			if (vector.x < handleWidth)
			{
				clickLocation = ClickLocation.LeftHandle;
			}
			if (vector.x > width - handleWidth)
			{
				clickLocation = ClickLocation.RightHandle;
			}
		}
		lastClickLocation = clickLocation;
	}

	public void OnDrag(PointerEventData eventData)
	{
		Transform parent = rectTransform.parent;
		int siblingIndex = rectTransform.GetSiblingIndex();
		if (lastClickLocation == ClickLocation.LeftHandle && siblingIndex != 0)
		{
			AdjustWidth(parent.GetChild(siblingIndex - 1)?.GetComponent<LayoutElement>(), layoutElement, eventData.delta.x);
		}
		if (lastClickLocation == ClickLocation.RightHandle && siblingIndex != parent.childCount - 1)
		{
			AdjustWidth(layoutElement, parent.GetChild(siblingIndex + 1)?.GetComponent<LayoutElement>(), eventData.delta.x);
		}
		void AdjustWidth(LayoutElement lhs, LayoutElement rhs, float change)
		{
			if ((bool)lhs && (bool)rhs)
			{
				if (lhs.preferredWidth + change < lhs.minWidth)
				{
					change = lhs.minWidth - lhs.preferredWidth;
				}
				if (rhs.preferredWidth - change < rhs.minWidth)
				{
					change = rhs.preferredWidth - rhs.minWidth;
				}
				if (change != 0f)
				{
					lhs.preferredWidth += change;
					rhs.preferredWidth -= change;
					if ((bool)rectTransformToLayoutInvalidate)
					{
						LayoutRebuilder.MarkLayoutForRebuild(rectTransformToLayoutInvalidate);
					}
				}
			}
		}
	}
}
