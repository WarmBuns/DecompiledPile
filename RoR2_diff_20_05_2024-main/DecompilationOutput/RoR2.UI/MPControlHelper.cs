using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RoR2.UI;

public struct MPControlHelper
{
	private readonly MPEventSystemLocator eventSystemLocator;

	private readonly Selectable owner;

	private readonly GameObject gameObject;

	private readonly bool allowAllEventSystems;

	private readonly bool disablePointerClick;

	public MPEventSystem eventSystem => eventSystemLocator.eventSystem;

	public MPControlHelper(Selectable owner, MPEventSystemLocator eventSystemLocator, bool allowAllEventSystems, bool disablePointerClick)
	{
		this.owner = owner;
		this.eventSystemLocator = eventSystemLocator;
		gameObject = this.owner.gameObject;
		this.allowAllEventSystems = allowAllEventSystems;
		this.disablePointerClick = disablePointerClick;
	}

	public Selectable FilterSelectable(Selectable selectable)
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

	private void AttemptSelection(PointerEventData eventData)
	{
		if ((bool)eventSystem && eventSystem.currentInputModule == eventData.currentInputModule)
		{
			eventSystem.SetSelectedGameObject(gameObject, eventData);
		}
	}

	public Selectable FindSelectableOnDown(Func<Selectable> baseMethod)
	{
		return FilterSelectable(baseMethod());
	}

	public Selectable FindSelectableOnLeft(Func<Selectable> baseMethod)
	{
		return FilterSelectable(baseMethod());
	}

	public Selectable FindSelectableOnRight(Func<Selectable> baseMethod)
	{
		return FilterSelectable(baseMethod());
	}

	public Selectable FindSelectableOnUp(Func<Selectable> baseMethod)
	{
		return FilterSelectable(baseMethod());
	}

	public void OnPointerEnter(PointerEventData eventData, Action<PointerEventData> baseMethod)
	{
		if (InputModuleIsAllowed(eventData.currentInputModule))
		{
			baseMethod(eventData);
			AttemptSelection(eventData);
		}
	}

	public void OnPointerExit(PointerEventData eventData, Action<PointerEventData> baseMethod)
	{
		if (InputModuleIsAllowed(eventData.currentInputModule))
		{
			if ((bool)eventSystem && owner.gameObject == eventSystem.currentSelectedGameObject)
			{
				owner.enabled = false;
				owner.enabled = true;
			}
			baseMethod(eventData);
		}
	}

	public void OnPointerClick(PointerEventData eventData, Action<PointerEventData> baseMethod)
	{
		if (InputModuleIsAllowed(eventData.currentInputModule) && !disablePointerClick)
		{
			baseMethod(eventData);
		}
	}

	public void OnPointerUp(PointerEventData eventData, Action<PointerEventData> baseMethod, ref bool inPointerUp)
	{
		if (InputModuleIsAllowed(eventData.currentInputModule) && !disablePointerClick)
		{
			inPointerUp = true;
			baseMethod(eventData);
			inPointerUp = false;
		}
	}

	public void OnPointerDown(PointerEventData eventData, Action<PointerEventData> baseMethod, ref bool inPointerUp)
	{
		if (InputModuleIsAllowed(eventData.currentInputModule) && !disablePointerClick)
		{
			if (owner.IsInteractable() && owner.navigation.mode != 0)
			{
				AttemptSelection(eventData);
			}
			baseMethod(eventData);
		}
	}

	public void OnSubmit(BaseEventData eventData, Action<BaseEventData> baseMethod, bool submitOnPointerUp, ref bool inPointerUp)
	{
		if (!submitOnPointerUp || inPointerUp)
		{
			baseMethod(eventData);
		}
	}
}
