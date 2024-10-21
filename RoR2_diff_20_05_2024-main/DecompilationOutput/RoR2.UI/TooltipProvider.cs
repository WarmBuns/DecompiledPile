using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RoR2.UI;

public class TooltipProvider : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
	public string titleToken = "";

	public Color titleColor = Color.clear;

	public string bodyToken = "";

	public Color bodyColor;

	public GameObject extraUIDisplayPrefab;

	public bool AllowTooltipOnNavigationSelect;

	public RequiredInspectStateForNavigationTooltip RequiredInspectStateForNavigationTooltip = RequiredInspectStateForNavigationTooltip.InspectOff | RequiredInspectStateForNavigationTooltip.InspectOn;

	public string overrideTitleText = "";

	public string overrideBodyText = "";

	public bool disableTitleRichText;

	public bool disableBodyRichText;

	[NonSerialized]
	public int userCount;

	private static readonly Color playerColor = new Color32(242, 65, 65, byte.MaxValue);

	private bool tooltipIsAvailable => titleColor != Color.clear;

	public string titleText
	{
		get
		{
			if (!string.IsNullOrEmpty(overrideTitleText))
			{
				return overrideTitleText;
			}
			if (titleToken == null)
			{
				return null;
			}
			return Language.GetString(titleToken);
		}
	}

	public string bodyText
	{
		get
		{
			if (!string.IsNullOrEmpty(overrideBodyText))
			{
				return overrideBodyText;
			}
			if (bodyToken == null)
			{
				return null;
			}
			return Language.GetString(bodyToken);
		}
	}

	private bool InspectState => (EventSystem.current as MPEventSystem)?.localUser?.userProfile?.useInspectFeature == true;

	private bool CanShowNavigationTooltip
	{
		get
		{
			if ((RequiredInspectStateForNavigationTooltip & RequiredInspectStateForNavigationTooltip.InspectOn) > (RequiredInspectStateForNavigationTooltip)0 && InspectState)
			{
				return true;
			}
			if ((RequiredInspectStateForNavigationTooltip & RequiredInspectStateForNavigationTooltip.InspectOff) > (RequiredInspectStateForNavigationTooltip)0 && !InspectState)
			{
				return true;
			}
			return false;
		}
	}

	public void SetContent(TooltipContent tooltipContent)
	{
		titleToken = tooltipContent.titleToken;
		overrideTitleText = tooltipContent.overrideTitleText;
		titleColor = tooltipContent.titleColor;
		bodyToken = tooltipContent.bodyToken;
		overrideBodyText = tooltipContent.overrideBodyText;
		bodyColor = tooltipContent.bodyColor;
		disableTitleRichText = tooltipContent.disableTitleRichText;
		disableBodyRichText = tooltipContent.disableBodyRichText;
	}

	private void OnDisable()
	{
		TooltipController.RemoveTooltip(this);
	}

	void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
	{
		MPEventSystem mPEventSystem = EventSystem.current as MPEventSystem;
		if (mPEventSystem != null && tooltipIsAvailable)
		{
			TooltipController.SetTooltip(mPEventSystem, this, eventData.position);
		}
	}

	void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
	{
		MPEventSystem mPEventSystem = EventSystem.current as MPEventSystem;
		if (mPEventSystem != null && tooltipIsAvailable)
		{
			TooltipController.RemoveTooltip(mPEventSystem, this);
		}
	}

	public void OnSelect(BaseEventData eventData)
	{
		MPEventSystem mPEventSystem = EventSystem.current as MPEventSystem;
		if (AllowTooltipOnNavigationSelect && CanShowNavigationTooltip && mPEventSystem != null && tooltipIsAvailable)
		{
			RectTransform rectTransform = base.transform as RectTransform;
			TooltipController.SetTooltip(mPEventSystem, this, rectTransform.position, rectTransform);
		}
	}

	public void OnDeselect(BaseEventData eventData)
	{
		MPEventSystem mPEventSystem = EventSystem.current as MPEventSystem;
		if (AllowTooltipOnNavigationSelect && CanShowNavigationTooltip && mPEventSystem != null && tooltipIsAvailable)
		{
			TooltipController.RemoveTooltip(mPEventSystem, this);
		}
	}

	public static TooltipContent GetPlayerNameTooltipContent(string userName)
	{
		string stringFormatted = Language.GetStringFormatted("PLAYER_NAME_TOOLTIP_FORMAT", userName);
		TooltipContent result = default(TooltipContent);
		result.overrideTitleText = stringFormatted;
		result.disableTitleRichText = true;
		result.titleColor = playerColor;
		return result;
	}
}
