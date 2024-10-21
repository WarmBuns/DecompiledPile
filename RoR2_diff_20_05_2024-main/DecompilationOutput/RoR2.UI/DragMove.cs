using UnityEngine;
using UnityEngine.EventSystems;

namespace RoR2.UI;

[RequireComponent(typeof(RectTransform))]
public class DragMove : MonoBehaviour, IBeginDragHandler, IEventSystemHandler, IDragHandler
{
	public RectTransform targetTransform;

	private Vector2 grabPoint;

	private RectTransform rectTransform;

	private void OnAwake()
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
			targetTransform.localPosition += new Vector3(vector.x, vector.y, 0f);
		}
	}
}
