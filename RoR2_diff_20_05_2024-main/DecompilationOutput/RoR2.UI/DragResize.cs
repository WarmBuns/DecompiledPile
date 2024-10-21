using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RoR2.UI;

[RequireComponent(typeof(RectTransform))]
public class DragResize : MonoBehaviour, IBeginDragHandler, IEventSystemHandler, IDragHandler
{
	public RectTransform targetTransform;

	public Vector2 minSize;

	private Vector2 grabPoint;

	private RectTransform rectTransform;

	private void Awake()
	{
		rectTransform = (RectTransform)base.transform;
	}

	public void OnDrag(PointerEventData eventData)
	{
		UpdateDrag(eventData);
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		if ((bool)targetTransform)
		{
			RectTransformUtility.ScreenPointToLocalPointInRectangle(targetTransform, eventData.position, eventData.pressEventCamera, out grabPoint);
		}
	}

	private void UpdateDrag(PointerEventData eventData)
	{
		if (eventData.button == PointerEventData.InputButton.Left && (bool)targetTransform)
		{
			RectTransformUtility.ScreenPointToLocalPointInRectangle(targetTransform, eventData.position, eventData.pressEventCamera, out var localPoint);
			Vector2 vector = localPoint - grabPoint;
			grabPoint = localPoint;
			vector.y = 0f - vector.y;
			minSize = Vector2.Max(rhs: new Vector2(LayoutUtility.GetMinSize(targetTransform, 0), LayoutUtility.GetMinSize(targetTransform, 1)), lhs: minSize);
			targetTransform.sizeDelta = Vector2.Max(targetTransform.sizeDelta + vector, minSize);
		}
	}
}
