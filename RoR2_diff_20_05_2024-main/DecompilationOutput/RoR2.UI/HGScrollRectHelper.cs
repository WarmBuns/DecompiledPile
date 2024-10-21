using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI;

[RequireComponent(typeof(MPEventSystemLocator))]
[RequireComponent(typeof(ScrollRect))]
public class HGScrollRectHelper : MonoBehaviour
{
	public float spacingPerElement;

	public bool allowVerticalScrollingWithGamepadSticks;

	public float stickScale;

	public UILayerKey requiredTopLayer;

	public MPDropdown dropDown;

	private ScrollRect scrollRect;

	private readonly List<Toggle> dropDownOptions = new List<Toggle>();

	private MPEventSystemLocator eventSystemLocator;

	private float idealVerticalNormalizedPosition;

	private float scrollbarVelocity;

	private const float scrollbarSmoothdampTime = 0.1f;

	private bool hasInitialized;

	private void Start()
	{
		Initialize();
	}

	private void Initialize()
	{
		if (!hasInitialized)
		{
			hasInitialized = true;
			scrollRect = GetComponent<ScrollRect>();
			eventSystemLocator = GetComponent<MPEventSystemLocator>();
			idealVerticalNormalizedPosition = scrollRect.verticalNormalizedPosition;
			if (!eventSystemLocator.eventSystem)
			{
				eventSystemLocator.eventSystem = MPEventSystemManager.primaryEventSystem;
			}
			if ((bool)dropDown)
			{
				scrollRect.content.GetComponentsInChildren(dropDownOptions);
			}
		}
	}

	private bool GamepadIsCurrentInputSource()
	{
		if (!hasInitialized)
		{
			return false;
		}
		if ((bool)eventSystemLocator.eventSystem)
		{
			return eventSystemLocator.eventSystem.currentInputSource == MPEventSystem.InputSource.Gamepad;
		}
		return false;
	}

	private bool CanAcceptInput()
	{
		if ((bool)requiredTopLayer)
		{
			return requiredTopLayer.representsTopLayer;
		}
		return true;
	}

	private void Update()
	{
		Initialize();
		if (!GamepadIsCurrentInputSource())
		{
			return;
		}
		if ((bool)eventSystemLocator && (bool)eventSystemLocator.eventSystem && CanAcceptInput())
		{
			float height = scrollRect.content.rect.height;
			float axis = eventSystemLocator.eventSystem.player.GetAxis(13);
			eventSystemLocator.eventSystem.player.GetAxis(12);
			if (allowVerticalScrollingWithGamepadSticks)
			{
				idealVerticalNormalizedPosition = Mathf.Clamp01(idealVerticalNormalizedPosition + axis * stickScale * Time.unscaledDeltaTime / height);
				scrollRect.verticalNormalizedPosition = idealVerticalNormalizedPosition;
				if ((bool)dropDown && dropDownOptions.Count > 0)
				{
					GamepadAutoScroll();
				}
			}
		}
		scrollRect.verticalNormalizedPosition = Mathf.SmoothDamp(scrollRect.verticalNormalizedPosition, idealVerticalNormalizedPosition, ref scrollbarVelocity, 0.1f, float.PositiveInfinity, Time.unscaledDeltaTime);
	}

	private void GamepadAutoScroll()
	{
		int num = -1;
		Toggle toggle = (eventSystemLocator.eventSystem.currentSelectedGameObject ? eventSystemLocator.eventSystem.currentSelectedGameObject.GetComponent<Toggle>() : null);
		if ((bool)toggle)
		{
			num = dropDownOptions.IndexOf(toggle);
		}
		if (num > -1)
		{
			idealVerticalNormalizedPosition = 1f - (float)num / (float)dropDownOptions.Count;
		}
	}

	public void ScrollToShowMe(MPButton mpButton)
	{
		Initialize();
		if (!GamepadIsCurrentInputSource() || !CanAcceptInput())
		{
			return;
		}
		Canvas.ForceUpdateCanvases();
		_ = scrollRect.content;
		RectTransform component = mpButton.GetComponent<RectTransform>();
		float num = (0f - scrollRect.viewport.rect.size.y) / 2f;
		float num2 = 0f - scrollRect.content.rect.size.y;
		float num3 = 0f;
		float num4 = 0f;
		SettingSubsectionHelper component2 = mpButton.transform.parent.GetComponent<SettingSubsectionHelper>();
		if ((object)component2 != null)
		{
			RectTransform component3 = component2.GetComponent<RectTransform>();
			num4 = component3.anchoredPosition.y + component3.sizeDelta.y * 0.5f;
		}
		float num5 = component.anchoredPosition.y + num4 - spacingPerElement;
		if (num5 > num3 + num)
		{
			idealVerticalNormalizedPosition = 1f;
		}
		else if (!(num5 > num))
		{
			if (num5 < num2 - num)
			{
				idealVerticalNormalizedPosition = 0f;
				return;
			}
			float num6 = Mathf.InverseLerp(num, num2 - num, num5);
			float num7 = 1f - num6;
			idealVerticalNormalizedPosition = num7;
		}
	}

	private static Rect GetWorldspaceRect(Vector3 pos, Rect rect, Vector2 offset)
	{
		float x = pos.x + rect.xMin - offset.x;
		float y = pos.y + rect.yMin - offset.y;
		return new Rect(new Vector2(x, y), rect.size);
	}
}
