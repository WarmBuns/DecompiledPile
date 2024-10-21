using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RoR2.UI;

[RequireComponent(typeof(MPEventSystemLocator))]
public class MPScrollbar : Scrollbar
{
	public bool allowAllEventSystems;

	private MPEventSystemLocator eventSystemLocator;

	private RectTransform viewPortRectTransform;

	private EventSystem eventSystem => eventSystemLocator.eventSystem;

	protected override void Awake()
	{
		base.Awake();
		eventSystemLocator = GetComponent<MPEventSystemLocator>();
	}

	private Selectable FilterSelectable(Selectable selectable)
	{
		if ((bool)selectable)
		{
			MPEventSystemLocator component = selectable.GetComponent<MPEventSystemLocator>();
			if (!component || component.eventSystem != eventSystemLocator.eventSystem)
			{
				selectable = null;
			}
		}
		return selectable;
	}

	public bool InputModuleIsAllowed(BaseInputModule inputModule)
	{
		if (allowAllEventSystems)
		{
			return true;
		}
		EventSystem eventSystem = this.eventSystem;
		if ((bool)eventSystem)
		{
			return inputModule == eventSystem.currentInputModule;
		}
		return false;
	}

	public override Selectable FindSelectableOnDown()
	{
		return FilterSelectable(base.FindSelectableOnDown());
	}

	public override Selectable FindSelectableOnLeft()
	{
		return FilterSelectable(base.FindSelectableOnLeft());
	}

	public override Selectable FindSelectableOnRight()
	{
		return FilterSelectable(base.FindSelectableOnRight());
	}

	public override Selectable FindSelectableOnUp()
	{
		return FilterSelectable(base.FindSelectableOnUp());
	}

	public override void OnPointerUp(PointerEventData eventData)
	{
		if (InputModuleIsAllowed(eventData.currentInputModule))
		{
			if ((bool)eventSystem && base.gameObject == eventSystem.currentSelectedGameObject)
			{
				base.enabled = false;
				base.enabled = true;
			}
			base.OnPointerUp(eventData);
		}
	}

	public override void OnSelect(BaseEventData eventData)
	{
		if (InputModuleIsAllowed(eventData.currentInputModule))
		{
			base.OnSelect(eventData);
		}
	}

	public override void OnDeselect(BaseEventData eventData)
	{
		if (InputModuleIsAllowed(eventData.currentInputModule))
		{
			base.OnDeselect(eventData);
		}
	}

	public override void OnPointerEnter(PointerEventData eventData)
	{
		if (InputModuleIsAllowed(eventData.currentInputModule))
		{
			base.OnPointerEnter(eventData);
		}
	}

	public override void OnPointerExit(PointerEventData eventData)
	{
		if (InputModuleIsAllowed(eventData.currentInputModule))
		{
			base.OnPointerExit(eventData);
		}
	}

	public override void OnPointerDown(PointerEventData eventData)
	{
		if (eventData.button == PointerEventData.InputButton.Left)
		{
			if (IsInteractable() && base.navigation.mode != 0)
			{
				eventSystem.SetSelectedGameObject(base.gameObject, eventData);
			}
			base.OnPointerDown(eventData);
		}
	}

	public override void Select()
	{
		if (!eventSystem.alreadySelecting)
		{
			eventSystem.SetSelectedGameObject(base.gameObject);
		}
	}
}
