using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI;

public class TooltipController : MonoBehaviour
{
	private static readonly List<TooltipController> instancesList = new List<TooltipController>();

	[NonSerialized]
	public MPEventSystem owner;

	private bool _isGamepadTooltip;

	private Vector2 _gamepadTooltipPosition;

	public RectTransform tooltipCenterTransform;

	public RectTransform tooltipFlipTransform;

	public Image colorHighlightImage;

	public TextMeshProUGUI titleLabel;

	public TextMeshProUGUI bodyLabel;

	public Transform extraUIPos;

	private UICamera uiCamera;

	private void SetTooltipProvider(TooltipProvider provider)
	{
		titleLabel.text = provider.titleText;
		titleLabel.richText = !provider.disableTitleRichText;
		bodyLabel.text = provider.bodyText;
		bodyLabel.richText = !provider.disableBodyRichText;
		colorHighlightImage.color = provider.titleColor;
		if (provider.extraUIDisplayPrefab != null)
		{
			UnityEngine.Object.Instantiate(provider.extraUIDisplayPrefab, extraUIPos).transform.SetSiblingIndex(bodyLabel.transform.parent.GetSiblingIndex() + 1);
		}
	}

	private static UICamera FindUICamera(MPEventSystem mpEventSystem)
	{
		foreach (UICamera readOnlyInstances in UICamera.readOnlyInstancesList)
		{
			if (readOnlyInstances.GetAssociatedEventSystem() as MPEventSystem == mpEventSystem)
			{
				return readOnlyInstances;
			}
		}
		return null;
	}

	private void Awake()
	{
		instancesList.Add(this);
	}

	private void OnDestroy()
	{
		instancesList.Remove(this);
	}

	private void LateUpdate()
	{
		if ((bool)owner)
		{
			Vector2 position;
			if (_isGamepadTooltip)
			{
				tooltipCenterTransform.position = _gamepadTooltipPosition;
			}
			else if (owner.GetCursorPosition(out position))
			{
				tooltipCenterTransform.position = position;
			}
		}
	}

	public static void RemoveTooltip(TooltipProvider tooltipProvider)
	{
		if (tooltipProvider.userCount <= 0)
		{
			return;
		}
		foreach (MPEventSystem readOnlyInstances in MPEventSystem.readOnlyInstancesList)
		{
			RemoveTooltip(readOnlyInstances, tooltipProvider);
		}
	}

	public static void RemoveTooltip(MPEventSystem eventSystem, TooltipProvider tooltipProvider)
	{
		if (eventSystem.currentTooltipProvider == tooltipProvider)
		{
			SetTooltip(eventSystem, null, Vector3.zero);
		}
	}

	public static void SetTooltip(MPEventSystem eventSystem, TooltipProvider newTooltipProvider, Vector2 tooltipPosition, RectTransform rectTransformToDisplayAt = null)
	{
		if (eventSystem.currentTooltipProvider != newTooltipProvider)
		{
			if ((bool)eventSystem.currentTooltip)
			{
				UnityEngine.Object.Destroy(eventSystem.currentTooltip.gameObject);
				eventSystem.currentTooltip = null;
			}
			if ((bool)eventSystem.currentTooltipProvider)
			{
				eventSystem.currentTooltipProvider.userCount--;
			}
			eventSystem.currentTooltipProvider = newTooltipProvider;
			if ((bool)newTooltipProvider && (!string.IsNullOrEmpty(newTooltipProvider.titleText) || !string.IsNullOrEmpty(newTooltipProvider.bodyText)))
			{
				newTooltipProvider.userCount++;
				GameObject gameObject = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/UI/Tooltip"));
				eventSystem.currentTooltip = gameObject.GetComponent<TooltipController>();
				eventSystem.currentTooltip.owner = eventSystem;
				eventSystem.currentTooltip.uiCamera = FindUICamera(eventSystem);
				eventSystem.currentTooltip.SetTooltipProvider(eventSystem.currentTooltipProvider);
				gameObject.GetComponent<Canvas>().worldCamera = eventSystem.currentTooltip.uiCamera?.camera;
			}
		}
		if (!eventSystem.currentTooltip)
		{
			return;
		}
		Vector2 vector = Vector2.zero;
		UICamera uICamera = eventSystem.currentTooltip.uiCamera;
		Camera camera = Camera.main;
		if ((bool)uICamera)
		{
			camera = uICamera.camera;
		}
		if ((bool)camera)
		{
			Vector2 gamepadTooltipPosition = tooltipPosition;
			if (rectTransformToDisplayAt != null)
			{
				Camera worldCamera = rectTransformToDisplayAt.gameObject.GetComponentInParent<Canvas>().worldCamera;
				Vector3 position = rectTransformToDisplayAt.TransformPoint(rectTransformToDisplayAt.rect.center);
				gamepadTooltipPosition = worldCamera.WorldToScreenPoint(position);
				eventSystem.currentTooltip._gamepadTooltipPosition = gamepadTooltipPosition;
			}
			Vector3 vector2 = camera.ScreenToViewportPoint(new Vector3(gamepadTooltipPosition.x, gamepadTooltipPosition.y, 0f));
			vector = new Vector2(vector2.x, vector2.y);
		}
		Vector2 vector3 = new Vector2(0f, 0f);
		vector3.x = ((vector.x > 0.5f) ? 1f : 0f);
		vector3.y = ((vector.y > 0.5f) ? 1f : 0f);
		eventSystem.currentTooltip.tooltipFlipTransform.anchorMin = vector3;
		eventSystem.currentTooltip.tooltipFlipTransform.anchorMax = vector3;
		eventSystem.currentTooltip.tooltipFlipTransform.pivot = vector3;
		eventSystem.currentTooltip._isGamepadTooltip = rectTransformToDisplayAt != null;
	}
}
