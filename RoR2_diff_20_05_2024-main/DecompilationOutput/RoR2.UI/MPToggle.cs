using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RoR2.UI;

[RequireComponent(typeof(MPEventSystemLocator))]
public class MPToggle : Toggle
{
	private MPControlHelper mpControlHelper;

	public bool allowAllEventSystems;

	public bool disablePointerClick;

	public Graphic hoverGraphic;

	private bool inPointerUp;

	protected override void Awake()
	{
		base.Awake();
		mpControlHelper = new MPControlHelper(this, GetComponent<MPEventSystemLocator>(), allowAllEventSystems, disablePointerClick);
	}

	private void LateUpdate()
	{
		if ((bool)hoverGraphic)
		{
			bool flag = base.currentSelectionState == SelectionState.Highlighted;
			if (hoverGraphic.enabled != flag)
			{
				hoverGraphic.enabled = flag;
			}
		}
	}

	public override void OnPointerEnter(PointerEventData eventData)
	{
		mpControlHelper.OnPointerEnter(eventData, base.OnPointerEnter);
	}

	public override void OnPointerExit(PointerEventData eventData)
	{
		mpControlHelper.OnPointerExit(eventData, base.OnPointerExit);
	}

	public override void OnPointerClick(PointerEventData eventData)
	{
		mpControlHelper.OnPointerClick(eventData, base.OnPointerClick);
	}

	public override void OnPointerUp(PointerEventData eventData)
	{
		mpControlHelper.OnPointerUp(eventData, base.OnPointerUp, ref inPointerUp);
	}

	public override void OnPointerDown(PointerEventData eventData)
	{
		mpControlHelper.OnPointerDown(eventData, base.OnPointerDown, ref inPointerUp);
	}

	public override Selectable FindSelectableOnDown()
	{
		return mpControlHelper.FindSelectableOnDown(base.FindSelectableOnDown);
	}

	public override Selectable FindSelectableOnLeft()
	{
		return mpControlHelper.FindSelectableOnLeft(base.FindSelectableOnLeft);
	}

	public override Selectable FindSelectableOnRight()
	{
		return mpControlHelper.FindSelectableOnRight(base.FindSelectableOnRight);
	}

	public override Selectable FindSelectableOnUp()
	{
		return mpControlHelper.FindSelectableOnUp(base.FindSelectableOnUp);
	}
}
