using System;
using System.Linq;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI.LogBook;

public class CategoryDef
{
	public class NavButtons
	{
		public string scroll = "SUBMENU_SCROLL";

		public string back = "SUBMENU_BACK";

		public string navigate = "LOGBOOK_ZOOMINOUT";

		public string rotate = "LOGBOOK_ROTATE";
	}

	[NotNull]
	public delegate Entry[] BuildEntriesDelegate([CanBeNull] UserProfile viewerProfile);

	[NotNull]
	public string nameToken = string.Empty;

	private GameObject _iconPrefab;

	public Vector2 iconSize = Vector2.one;

	public bool fullWidth;

	public Action<GameObject, Entry, EntryStatus, UserProfile> initializeElementGraphics = InitializeDefault;

	public NavButtons navButtons = new NavButtons();

	[CanBeNull]
	public ViewablesCatalog.Node viewableNode;

	public GameObject iconPrefab
	{
		get
		{
			return _iconPrefab;
		}
		[NotNull]
		set
		{
			_iconPrefab = value;
			iconSize = ((RectTransform)_iconPrefab.transform).sizeDelta;
		}
	}

	[NotNull]
	public BuildEntriesDelegate buildEntries { private get; set; }

	public Entry[] BuildEntries([CanBeNull] UserProfile viewerProfile)
	{
		Entry[] array = buildEntries(viewerProfile);
		for (int i = 0; i < array.Length; i++)
		{
			array[i].category = this;
		}
		return array;
	}

	public static void InitializeDefault(GameObject gameObject, Entry entry, EntryStatus status, UserProfile userProfile)
	{
		Texture texture = null;
		Texture texture2 = null;
		Color color = Color.white;
		switch (status)
		{
		case EntryStatus.Unimplemented:
			texture = LegacyResourcesAPI.Load<Texture2D>("Textures/MiscIcons/texWIPIcon");
			break;
		case EntryStatus.Locked:
			texture = LegacyResourcesAPI.Load<Texture2D>("Textures/MiscIcons/texUnlockIcon");
			color = Color.gray;
			break;
		case EntryStatus.Unencountered:
			texture = entry.iconTexture;
			color = Color.black;
			break;
		case EntryStatus.Available:
			texture = entry.iconTexture;
			texture2 = entry.bgTexture;
			color = Color.white;
			break;
		case EntryStatus.New:
			texture = entry.iconTexture;
			texture2 = entry.bgTexture;
			color = new Color(1f, 0.8f, 0.5f, 1f);
			break;
		default:
			throw new ArgumentOutOfRangeException("status", status, null);
		}
		RawImage rawImage = null;
		RawImage rawImage2 = null;
		ChildLocator component = gameObject.GetComponent<ChildLocator>();
		if ((bool)component)
		{
			rawImage = component.FindChild("Icon").GetComponent<RawImage>();
			rawImage2 = component.FindChild("BG").GetComponent<RawImage>();
		}
		else
		{
			rawImage = gameObject.GetComponentInChildren<RawImage>();
		}
		rawImage.texture = texture;
		rawImage.color = color;
		if ((bool)rawImage2)
		{
			if (texture2 != null)
			{
				rawImage2.texture = texture2;
			}
			else
			{
				rawImage2.enabled = false;
			}
		}
		TextMeshProUGUI componentInChildren = gameObject.GetComponentInChildren<TextMeshProUGUI>();
		if ((bool)componentInChildren)
		{
			if (status >= EntryStatus.Available)
			{
				componentInChildren.text = entry.GetDisplayName(userProfile);
			}
			else
			{
				componentInChildren.text = Language.GetString("UNIDENTIFIED");
			}
		}
	}

	public static void InitializeChallenge(GameObject gameObject, Entry entry, EntryStatus status, UserProfile userProfile)
	{
		TextMeshProUGUI textMeshProUGUI = null;
		TextMeshProUGUI textMeshProUGUI2 = null;
		RawImage rawImage = null;
		AchievementDef achievementDef = (AchievementDef)entry.extraData;
		float achievementProgress = AchievementManager.GetUserAchievementManager(LocalUserManager.readOnlyLocalUsersList.FirstOrDefault((LocalUser v) => v.userProfile == userProfile)).GetAchievementProgress(achievementDef);
		HGButton component = gameObject.GetComponent<HGButton>();
		if ((bool)component)
		{
			component.disablePointerClick = true;
			component.disableGamepadClick = true;
		}
		ChildLocator component2 = gameObject.GetComponent<ChildLocator>();
		if ((bool)component2)
		{
			textMeshProUGUI = component2.FindChild("DescriptionLabel").GetComponent<TextMeshProUGUI>();
			textMeshProUGUI2 = component2.FindChild("NameLabel").GetComponent<TextMeshProUGUI>();
			rawImage = component2.FindChild("RewardImage").GetComponent<RawImage>();
			textMeshProUGUI2.text = Language.GetString(achievementDef.nameToken);
			textMeshProUGUI.text = Language.GetString(achievementDef.descriptionToken);
		}
		Texture texture = null;
		Color color = Color.white;
		switch (status)
		{
		case EntryStatus.Unimplemented:
			texture = LegacyResourcesAPI.Load<Texture2D>("Textures/MiscIcons/texWIPIcon");
			break;
		case EntryStatus.Locked:
			texture = LegacyResourcesAPI.Load<Texture2D>("Textures/MiscIcons/texUnlockIcon");
			color = Color.black;
			textMeshProUGUI2.text = Language.GetString("UNIDENTIFIED");
			textMeshProUGUI.text = Language.GetString("UNIDENTIFIED_DESCRIPTION");
			component2.FindChild("CantBeAchieved").gameObject.SetActive(value: true);
			break;
		case EntryStatus.Unencountered:
			texture = LegacyResourcesAPI.Load<Texture2D>("Textures/MiscIcons/texUnlockIcon");
			color = Color.gray;
			component2.FindChild("ProgressTowardsUnlocking").GetComponent<Image>().fillAmount = achievementProgress;
			break;
		case EntryStatus.Available:
			texture = entry.iconTexture;
			color = Color.white;
			component2.FindChild("HasBeenUnlocked").gameObject.SetActive(value: true);
			break;
		case EntryStatus.New:
			texture = entry.iconTexture;
			color = new Color(1f, 0.8f, 0.5f, 1f);
			component2.FindChild("HasBeenUnlocked").gameObject.SetActive(value: true);
			break;
		case EntryStatus.None:
			component2.FindChild("RewardImageContainer").gameObject.SetActive(value: true);
			textMeshProUGUI2.text = "";
			textMeshProUGUI.text = "";
			break;
		default:
			throw new ArgumentOutOfRangeException("status", status, null);
		}
		if (texture != null)
		{
			rawImage.texture = texture;
			rawImage.color = color;
		}
		else
		{
			rawImage.enabled = false;
		}
	}

	public static void InitializeMorgue(GameObject gameObject, Entry entry, EntryStatus status, UserProfile userProfile)
	{
		RunReport runReport = entry.extraData as RunReport;
		GameEndingDef gameEnding = runReport.gameEnding;
		ChildLocator component = gameObject.GetComponent<ChildLocator>();
		if ((bool)component)
		{
			TextMeshProUGUI component2 = component.FindChild("DescriptionLabel").GetComponent<TextMeshProUGUI>();
			TextMeshProUGUI component3 = component.FindChild("NameLabel").GetComponent<TextMeshProUGUI>();
			RawImage component4 = component.FindChild("IconImage").GetComponent<RawImage>();
			Image component5 = component.FindChild("BackgroundImage").GetComponent<Image>();
			Texture iconTexture = entry.iconTexture;
			component3.text = entry.GetDisplayName(userProfile);
			component2.text = Language.GetString(runReport.gameEnding?.endingTextToken ?? string.Empty);
			component2.color = gameEnding?.foregroundColor ?? Color.white;
			component5.color = gameEnding?.backgroundColor ?? Color.black;
			if (iconTexture != null)
			{
				component4.texture = iconTexture;
			}
			else
			{
				component4.enabled = false;
			}
		}
	}

	public static void InitializeStats(GameObject gameObject, Entry entry, EntryStatus status, UserProfile userProfile)
	{
		UserProfile userProfile2 = entry.extraData as UserProfile;
		ChildLocator component = gameObject.GetComponent<ChildLocator>();
		if ((bool)component)
		{
			TextMeshProUGUI component2 = component.FindChild("NameLabel").GetComponent<TextMeshProUGUI>();
			TextMeshProUGUI component3 = component.FindChild("TimeLabel").GetComponent<TextMeshProUGUI>();
			TextMeshProUGUI component4 = component.FindChild("CompletionLabel").GetComponent<TextMeshProUGUI>();
			RawImage component5 = component.FindChild("IconImage").GetComponent<RawImage>();
			component.FindChild("BackgroundImage").GetComponent<Image>();
			Texture iconTexture = entry.iconTexture;
			float num = (float)new GameCompletionStatsHelper().GetTotalCompletion(userProfile2);
			string text = $"{num:0%}";
			TimeSpan timeSpan = TimeSpan.FromSeconds(userProfile2.totalLoginSeconds);
			string text2 = $"{(uint)timeSpan.TotalHours}:{(uint)timeSpan.Minutes:D2}";
			component2.text = entry.GetDisplayName(userProfile2);
			component3.text = text2;
			component4.text = text;
			if (iconTexture != null)
			{
				component5.texture = iconTexture;
			}
			else
			{
				component5.enabled = false;
			}
		}
	}
}
