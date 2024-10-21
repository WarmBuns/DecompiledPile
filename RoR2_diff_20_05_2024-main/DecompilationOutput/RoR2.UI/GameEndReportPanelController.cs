using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JetBrains.Annotations;
using RoR2.Stats;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RoR2.UI;

[RequireComponent(typeof(MPEventSystemProvider))]
public class GameEndReportPanelController : MonoBehaviour
{
	public struct DisplayData : IEquatable<DisplayData>
	{
		[CanBeNull]
		public RunReport runReport;

		public int playerIndex;

		public bool Equals(DisplayData other)
		{
			if (object.Equals(runReport, other.runReport))
			{
				return playerIndex == other.playerIndex;
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			if (obj is DisplayData other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return ((-1418150836 * -1521134295 + base.GetHashCode()) * -1521134295 + EqualityComparer<RunReport>.Default.GetHashCode(runReport)) * -1521134295 + playerIndex.GetHashCode();
		}
	}

	[Header("Result")]
	[Tooltip("The TextMeshProUGUI component to use to display the result of the game: Win or Loss")]
	public TextMeshProUGUI resultLabel;

	public Image resultIconBackgroundImage;

	public Image resultIconForegroundImage;

	public HGTextMeshProUGUI finalMessageLabel;

	[Header("Run Settings")]
	[Tooltip("The Image component to use to display the selected difficulty of the run.")]
	public Image selectedDifficultyImage;

	[Tooltip("The LanguageTextMeshController component to use to display the selected difficulty of the run.")]
	public LanguageTextMeshController selectedDifficultyLabel;

	[Tooltip("The ArtifactDisplayPanelController component to send the enabled artifacts of the run to.")]
	public ArtifactDisplayPanelController artifactDisplayPanelController;

	[Header("Stats")]
	[Tooltip("A list of StatDef names to display in the stats section.")]
	public string[] statsToDisplay;

	[Tooltip("Prefab to be used for stat display.")]
	public GameObject statStripPrefab;

	[Tooltip("The RectTransform in which to build the stat strips.")]
	public RectTransform statContentArea;

	[Tooltip("The TextMeshProUGUI component used to display the total points.")]
	public TextMeshProUGUI totalPointsLabel;

	[Header("Unlocks")]
	[Tooltip("Prefab to be used for unlock display.")]
	public GameObject unlockStripPrefab;

	[Tooltip("The RectTransform in which to build the unlock strips.")]
	public RectTransform unlockContentArea;

	[Header("Items")]
	[Tooltip("The inventory display controller.")]
	public ItemInventoryDisplay itemInventoryDisplay;

	[Header("Player Info")]
	[Tooltip("The RawImage component to use to display the player character's portrait.")]
	public RawImage playerBodyPortraitImage;

	[Tooltip("The TextMeshProUGUI component to use to display the player character's body name.")]
	public TextMeshProUGUI playerBodyLabel;

	[Tooltip("The TextMeshProUGUI component to use to display the player's username.")]
	public TextMeshProUGUI playerUsernameLabel;

	[Header("Killer Info")]
	[Tooltip("The RawImage component to use to display the killer character's portrait.")]
	public RawImage killerBodyPortraitImage;

	[Tooltip("The TextMeshProUGUI component to use to display the killer character's body name.")]
	public TextMeshProUGUI killerBodyLabel;

	[Tooltip("The GameObject used as the panel for the killer information. This is used to disable the killer panel when the player has won the game.")]
	public GameObject killerPanelObject;

	[Header("Navigation and Misc")]
	public MPButton continueButton;

	public RectTransform chatboxTransform;

	public CarouselNavigationController playerNavigationController;

	public RectTransform selectedPlayerEffectRoot;

	public RectTransform acceptButtonArea;

	private readonly List<GameObject> statStrips = new List<GameObject>();

	private readonly List<GameObject> unlockStrips = new List<GameObject>();

	public DisplayData displayData { get; private set; }

	private void Awake()
	{
		playerNavigationController.onPageChangeSubmitted += OnPlayerNavigationControllerPageChangeSubmitted;
		SetContinueButtonAction(null);
	}

	private void AllocateStatStrips(int count)
	{
		while (statStrips.Count > count)
		{
			int index = statStrips.Count - 1;
			UnityEngine.Object.Destroy(statStrips[index].gameObject);
			statStrips.RemoveAt(index);
		}
		while (statStrips.Count < count)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(statStripPrefab, statContentArea);
			gameObject.SetActive(value: true);
			statStrips.Add(gameObject);
		}
	}

	private void AllocateUnlockStrips(int count)
	{
		while (unlockStrips.Count > count)
		{
			int index = unlockStrips.Count - 1;
			UnityEngine.Object.Destroy(unlockStrips[index].gameObject);
			unlockStrips.RemoveAt(index);
		}
		while (unlockStrips.Count < count)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(unlockStripPrefab, unlockContentArea);
			gameObject.SetActive(value: true);
			unlockStrips.Add(gameObject);
		}
	}

	public void SetDisplayData(DisplayData newDisplayData)
	{
		if (displayData.Equals(newDisplayData))
		{
			return;
		}
		displayData = newDisplayData;
		bool flag = (bool)Run.instance && displayData.runReport != null && Run.instance.GetUniqueId() == displayData.runReport.runGuid;
		if ((bool)resultLabel && (bool)resultIconBackgroundImage && (bool)resultIconForegroundImage)
		{
			GameEndingDef gameEndingDef = null;
			if (displayData.runReport != null)
			{
				gameEndingDef = displayData.runReport.gameEnding;
			}
			resultLabel.gameObject.SetActive(gameEndingDef);
			resultIconBackgroundImage.gameObject.SetActive(gameEndingDef);
			resultIconForegroundImage.gameObject.SetActive(gameEndingDef);
			if ((bool)gameEndingDef)
			{
				resultLabel.text = Language.GetString(gameEndingDef.endingTextToken);
				resultIconBackgroundImage.color = gameEndingDef.backgroundColor;
				resultIconForegroundImage.color = gameEndingDef.foregroundColor;
				resultIconForegroundImage.sprite = gameEndingDef.icon;
				resultIconBackgroundImage.material = gameEndingDef.material;
			}
		}
		DifficultyIndex difficultyIndex = DifficultyIndex.Invalid;
		if (displayData.runReport != null)
		{
			difficultyIndex = displayData.runReport.ruleBook.FindDifficulty();
		}
		DifficultyDef difficultyDef = DifficultyCatalog.GetDifficultyDef(difficultyIndex);
		if ((bool)selectedDifficultyImage)
		{
			selectedDifficultyImage.sprite = difficultyDef?.GetIconSprite();
		}
		if ((bool)selectedDifficultyLabel)
		{
			selectedDifficultyLabel.token = difficultyDef?.nameToken;
		}
		if ((bool)artifactDisplayPanelController)
		{
			RuleBook ruleBook = displayData.runReport.ruleBook;
			List<ArtifactDef> list = new List<ArtifactDef>(ArtifactCatalog.artifactCount);
			for (int i = 0; i < RuleCatalog.choiceCount; i++)
			{
				RuleChoiceDef choiceDef = RuleCatalog.GetChoiceDef(i);
				if (choiceDef.artifactIndex != ArtifactIndex.None && ruleBook.GetRuleChoice(choiceDef.ruleDef) == choiceDef)
				{
					list.Add(ArtifactCatalog.GetArtifactDef(choiceDef.artifactIndex));
				}
			}
			List<ArtifactDef>.Enumerator enabledArtifacts = list.GetEnumerator();
			artifactDisplayPanelController.SetDisplayData(ref enabledArtifacts);
		}
		RunReport.PlayerInfo playerInfo = displayData.runReport?.GetPlayerInfoSafe(displayData.playerIndex);
		SetPlayerInfo(playerInfo, displayData.playerIndex);
		int pageCount = displayData.runReport?.playerInfoCount ?? 0;
		playerNavigationController.gameObject.SetActive((displayData.runReport?.playerInfoCount ?? 0) > 1);
		if ((bool)chatboxTransform)
		{
			chatboxTransform.gameObject.SetActive(!RoR2Application.isInSinglePlayer && flag);
		}
		playerNavigationController.SetDisplayData(new CarouselNavigationController.DisplayData(pageCount, displayData.playerIndex));
		ReadOnlyCollection<MPButton> elements = playerNavigationController.buttonAllocator.elements;
		for (int j = 0; j < elements.Count; j++)
		{
			MPButton mPButton = elements[j];
			RunReport.PlayerInfo playerInfo2 = displayData.runReport.GetPlayerInfo(j);
			CharacterBody bodyPrefabBodyComponent = BodyCatalog.GetBodyPrefabBodyComponent(playerInfo2.bodyIndex);
			Texture texture = (bodyPrefabBodyComponent ? bodyPrefabBodyComponent.portraitIcon : null);
			mPButton.GetComponentInChildren<RawImage>().texture = texture;
			mPButton.GetComponent<TooltipProvider>().SetContent(TooltipProvider.GetPlayerNameTooltipContent(playerInfo2.name));
		}
		selectedPlayerEffectRoot.transform.SetParent(playerNavigationController.buttonAllocator.elements[displayData.playerIndex].transform);
		selectedPlayerEffectRoot.gameObject.SetActive(value: false);
		selectedPlayerEffectRoot.gameObject.SetActive(value: true);
		selectedPlayerEffectRoot.offsetMin = Vector2.zero;
		selectedPlayerEffectRoot.offsetMax = Vector2.zero;
		selectedPlayerEffectRoot.localScale = Vector3.one;
	}

	public void SetContinueButtonAction(UnityAction action)
	{
		continueButton.onClick.RemoveAllListeners();
		if (action != null)
		{
			continueButton.onClick.AddListener(action);
		}
		acceptButtonArea.gameObject.SetActive(action != null);
	}

	private void OnPlayerNavigationControllerPageChangeSubmitted(int newPage)
	{
		DisplayData displayData = this.displayData;
		displayData.playerIndex = newPage;
		SetDisplayData(displayData);
	}

	private void SetPlayerInfo([CanBeNull] RunReport.PlayerInfo playerInfo, int playerIndex)
	{
		ulong num = 0uL;
		if (playerInfo != null && playerInfo.statSheet != null)
		{
			StatSheet statSheet = playerInfo.statSheet;
			AllocateStatStrips(statsToDisplay.Length);
			for (int i = 0; i < statsToDisplay.Length; i++)
			{
				string text = statsToDisplay[i];
				StatDef statDef = StatDef.Find(text);
				if (statDef == null)
				{
					Debug.LogWarningFormat("GameEndReportPanelController.SetStatSheet: Could not find stat def \"{0}\".", text);
				}
				else
				{
					AssignStatToStrip(statSheet, statDef, statStrips[i]);
					num += statSheet.GetStatPointValue(statDef);
				}
			}
			int unlockableCount = statSheet.GetUnlockableCount();
			int num2 = 0;
			for (int j = 0; j < unlockableCount; j++)
			{
				UnlockableDef unlockable = statSheet.GetUnlockable(j);
				if (unlockable != null && !unlockable.hidden)
				{
					num2++;
				}
			}
			AllocateUnlockStrips(num2);
			int num3 = 0;
			for (int k = 0; k < unlockableCount; k++)
			{
				UnlockableDef unlockable2 = statSheet.GetUnlockable(k);
				if (unlockable2 != null && !unlockable2.hidden)
				{
					AssignUnlockToStrip(unlockable2, unlockStrips[num3]);
					num3++;
				}
			}
			if ((bool)itemInventoryDisplay && playerInfo.itemAcquisitionOrder != null)
			{
				itemInventoryDisplay.SetItems(playerInfo.itemAcquisitionOrder, playerInfo.itemAcquisitionOrder.Length, playerInfo.itemStacks);
				itemInventoryDisplay.UpdateDisplay();
			}
			string token = playerInfo.finalMessageToken + "_2P";
			if (Language.IsTokenInvalid(token))
			{
				token = playerInfo.finalMessageToken;
			}
			finalMessageLabel.SetText(Language.GetStringFormatted(token, playerInfo.name));
		}
		else
		{
			AllocateStatStrips(0);
			AllocateUnlockStrips(0);
			if ((bool)itemInventoryDisplay)
			{
				itemInventoryDisplay.ResetItems();
			}
			finalMessageLabel.SetText(string.Empty);
		}
		string @string = Language.GetString("STAT_POINTS_FORMAT");
		totalPointsLabel.text = string.Format(@string, TextSerialization.ToStringNumeric(num));
		GameObject gameObject = null;
		if (playerInfo != null)
		{
			gameObject = BodyCatalog.GetBodyPrefab(playerInfo.bodyIndex);
		}
		string text2 = "";
		Texture texture = null;
		if ((bool)gameObject)
		{
			texture = gameObject.GetComponent<CharacterBody>().portraitIcon;
			text2 = Language.GetString(gameObject.GetComponent<CharacterBody>().baseNameToken);
			if (playerIndex == 0)
			{
				if (PlatformSystems.userManager.ShouldAlwaysUseCurrentDisplayName())
				{
					playerUsernameLabel.text = PlatformSystems.userManager.GetUserName();
				}
				else
				{
					playerUsernameLabel.text = playerInfo.name;
				}
			}
			else if (PlatformSystems.userManager.ShouldHideOtherDisplayNames())
			{
				playerUsernameLabel.text = text2;
			}
			else
			{
				playerUsernameLabel.text = playerInfo.name;
			}
		}
		string string2 = Language.GetString("STAT_CLASS_NAME_FORMAT");
		playerBodyLabel.text = string.Format(string2, text2);
		playerBodyPortraitImage.texture = texture;
		GameObject gameObject2 = null;
		if (playerInfo != null)
		{
			gameObject2 = BodyCatalog.GetBodyPrefab(playerInfo.killerBodyIndex);
			killerPanelObject?.SetActive(playerInfo.isDead);
		}
		string string3 = Language.GetString("UNIDENTIFIED_KILLER_NAME");
		Texture texture2 = LegacyResourcesAPI.Load<Texture>("Textures/BodyIcons/texUnidentifiedKillerIcon");
		if ((bool)gameObject2)
		{
			Texture portraitIcon = gameObject2.GetComponent<CharacterBody>().portraitIcon;
			string baseNameToken = gameObject2.GetComponent<CharacterBody>().baseNameToken;
			if (portraitIcon != null)
			{
				texture2 = portraitIcon;
			}
			if (!Language.IsTokenInvalid(baseNameToken))
			{
				string3 = Language.GetString(gameObject2.GetComponent<CharacterBody>().baseNameToken);
			}
		}
		string string4 = Language.GetString("STAT_KILLER_NAME_FORMAT");
		killerBodyLabel.text = string.Format(string4, string3);
		killerBodyPortraitImage.texture = texture2;
	}

	private void AssignStatToStrip([CanBeNull] StatSheet srcStatSheet, [NotNull] StatDef statDef, GameObject destStatStrip)
	{
		string arg = "0";
		ulong value = 0uL;
		if (srcStatSheet != null)
		{
			arg = srcStatSheet.GetStatDisplayValue(statDef);
			value = srcStatSheet.GetStatPointValue(statDef);
		}
		string @string = Language.GetString(statDef.displayToken);
		string text = string.Format(Language.GetString("STAT_NAME_VALUE_FORMAT"), @string, arg);
		destStatStrip.transform.Find("StatNameLabel").GetComponent<TextMeshProUGUI>().text = text;
		string string2 = Language.GetString("STAT_POINTS_FORMAT");
		destStatStrip.transform.Find("PointValueLabel").GetComponent<TextMeshProUGUI>().text = string.Format(string2, TextSerialization.ToStringNumeric(value));
	}

	private void AssignUnlockToStrip(UnlockableDef unlockableDef, GameObject destUnlockableStrip)
	{
		AchievementDef achievementDefFromUnlockable = AchievementManager.GetAchievementDefFromUnlockable(unlockableDef.cachedName);
		Texture texture = null;
		string @string = Language.GetString("TOOLTIP_UNLOCK_GENERIC_NAME");
		string string2 = Language.GetString("TOOLTIP_UNLOCK_GENERIC_DESCRIPTION");
		if (unlockableDef.cachedName.Contains("Items."))
		{
			@string = Language.GetString("TOOLTIP_UNLOCK_ITEM_NAME");
			string2 = Language.GetString("TOOLTIP_UNLOCK_ITEM_DESCRIPTION");
		}
		else if (unlockableDef.cachedName.Contains("Logs."))
		{
			@string = Language.GetString("TOOLTIP_UNLOCK_LOG_NAME");
			string2 = Language.GetString("TOOLTIP_UNLOCK_LOG_DESCRIPTION");
		}
		else if (unlockableDef.cachedName.Contains("Characters."))
		{
			@string = Language.GetString("TOOLTIP_UNLOCK_SURVIVOR_NAME");
			string2 = Language.GetString("TOOLTIP_UNLOCK_SURVIVOR_DESCRIPTION");
		}
		else if (unlockableDef.cachedName.Contains("Artifacts."))
		{
			@string = Language.GetString("TOOLTIP_UNLOCK_ARTIFACT_NAME");
			string2 = Language.GetString("TOOLTIP_UNLOCK_ARTIFACT_DESCRIPTION");
		}
		string string3;
		if (achievementDefFromUnlockable != null)
		{
			texture = achievementDefFromUnlockable.GetAchievedIcon().texture;
			string3 = Language.GetString(achievementDefFromUnlockable.nameToken);
		}
		else
		{
			string3 = Language.GetString(unlockableDef.nameToken);
		}
		if (texture != null)
		{
			destUnlockableStrip.transform.Find("IconImage").GetComponent<RawImage>().texture = texture;
		}
		destUnlockableStrip.transform.Find("NameLabel").GetComponent<TextMeshProUGUI>().text = string3;
		destUnlockableStrip.GetComponent<TooltipProvider>().overrideTitleText = @string;
		destUnlockableStrip.GetComponent<TooltipProvider>().overrideBodyText = string2;
	}
}
