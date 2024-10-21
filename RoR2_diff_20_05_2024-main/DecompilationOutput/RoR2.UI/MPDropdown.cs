using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RoR2.UI;

[RequireComponent(typeof(MPEventSystemLocator))]
public class MPDropdown : TMP_Dropdown
{
	private MPEventSystemLocator eventSystemLocator;

	protected bool isPointerInside;

	public bool allowAllEventSystems;

	private SelectionState previousState = SelectionState.Disabled;

	protected MPEventSystem eventSystem => eventSystemLocator.eventSystem;

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

	private void AttemptSelection(PointerEventData eventData)
	{
		if ((bool)eventSystem && eventSystem.currentInputModule == eventData.currentInputModule)
		{
			eventSystem.SetSelectedGameObject(base.gameObject, eventData);
		}
	}

	public override void OnPointerEnter(PointerEventData eventData)
	{
		if (InputModuleIsAllowed(eventData.currentInputModule))
		{
			isPointerInside = true;
			base.OnPointerEnter(eventData);
			AttemptSelection(eventData);
		}
	}

	public override void OnPointerExit(PointerEventData eventData)
	{
		if (InputModuleIsAllowed(eventData.currentInputModule))
		{
			if ((bool)eventSystem && base.gameObject == eventSystem.currentSelectedGameObject)
			{
				base.enabled = false;
				base.enabled = true;
			}
			isPointerInside = false;
			base.OnPointerExit(eventData);
		}
	}

	public override void OnPointerClick(PointerEventData eventData)
	{
		if (InputModuleIsAllowed(eventData.currentInputModule))
		{
			base.OnPointerClick(eventData);
		}
	}

	public override void OnPointerUp(PointerEventData eventData)
	{
		if (InputModuleIsAllowed(eventData.currentInputModule))
		{
			base.OnPointerUp(eventData);
		}
	}

	public override void OnPointerDown(PointerEventData eventData)
	{
		if (InputModuleIsAllowed(eventData.currentInputModule))
		{
			if (IsInteractable() && base.navigation.mode != 0)
			{
				AttemptSelection(eventData);
			}
			base.OnPointerDown(eventData);
		}
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		isPointerInside = false;
	}

	protected override void DoStateTransition(SelectionState state, bool instant)
	{
		base.DoStateTransition(state, instant);
		if (previousState != state)
		{
			if (state == SelectionState.Highlighted)
			{
				Util.PlaySound("Play_UI_menuHover", RoR2Application.instance.gameObject);
			}
			previousState = state;
		}
	}
}
