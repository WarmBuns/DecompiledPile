using System;
using System.Collections.Generic;
using RoR2.EntitlementManagement;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace RoR2.UI;

[RequireComponent(typeof(MPEventSystemLocator))]
[RequireComponent(typeof(RectTransform))]
public class LoadoutPanelController : MonoBehaviour
{
	public struct DisplayData : IEquatable<DisplayData>
	{
		public UserProfile userProfile;

		public BodyIndex bodyIndex;

		public bool Equals(DisplayData other)
		{
			if (userProfile == other.userProfile)
			{
				return bodyIndex == other.bodyIndex;
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj is DisplayData other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (((userProfile != null) ? userProfile.GetHashCode() : 0) * 397) ^ (int)bodyIndex;
		}
	}

	private struct RowData
	{
		public MPButton button;

		public int defIndex;

		public RowData(MPButton _button, int _defIndex)
		{
			button = _button;
			defIndex = _defIndex;
		}
	}

	private class Row : IDisposable
	{
		public List<RowData> rowData = new List<RowData>();

		private LoadoutPanelController owner;

		private UserProfile userProfile;

		private RectTransform rowPanelTransform;

		private RectTransform buttonContainerTransform;

		private RectTransform choiceHighlightRect;

		private Color primaryColor;

		private Color complementaryColor;

		private Func<Loadout, int> findCurrentChoice;

		private Row(LoadoutPanelController owner, BodyIndex bodyIndex, string titleToken)
		{
			this.owner = owner;
			userProfile = owner.currentDisplayData.userProfile;
			rowPanelTransform = (RectTransform)UnityEngine.Object.Instantiate(rowPrefab, owner.rowContainer).transform;
			buttonContainerTransform = (RectTransform)rowPanelTransform.Find("ButtonContainer");
			choiceHighlightRect = (RectTransform)rowPanelTransform.Find("ButtonSelectionHighlight, Checkbox");
			UserProfile.onLoadoutChangedGlobal += OnLoadoutChangedGlobal;
			SurvivorCatalog.FindSurvivorDefFromBody(BodyCatalog.GetBodyPrefab(bodyIndex));
			CharacterBody bodyPrefabBodyComponent = BodyCatalog.GetBodyPrefabBodyComponent(bodyIndex);
			if (bodyPrefabBodyComponent != null)
			{
				primaryColor = bodyPrefabBodyComponent.bodyColor;
			}
			Color.RGBToHSV(primaryColor, out var H, out var S, out var V);
			H += 0.5f;
			if (H > 1f)
			{
				H -= 1f;
			}
			complementaryColor = Color.HSVToRGB(H, S, V);
			RectTransform obj = (RectTransform)rowPanelTransform.Find("SlotLabel");
			obj.GetComponent<LanguageTextMeshController>().token = titleToken;
			obj.GetComponent<HGTextMeshProUGUI>().color = primaryColor;
		}

		private void OnLoadoutChangedGlobal(UserProfile userProfile)
		{
			if (userProfile == this.userProfile)
			{
				UpdateHighlightedChoice();
			}
		}

		public static Row FromSkillSlot(LoadoutPanelController owner, BodyIndex bodyIndex, int skillSlotIndex, GenericSkill skillSlot)
		{
			SkillFamily skillFamily = skillSlot.skillFamily;
			SkillLocator component = BodyCatalog.GetBodyPrefabBodyComponent(bodyIndex).GetComponent<SkillLocator>();
			bool addWIPIcons = false;
			string titleToken;
			switch (component.FindSkillSlot(skillSlot))
			{
			case SkillSlot.None:
				titleToken = "LOADOUT_SKILL_MISC";
				addWIPIcons = false;
				break;
			case SkillSlot.Primary:
				titleToken = "LOADOUT_SKILL_PRIMARY";
				addWIPIcons = false;
				break;
			case SkillSlot.Secondary:
				titleToken = "LOADOUT_SKILL_SECONDARY";
				break;
			case SkillSlot.Utility:
				titleToken = "LOADOUT_SKILL_UTILITY";
				break;
			case SkillSlot.Special:
				titleToken = "LOADOUT_SKILL_SPECIAL";
				break;
			default:
				throw new ArgumentOutOfRangeException();
			}
			Row row = new Row(owner, bodyIndex, titleToken);
			for (int i = 0; i < skillFamily.variants.Length; i++)
			{
				ref SkillFamily.Variant reference = ref skillFamily.variants[i];
				uint skillVariantIndexToAssign = (uint)i;
				row.AddButton(owner, reference.skillDef.icon, reference.skillDef.skillNameToken, reference.skillDef.skillDescriptionToken, row.primaryColor, delegate
				{
					Loadout loadout2 = new Loadout();
					row.userProfile.CopyLoadout(loadout2);
					loadout2.bodyLoadoutManager.SetSkillVariant(bodyIndex, skillSlotIndex, skillVariantIndexToAssign);
					row.userProfile.SetLoadout(loadout2);
				}, reference.unlockableDef?.cachedName ?? "", reference.viewableNode, isWIP: false, (int)skillVariantIndexToAssign);
			}
			row.findCurrentChoice = (Loadout loadout) => (int)loadout.bodyLoadoutManager.GetSkillVariant(bodyIndex, skillSlotIndex);
			row.FinishSetup(addWIPIcons);
			return row;
		}

		public static Row FromSkin(LoadoutPanelController owner, BodyIndex bodyIndex)
		{
			Row row = new Row(owner, bodyIndex, "LOADOUT_SKIN");
			LocalUser user = owner.eventSystemLocator.eventSystem?.localUser;
			SkinDef[] bodySkins = BodyCatalog.GetBodySkins(bodyIndex);
			for (int i = 0; i < bodySkins.Length; i++)
			{
				SkinDef skinDef = bodySkins[i];
				if (EntitlementManager.localUserEntitlementTracker.UserHasEntitlementForUnlockable(user, skinDef.unlockableDef))
				{
					uint skinToAssign = (uint)i;
					ViewablesCatalog.Node viewableNode = ViewablesCatalog.FindNode($"/Loadout/Bodies/{BodyCatalog.GetBodyName(bodyIndex)}/Skins/{skinDef.name}");
					row.AddButton(owner, skinDef.icon, skinDef.nameToken, string.Empty, row.primaryColor, delegate
					{
						Loadout loadout2 = new Loadout();
						row.userProfile.CopyLoadout(loadout2);
						loadout2.bodyLoadoutManager.SetSkinIndex(bodyIndex, skinToAssign);
						row.userProfile.SetLoadout(loadout2);
					}, skinDef.unlockableDef?.cachedName ?? "", viewableNode, isWIP: false, i);
				}
			}
			row.findCurrentChoice = (Loadout loadout) => (int)loadout.bodyLoadoutManager.GetSkinIndex(bodyIndex);
			row.FinishSetup();
			return row;
		}

		private void FinishSetup(bool addWIPIcons = false)
		{
			if (addWIPIcons)
			{
				Sprite icon = LegacyResourcesAPI.Load<Sprite>("Textures/MiscIcons/texWIPIcon");
				for (int i = rowData.Count; i < minimumEntriesPerRow; i++)
				{
					AddButton(owner, icon, "TOOLTIP_WIP_CONTENT_NAME", "TOOLTIP_WIP_CONTENT_DESCRIPTION", ColorCatalog.GetColor(ColorCatalog.ColorIndex.WIP), delegate
					{
					}, "", null, isWIP: true);
				}
			}
			RectTransform rectTransform = (RectTransform)rowPanelTransform.Find("ButtonContainer/Spacer");
			if ((bool)rectTransform)
			{
				rectTransform.SetAsLastSibling();
			}
			UpdateHighlightedChoice();
		}

		private void SetButtonColorMultiplier(int i, float f)
		{
			MPButton button = rowData[i].button;
			ColorBlock colors = button.colors;
			colors.colorMultiplier = f;
			button.colors = colors;
		}

		private void UpdateHighlightedChoice()
		{
			Loadout loadout = new Loadout();
			userProfile?.CopyLoadout(loadout);
			int num = findCurrentChoice(loadout);
			for (int i = 0; i < rowData.Count; i++)
			{
				ColorBlock colors = rowData[i].button.colors;
				colors.colorMultiplier = 0.5f;
				rowData[i].button.colors = colors;
				SetButtonColorMultiplier(i, 0.5f);
				if (num == rowData[i].defIndex)
				{
					choiceHighlightRect.SetParent((RectTransform)rowData[i].button.transform, worldPositionStays: false);
					SetButtonColorMultiplier(i, 1f);
				}
			}
		}

		private void AddButton(LoadoutPanelController owner, Sprite icon, string titleToken, string bodyToken, Color tooltipColor, UnityAction callback, string unlockableName, ViewablesCatalog.Node viewableNode, bool isWIP = false, int defIndex = -1)
		{
			HGButton component = UnityEngine.Object.Instantiate(loadoutButtonPrefab, buttonContainerTransform).GetComponent<HGButton>();
			component.updateTextOnHover = true;
			component.hoverLanguageTextMeshController = owner.hoverTextDescription;
			component.requiredTopLayer = owner.requiredUILayerKey;
			string text = "";
			TooltipProvider component2 = component.GetComponent<TooltipProvider>();
			UserProfile obj = userProfile;
			string @string;
			string text2;
			Color color;
			if (obj != null && obj.HasUnlockable(unlockableName))
			{
				component.onClick.AddListener(callback);
				component.interactable = true;
				if (viewableNode != null)
				{
					ViewableTag component3 = component.GetComponent<ViewableTag>();
					component3.viewableName = viewableNode.fullName;
					component3.Refresh();
				}
				@string = Language.GetString(titleToken);
				text2 = Language.GetString(bodyToken);
				color = tooltipColor;
			}
			else
			{
				UnlockableDef unlockableDef = UnlockableCatalog.GetUnlockableDef(unlockableName);
				icon = lockedIcon;
				component.interactable = true;
				component.disableGamepadClick = true;
				component.disablePointerClick = true;
				@string = Language.GetString("UNIDENTIFIED");
				text2 = unlockableDef.getHowToUnlockString();
				color = Color.gray;
			}
			if (isWIP)
			{
				component.interactable = false;
			}
			component2.titleColor = color;
			component2.overrideTitleText = @string;
			component2.overrideBodyText = text2;
			color.a = 0.2f;
			text = Language.GetStringFormatted("LOGBOOK_HOVER_DESCRIPTION_FORMAT", @string, text2, ColorUtility.ToHtmlStringRGBA(color));
			component.hoverToken = text;
			((Image)component.targetGraphic).sprite = icon;
			rowData.Add(new RowData(component, defIndex));
		}

		public void Dispose()
		{
			UserProfile.onLoadoutChangedGlobal -= OnLoadoutChangedGlobal;
			for (int num = rowData.Count - 1; num >= 0; num--)
			{
				UnityEngine.Object.Destroy(rowData[num].button.gameObject);
			}
			UnityEngine.Object.Destroy(rowPanelTransform.gameObject);
		}
	}

	public UILayerKey requiredUILayerKey;

	public LanguageTextMeshController hoverTextDescription;

	private DisplayData currentDisplayData = new DisplayData
	{
		userProfile = null,
		bodyIndex = BodyIndex.None
	};

	private UIElementAllocator<RectTransform> buttonAllocator;

	private readonly List<Row> rows = new List<Row>();

	public static int minimumEntriesPerRow = 2;

	private MPEventSystemLocator eventSystemLocator;

	private UIElementAllocator<RectTransform> rowAllocator;

	private static GameObject loadoutButtonPrefab;

	private static GameObject rowPrefab;

	private static Sprite lockedIcon;

	private RectTransform rowContainer => (RectTransform)base.transform;

	public void SetDisplayData(DisplayData displayData)
	{
		if (!displayData.Equals(currentDisplayData))
		{
			currentDisplayData = displayData;
			Rebuild();
		}
	}

	private void OnEnable()
	{
		UpdateDisplayData();
	}

	private void Update()
	{
		UpdateDisplayData();
	}

	private void UpdateDisplayData()
	{
		UserProfile userProfile = eventSystemLocator.eventSystem?.localUser?.userProfile;
		NetworkUser networkUser = eventSystemLocator.eventSystem?.localUser?.currentNetworkUser;
		BodyIndex bodyIndex = (networkUser ? networkUser.bodyIndexPreference : BodyIndex.None);
		SetDisplayData(new DisplayData
		{
			userProfile = userProfile,
			bodyIndex = bodyIndex
		});
	}

	private void DestroyRows()
	{
		for (int num = rows.Count - 1; num >= 0; num--)
		{
			rows[num].Dispose();
		}
		rows.Clear();
	}

	private void Rebuild()
	{
		DestroyRows();
		CharacterBody bodyPrefabBodyComponent = BodyCatalog.GetBodyPrefabBodyComponent(currentDisplayData.bodyIndex);
		if (!bodyPrefabBodyComponent)
		{
			return;
		}
		List<GenericSkill> gameObjectComponents = GetComponentsCache<GenericSkill>.GetGameObjectComponents(bodyPrefabBodyComponent.gameObject);
		int i = 0;
		for (int count = gameObjectComponents.Count; i < count; i++)
		{
			GenericSkill skillSlot = gameObjectComponents[i];
			rows.Add(Row.FromSkillSlot(this, currentDisplayData.bodyIndex, i, skillSlot));
		}
		_ = BodyCatalog.GetBodySkins(currentDisplayData.bodyIndex).Length;
		if (true)
		{
			rows.Add(Row.FromSkin(this, currentDisplayData.bodyIndex));
		}
		int count2 = rows.Count;
		for (int j = 0; j < count2; j++)
		{
			int count3 = rows[j].rowData.Count;
			for (int k = 0; k < count3; k++)
			{
				UnityEngine.UI.Navigation navigation = rows[j].rowData[k].button.navigation;
				navigation.mode = UnityEngine.UI.Navigation.Mode.Explicit;
				navigation.selectOnLeft = null;
				navigation.selectOnRight = null;
				navigation.selectOnUp = null;
				navigation.selectOnDown = null;
				if (count3 > 1)
				{
					int index = (k + count3 - 1) % count3;
					navigation.selectOnLeft = rows[j].rowData[index].button;
					int index2 = (k + count3 + 1) % count3;
					navigation.selectOnRight = rows[j].rowData[index2].button;
				}
				if (count2 > 1)
				{
					if (j != 0)
					{
						int num = rows[j - 1].rowData.Count - 1;
						int index3 = ((num < k) ? num : k);
						navigation.selectOnUp = rows[j - 1].rowData[index3].button;
					}
					if (j != count2 - 1)
					{
						int num2 = rows[j + 1].rowData.Count - 1;
						int index4 = ((num2 < k) ? num2 : k);
						navigation.selectOnDown = rows[j + 1].rowData[index4].button;
					}
				}
				rows[j].rowData[k].button.navigation = navigation;
			}
		}
	}

	private void Awake()
	{
		eventSystemLocator = GetComponent<MPEventSystemLocator>();
		EntitlementManager.onEntitlementsUpdated += OnEntitlementsUpdated;
	}

	[InitDuringStartup]
	private static void Init()
	{
		AsyncOperationHandle<GameObject> asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/UI/Loadout/LoadoutButton");
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
		{
			loadoutButtonPrefab = x.Result;
		};
		asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/UI/Loadout/Row");
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
		{
			rowPrefab = x.Result;
		};
		AsyncOperationHandle<Sprite> asyncOperationHandle2 = LegacyResourcesAPI.LoadAsync<Sprite>("Textures/MiscIcons/texUnlockIcon");
		asyncOperationHandle2.Completed += delegate(AsyncOperationHandle<Sprite> x)
		{
			lockedIcon = x.Result;
		};
	}

	private void OnDestroy()
	{
		EntitlementManager.onEntitlementsUpdated -= OnEntitlementsUpdated;
		DestroyRows();
	}

	private void OnEntitlementsUpdated()
	{
		Rebuild();
	}
}
