using RoR2.Scripts.Utility;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace RoR2.UI;

[RequireComponent(typeof(MPEventSystemLocator))]
public class MPButton : Button
{
	private MPEventSystemLocator eventSystemLocator;

	public bool allowAllEventSystems;

	[FormerlySerializedAs("pointerClickOnly")]
	public bool submitOnPointerUp;

	public bool selectOnPointerEnter = true;

	public bool disablePointerClick;

	public bool disableGamepadClick;

	public bool useBaseGamepadClick;

	private static bool isIgnore;

	public UILayerKey requiredTopLayer;

	public UnityEvent onFindSelectableLeft;

	public UnityEvent onFindSelectableRight;

	public UnityEvent onSelect;

	public UnityEvent onDeselect;

	public bool defaultFallbackButton;

	private static bool autonavigationFindInProgress;

	private bool inPointerUp;

	public MPEventSystem eventSystem => eventSystemLocator.eventSystem;

	protected override void Awake()
	{
		base.Awake();
		eventSystemLocator = GetComponent<MPEventSystemLocator>();
	}

	public void Update()
	{
		if (eventSystem == null || eventSystem.player == null || isIgnore)
		{
			return;
		}
		if (!disableGamepadClick && eventSystem.player.GetButtonDown(14) && eventSystem.currentSelectedGameObject == base.gameObject && !useBaseGamepadClick)
		{
			InvokeClick();
		}
		if (!(eventSystem == null) && eventSystem.player != null && defaultFallbackButton && eventSystem.currentInputSource == MPEventSystem.InputSource.Gamepad && eventSystem.currentSelectedGameObject == null && CanBeSelected())
		{
			if (eventSystem == EventSystem.current)
			{
				Select();
			}
			else
			{
				eventSystem.SetSelectedGameObject(base.gameObject);
			}
		}
	}

	public void InvokeClick()
	{
		base.onClick?.Invoke();
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
			MPButton mPButton = selectable?.GetComponent<MPButton>();
			if ((bool)mPButton && !mPButton.CanBeSelected())
			{
				selectable = null;
			}
		}
		return selectable;
	}

	public override bool IsInteractable()
	{
		if (base.IsInteractable())
		{
			if (autonavigationFindInProgress)
			{
				return CanBeSelected();
			}
			return true;
		}
		return false;
	}

	public static void BlockMPButton()
	{
		isIgnore = true;
	}

	public static void UnblockMPButton()
	{
		isIgnore = false;
	}

	public bool CanBeSelected()
	{
		if (base.gameObject.activeInHierarchy)
		{
			if ((bool)requiredTopLayer)
			{
				return requiredTopLayer.representsTopLayer;
			}
			return true;
		}
		return false;
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

	public override void OnSelect(BaseEventData eventData)
	{
		onSelect?.Invoke();
		base.OnSelect(eventData);
	}

	public override void OnDeselect(BaseEventData eventData)
	{
		onDeselect?.Invoke();
		base.OnDeselect(eventData);
	}

	public override Selectable FindSelectableOnDown()
	{
		using (new ScopedReset<bool>(() => autonavigationFindInProgress, delegate(bool val)
		{
			autonavigationFindInProgress = val;
		}, tempVal: true))
		{
			return FilterSelectable(base.FindSelectableOnDown());
		}
	}

	public override Selectable FindSelectableOnLeft()
	{
		using (new ScopedReset<bool>(() => autonavigationFindInProgress, delegate(bool val)
		{
			autonavigationFindInProgress = val;
		}, tempVal: true))
		{
			onFindSelectableLeft?.Invoke();
			return FilterSelectable(base.FindSelectableOnLeft());
		}
	}

	public override Selectable FindSelectableOnRight()
	{
		using (new ScopedReset<bool>(() => autonavigationFindInProgress, delegate(bool val)
		{
			autonavigationFindInProgress = val;
		}, tempVal: true))
		{
			onFindSelectableRight?.Invoke();
			return FilterSelectable(base.FindSelectableOnRight());
		}
	}

	public override Selectable FindSelectableOnUp()
	{
		using (new ScopedReset<bool>(() => autonavigationFindInProgress, delegate(bool val)
		{
			autonavigationFindInProgress = val;
		}, tempVal: true))
		{
			return FilterSelectable(base.FindSelectableOnUp());
		}
	}

	public override void OnPointerEnter(PointerEventData eventData)
	{
		if (InputModuleIsAllowed(eventData.currentInputModule))
		{
			base.OnPointerEnter(eventData);
			if (selectOnPointerEnter)
			{
				AttemptSelection(eventData);
			}
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
			base.OnPointerExit(eventData);
		}
	}

	public override void OnPointerClick(PointerEventData eventData)
	{
		if (InputModuleIsAllowed(eventData.currentInputModule) && !disablePointerClick)
		{
			base.OnPointerClick(eventData);
		}
	}

	public override void OnPointerUp(PointerEventData eventData)
	{
		if (InputModuleIsAllowed(eventData.currentInputModule) && !disablePointerClick)
		{
			inPointerUp = true;
			base.OnPointerUp(eventData);
			inPointerUp = false;
		}
	}

	public override void OnPointerDown(PointerEventData eventData)
	{
		if (InputModuleIsAllowed(eventData.currentInputModule) && !disablePointerClick)
		{
			if (IsInteractable() && base.navigation.mode != 0)
			{
				AttemptSelection(eventData);
			}
			base.OnPointerDown(eventData);
		}
	}

	public override void OnSubmit(BaseEventData eventData)
	{
		if ((!submitOnPointerUp || inPointerUp) && !disableGamepadClick)
		{
			base.OnSubmit(eventData);
		}
	}

	public void SetFallback(bool fallbackButton)
	{
	}
}
