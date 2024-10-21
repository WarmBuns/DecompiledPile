using System.Globalization;
using RoR2.ExpansionManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RoR2.UI;

[RequireComponent(typeof(HGButton))]
public class SurvivorIconController : MonoBehaviour
{
	public EclipseRunScreenController eclipseRunScreenController;

	private SurvivorIndex _survivorIndex = SurvivorIndex.None;

	private SurvivorDef _survivorDef;

	private BodyIndex survivorBodyIndex = BodyIndex.None;

	private CharacterBody survivorBodyPrefabBodyComponent;

	private bool survivorIsUnlocked;

	private bool survivorRequiredExpansionEnabled;

	private bool survivorRequiredEntitlementAvailable;

	private bool shouldRebuild = true;

	private bool isCurrentChoice;

	public RawImage survivorIcon;

	public GameObject survivorIsSelectedEffect;

	public ViewableTag viewableTag;

	public ViewableTag loadoutViewableTag;

	public ViewableTrigger viewableTrigger;

	public TooltipProvider tooltipProvider;

	public CharacterSelectBarController characterSelectBarController { get; set; }

	public SurvivorIndex survivorIndex
	{
		get
		{
			return _survivorIndex;
		}
		set
		{
			survivorDef = SurvivorCatalog.GetSurvivorDef(value);
		}
	}

	public SurvivorDef survivorDef
	{
		get
		{
			return _survivorDef;
		}
		set
		{
			if ((object)_survivorDef != value)
			{
				_survivorDef = value;
				_survivorIndex = (_survivorDef ? _survivorDef.survivorIndex : SurvivorIndex.None);
				survivorBodyIndex = (_survivorDef ? BodyCatalog.FindBodyIndex(_survivorDef.bodyPrefab) : BodyIndex.None);
				survivorBodyPrefabBodyComponent = BodyCatalog.GetBodyPrefabBodyComponent(survivorBodyIndex);
				shouldRebuild = true;
				UpdateAvailability();
			}
		}
	}

	public bool survivorIsAvailable { get; private set; }

	public HGButton hgButton { get; private set; }

	private void Awake()
	{
		hgButton = GetComponent<HGButton>();
	}

	private void Update()
	{
		isCurrentChoice = (object)characterSelectBarController.pickedIcon == this;
		UpdateAvailability();
		if (shouldRebuild)
		{
			shouldRebuild = false;
			Rebuild();
		}
		hgButton.interactable = true;
		survivorIsSelectedEffect.SetActive(isCurrentChoice);
	}

	private LocalUser GetLocalUser()
	{
		return ((MPEventSystem)EventSystem.current).localUser;
	}

	private void SetBoolAndMarkDirtyIfChanged(ref bool oldValue, bool newValue)
	{
		if (oldValue != newValue)
		{
			oldValue = newValue;
			shouldRebuild = true;
		}
	}

	private void UpdateAvailability()
	{
		SetBoolAndMarkDirtyIfChanged(ref survivorIsUnlocked, SurvivorCatalog.SurvivorIsUnlockedOnThisClient(survivorIndex));
		SetBoolAndMarkDirtyIfChanged(ref survivorRequiredEntitlementAvailable, survivorDef.CheckUserHasRequiredEntitlement(GetLocalUser()));
		SetBoolAndMarkDirtyIfChanged(ref survivorRequiredExpansionEnabled, survivorDef.CheckRequiredExpansionEnabled());
		survivorRequiredExpansionEnabled = survivorRequiredExpansionEnabled || ExpansionRequirementComponent.ignoreExpansionDisablingForPlayerControlled;
		survivorIsAvailable = survivorIsUnlocked && survivorRequiredExpansionEnabled && survivorRequiredEntitlementAvailable;
	}

	private void Rebuild()
	{
		string viewableName = "";
		string viewableName2 = "";
		Texture texture = null;
		string text = "";
		Color titleColor = Color.clear;
		string overrideBodyText = "";
		if (survivorDef != null)
		{
			UnlockableDef unlockableDef = survivorDef.unlockableDef;
			if ((bool)survivorBodyPrefabBodyComponent)
			{
				texture = survivorBodyPrefabBodyComponent.portraitIcon;
				viewableName = string.Format(CultureInfo.InvariantCulture, "/Survivors/{0}", survivorDef.cachedName);
				viewableName2 = string.Format(CultureInfo.InvariantCulture, "/Loadout/Bodies/{0}/", BodyCatalog.GetBodyName(survivorBodyPrefabBodyComponent.bodyIndex));
				if (!survivorIsAvailable)
				{
					text = "UNIDENTIFIED";
					titleColor = Color.gray;
					if (!survivorRequiredEntitlementAvailable)
					{
						overrideBodyText = Language.GetStringFormatted("EXPANSION_PURCHASED_REQUIRED_FORMAT", Language.GetString(survivorDef.GetRequiredEntitlement().nameToken));
					}
					else if (!survivorRequiredExpansionEnabled)
					{
						overrideBodyText = Language.GetStringFormatted("EXPANSION_ENABLED_REQUIRED_FORMAT", Language.GetString(survivorDef.GetRequiredExpansion().nameToken));
					}
					else if (!survivorIsUnlocked && (bool)unlockableDef)
					{
						overrideBodyText = unlockableDef.getHowToUnlockString();
					}
				}
			}
		}
		if ((bool)survivorIcon)
		{
			survivorIcon.texture = texture;
			survivorIcon.color = (survivorIsAvailable ? Color.white : Color.black);
		}
		if ((bool)viewableTag)
		{
			viewableTag.viewableName = viewableName;
			viewableTag.Refresh();
		}
		if ((bool)loadoutViewableTag)
		{
			loadoutViewableTag.viewableName = viewableName2;
			loadoutViewableTag.Refresh();
		}
		if ((bool)viewableTrigger)
		{
			viewableTrigger.viewableName = viewableName;
		}
		if ((bool)tooltipProvider)
		{
			tooltipProvider.enabled = text != "";
			tooltipProvider.titleToken = text;
			tooltipProvider.titleColor = titleColor;
			tooltipProvider.overrideBodyText = overrideBodyText;
		}
		hgButton.disableGamepadClick = !survivorIsAvailable;
		hgButton.disablePointerClick = !survivorIsAvailable;
	}
}
