using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using HG;
using Rewired;
using RoR2.EntitlementManagement;
using RoR2.Skills;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI;

[RequireComponent(typeof(MPEventSystemLocator))]
public class CharacterSelectController : MonoBehaviour
{
	[Serializable]
	public struct SkillStrip
	{
		public GameObject stripRoot;

		public Image skillIcon;

		public TextMeshProUGUI skillName;

		public TextMeshProUGUI skillDescription;
	}

	private struct BodyInfo
	{
		public readonly BodyIndex bodyIndex;

		public readonly GameObject bodyPrefab;

		public readonly CharacterBody bodyPrefabBodyComponent;

		public readonly Color bodyColor;

		public readonly SkillLocator skillLocator;

		public readonly GenericSkill[] skillSlots;

		public BodyInfo(BodyIndex bodyIndex)
		{
			this.bodyIndex = bodyIndex;
			bodyPrefab = BodyCatalog.GetBodyPrefab(bodyIndex);
			bodyPrefabBodyComponent = BodyCatalog.GetBodyPrefabBodyComponent(bodyIndex);
			bodyColor = (bodyPrefabBodyComponent ? bodyPrefabBodyComponent.bodyColor : Color.gray);
			skillLocator = (bodyPrefab ? bodyPrefab.GetComponent<SkillLocator>() : null);
			skillSlots = BodyCatalog.GetBodyPrefabSkillSlots(bodyIndex);
		}
	}

	private struct StripDisplayData
	{
		public bool enabled;

		public Color primaryColor;

		public Sprite icon;

		public string titleString;

		public string descriptionString;

		public string keywordString;

		public string actionName;
	}

	[Header("Survivor Panel")]
	public TextMeshProUGUI survivorName;

	public Image[] primaryColorImages;

	public TextMeshProUGUI[] primaryColorTexts;

	public GameObject activeSurvivorInfoPanel;

	public GameObject inactiveSurvivorInfoPanel;

	[Header("Overview Panel")]
	public TextMeshProUGUI survivorDescription;

	[Header("Skill Panel")]
	public GameObject skillStripPrefab;

	public GameObject skillStripFillerPrefab;

	public RectTransform skillStripContainer;

	private UIElementAllocator<RectTransform> skillStripAllocator;

	private UIElementAllocator<RectTransform> skillStripFillerAllocator;

	[Header("Loadout Panel")]
	[Tooltip("The header button for the loadout tab. Will be disabled if the user has no unlocked loadout options.")]
	public GameObject loadoutHeaderButton;

	public ViewableTag loadoutViewableTag;

	[Header("Ready and Misc")]
	public MPButton readyButton;

	public MPButton unreadyButton;

	public GameObject[] multiplayerOnlyObjects;

	private MPEventSystemLocator eventSystemLocator;

	private CharacterSelectBarController characterSelectBarController;

	private MPEventSystem eventSystem;

	private LocalUser localUser;

	private SurvivorDef currentSurvivorDef;

	private static UnlockableDef[] loadoutAssociatedUnlockableDefs;

	private bool shouldRebuild = true;

	private bool shouldShowSurvivorInfoPanel;

	private void Awake()
	{
		EntitlementManager.UpdateLocalUsersEntitlements();
		eventSystemLocator = GetComponent<MPEventSystemLocator>();
		skillStripAllocator = new UIElementAllocator<RectTransform>(skillStripContainer, skillStripPrefab);
		skillStripFillerAllocator = new UIElementAllocator<RectTransform>(skillStripContainer, skillStripFillerPrefab);
		bool active = true;
		loadoutHeaderButton.SetActive(active);
		characterSelectBarController = GetComponentInChildren<CharacterSelectBarController>();
	}

	private void OnEnable()
	{
		UserProfile.onLoadoutChangedGlobal += OnLoadoutChangedGlobal;
		EntitlementManager.onEntitlementsUpdated += OnEntitlementsUpdated;
		eventSystemLocator.onEventSystemDiscovered += OnEventSystemDiscovered;
		eventSystemLocator.onEventSystemLost += OnEventSystemLost;
		if ((bool)eventSystemLocator.eventSystem)
		{
			OnEventSystemDiscovered(eventSystemLocator.eventSystem);
		}
	}

	private void OnDisable()
	{
		eventSystemLocator.onEventSystemLost -= OnEventSystemLost;
		eventSystemLocator.onEventSystemDiscovered -= OnEventSystemDiscovered;
		if ((bool)eventSystemLocator.eventSystem)
		{
			OnEventSystemLost(eventSystemLocator.eventSystem);
		}
		EntitlementManager.onEntitlementsUpdated -= OnEntitlementsUpdated;
		UserProfile.onLoadoutChangedGlobal -= OnLoadoutChangedGlobal;
	}

	private void Update()
	{
		SurvivorDef survivorDef = null;
		NetworkUser networkUser = localUser?.currentNetworkUser;
		if ((bool)networkUser)
		{
			survivorDef = networkUser.GetSurvivorPreference();
		}
		if ((object)currentSurvivorDef != survivorDef)
		{
			bool deselectButtons = false;
			if ((bool)currentSurvivorDef && (bool)survivorDef)
			{
				deselectButtons = currentSurvivorDef.cachedName != survivorDef.cachedName;
			}
			currentSurvivorDef = survivorDef;
			RebuildLocal(deselectButtons);
		}
		if (shouldRebuild)
		{
			shouldRebuild = false;
			RebuildLocal();
		}
		UpdateSurvivorInfoPanel();
		if (!RoR2Application.isInSinglePlayer)
		{
			bool flag = IsClientReady();
			readyButton.gameObject.SetActive(!flag);
			unreadyButton.gameObject.SetActive(flag);
		}
	}

	private void OnEntitlementsUpdated()
	{
		shouldRebuild = true;
	}

	private void OnEventSystemDiscovered(MPEventSystem discoveredEventSystem)
	{
		eventSystem = discoveredEventSystem;
		localUser = (eventSystem ? eventSystem.localUser : null);
		shouldRebuild = true;
	}

	private void OnEventSystemLost(MPEventSystem lostEventSystem)
	{
		eventSystem = null;
		localUser = null;
		shouldRebuild = true;
	}

	private static UnlockableDef[] GenerateLoadoutAssociatedUnlockableDefs()
	{
		HashSet<UnlockableDef> encounteredUnlockables = new HashSet<UnlockableDef>();
		foreach (SkillFamily allSkillFamily in SkillCatalog.allSkillFamilies)
		{
			for (int i = 0; i < allSkillFamily.variants.Length; i++)
			{
				TryAddUnlockableByDef(allSkillFamily.variants[i].unlockableDef);
			}
		}
		foreach (CharacterBody allBodyPrefabBodyBodyComponent in BodyCatalog.allBodyPrefabBodyBodyComponents)
		{
			SkinDef[] bodySkins = BodyCatalog.GetBodySkins(allBodyPrefabBodyBodyComponent.bodyIndex);
			for (int j = 0; j < bodySkins.Length; j++)
			{
				TryAddUnlockableByDef(bodySkins[j].unlockableDef);
			}
		}
		return encounteredUnlockables.ToArray();
		void TryAddUnlockableByDef(UnlockableDef unlockableDef)
		{
			if (unlockableDef != null)
			{
				encounteredUnlockables.Add(unlockableDef);
			}
		}
	}

	private static bool UserHasAnyLoadoutUnlockables(LocalUser localUser)
	{
		if (loadoutAssociatedUnlockableDefs == null)
		{
			loadoutAssociatedUnlockableDefs = GenerateLoadoutAssociatedUnlockableDefs();
		}
		UserProfile userProfile = localUser.userProfile;
		UnlockableDef[] array = loadoutAssociatedUnlockableDefs;
		foreach (UnlockableDef unlockableDef in array)
		{
			if (userProfile.HasUnlockable(unlockableDef))
			{
				return true;
			}
		}
		return false;
	}

	private void RebuildLocal(bool deselectButtons = false)
	{
		Loadout loadout = Loadout.RequestInstance();
		try
		{
			NetworkUser networkUser = localUser?.currentNetworkUser;
			SurvivorDef survivorDef = (networkUser ? networkUser.GetSurvivorPreference() : null);
			if (localUser.userProfile != null)
			{
				localUser.userProfile.loadout.EnforceUnlockables(localUser.userProfile);
			}
			localUser?.userProfile.CopyLoadout(loadout);
			BodyInfo bodyInfo = new BodyInfo(SurvivorCatalog.GetBodyIndexFromSurvivorIndex(survivorDef ? survivorDef.survivorIndex : SurvivorIndex.None));
			string sourceText = string.Empty;
			string sourceText2 = string.Empty;
			if ((bool)survivorDef)
			{
				sourceText = Language.GetString(survivorDef.displayNameToken);
				sourceText2 = Language.GetString(survivorDef.descriptionToken);
			}
			survivorName.SetText(sourceText);
			survivorDescription.SetText(sourceText2);
			Image[] array = primaryColorImages;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].color = bodyInfo.bodyColor;
			}
			TextMeshProUGUI[] array2 = primaryColorTexts;
			for (int i = 0; i < array2.Length; i++)
			{
				array2[i].color = bodyInfo.bodyColor;
			}
			List<StripDisplayData> list = new List<StripDisplayData>();
			BuildSkillStripDisplayData(loadout, in bodyInfo, list);
			skillStripFillerAllocator.AllocateElements(0);
			skillStripAllocator.AllocateElements(list.Count);
			for (int j = 0; j < list.Count; j++)
			{
				RebuildStrip(skillStripAllocator.elements[j], list[j]);
			}
			if (deselectButtons)
			{
				DeselectButton();
			}
			skillStripFillerAllocator.AllocateElements(Mathf.Max(0, 5 - list.Count));
			skillStripFillerAllocator.MoveElementsToContainerEnd();
			string empty = string.Empty;
			StringBuilder stringBuilder = HG.StringBuilderPool.RentStringBuilder();
			stringBuilder.Append("/Loadout/Bodies/").Append(BodyCatalog.GetBodyName(bodyInfo.bodyIndex)).Append("/");
			empty = stringBuilder.ToString();
			HG.StringBuilderPool.ReturnStringBuilder(stringBuilder);
			loadoutViewableTag.viewableName = empty;
			bool active = RoR2Application.isInMultiPlayer && !RoR2Application.isInLocalMultiPlayer;
			GameObject[] array3 = multiplayerOnlyObjects;
			for (int i = 0; i < array3.Length; i++)
			{
				array3[i].SetActive(active);
			}
		}
		finally
		{
			Loadout.ReturnInstance(loadout);
		}
	}

	public void RebuildOptions()
	{
		characterSelectBarController?.Build();
	}

	public void DeselectButton()
	{
		if (eventSystem.currentSelectedGameObject != null)
		{
			eventSystem.SetSelectedGameObject(null);
		}
	}

	private void RebuildStrip(RectTransform skillStrip, StripDisplayData stripDisplayData)
	{
		GameObject gameObject = skillStrip.gameObject;
		Image component = skillStrip.Find("Inner/Icon").GetComponent<Image>();
		HGTextMeshProUGUI component2 = skillStrip.Find("Inner/SkillDescriptionPanel/SkillName").GetComponent<HGTextMeshProUGUI>();
		HGTextMeshProUGUI component3 = skillStrip.Find("Inner/SkillDescriptionPanel/SkillDescription").GetComponent<HGTextMeshProUGUI>();
		HGButton component4 = skillStrip.gameObject.GetComponent<HGButton>();
		if (stripDisplayData.enabled)
		{
			gameObject.SetActive(value: true);
			component.sprite = stripDisplayData.icon;
			component2.SetText(stripDisplayData.titleString);
			component2.color = stripDisplayData.primaryColor;
			component3.SetText(stripDisplayData.descriptionString);
			component4.hoverToken = stripDisplayData.keywordString;
		}
		else
		{
			gameObject.SetActive(value: false);
		}
	}

	private void BuildSkillStripDisplayData(Loadout loadout, in BodyInfo bodyInfo, List<StripDisplayData> dest)
	{
		if (!bodyInfo.bodyPrefab || !bodyInfo.bodyPrefabBodyComponent || !bodyInfo.skillLocator)
		{
			return;
		}
		BodyIndex bodyIndex = bodyInfo.bodyIndex;
		SkillLocator skillLocator = bodyInfo.skillLocator;
		GenericSkill[] skillSlots = bodyInfo.skillSlots;
		Color bodyColor = bodyInfo.bodyColor;
		if (skillLocator.passiveSkill.enabled)
		{
			dest.Add(new StripDisplayData
			{
				enabled = true,
				primaryColor = bodyColor,
				icon = skillLocator.passiveSkill.icon,
				titleString = Language.GetString(skillLocator.passiveSkill.skillNameToken),
				descriptionString = Language.GetString(skillLocator.passiveSkill.skillDescriptionToken),
				keywordString = (string.IsNullOrEmpty(skillLocator.passiveSkill.keywordToken) ? "" : Language.GetString(skillLocator.passiveSkill.keywordToken)),
				actionName = ""
			});
		}
		for (int i = 0; i < skillSlots.Length; i++)
		{
			GenericSkill genericSkill = skillSlots[i];
			if (genericSkill.hideInCharacterSelect)
			{
				continue;
			}
			uint skillVariant = loadout.bodyLoadoutManager.GetSkillVariant(bodyIndex, i);
			SkillDef skillDef = genericSkill.skillFamily.variants[skillVariant].skillDef;
			string actionName = "";
			switch (skillLocator.FindSkillSlot(genericSkill))
			{
			case SkillSlot.Primary:
				actionName = "PrimarySkill";
				break;
			case SkillSlot.Secondary:
				actionName = "SecondarySkill";
				break;
			case SkillSlot.Utility:
				actionName = "UtilitySkill";
				break;
			case SkillSlot.Special:
				actionName = "SpecialSkill";
				break;
			}
			string keywordString = string.Empty;
			if (skillDef.keywordTokens != null)
			{
				StringBuilder stringBuilder = HG.StringBuilderPool.RentStringBuilder();
				for (int j = 0; j < skillDef.keywordTokens.Length; j++)
				{
					string @string = Language.GetString(skillDef.keywordTokens[j]);
					stringBuilder.Append(@string).Append("\n\n");
				}
				keywordString = stringBuilder.ToString();
				stringBuilder = HG.StringBuilderPool.ReturnStringBuilder(stringBuilder);
			}
			dest.Add(new StripDisplayData
			{
				enabled = true,
				primaryColor = bodyColor,
				icon = skillDef.icon,
				titleString = Language.GetString(skillDef.skillNameToken),
				descriptionString = Language.GetString(skillDef.skillDescriptionToken),
				keywordString = keywordString,
				actionName = actionName
			});
		}
	}

	private void OnLoadoutChangedGlobal(UserProfile userProfile)
	{
		if (userProfile == localUser?.userProfile)
		{
			shouldRebuild = true;
		}
	}

	private void UpdateSurvivorInfoPanel()
	{
		if ((bool)eventSystem && eventSystem.currentInputSource == MPEventSystem.InputSource.MouseAndKeyboard)
		{
			shouldShowSurvivorInfoPanel = true;
		}
		activeSurvivorInfoPanel.SetActive(shouldShowSurvivorInfoPanel);
		inactiveSurvivorInfoPanel.SetActive(!shouldShowSurvivorInfoPanel);
	}

	public void SetSurvivorInfoPanelActive(bool active)
	{
		shouldShowSurvivorInfoPanel = active;
		UpdateSurvivorInfoPanel();
	}

	private static bool InputPlayerIsAssigned(Player inputPlayer)
	{
		ReadOnlyCollection<NetworkUser> readOnlyInstancesList = NetworkUser.readOnlyInstancesList;
		for (int i = 0; i < readOnlyInstancesList.Count; i++)
		{
			if (readOnlyInstancesList[i].inputPlayer == inputPlayer)
			{
				return true;
			}
		}
		return false;
	}

	public bool IsClientReady()
	{
		int num = 0;
		if (!PreGameController.instance)
		{
			return false;
		}
		VoteController component = PreGameController.instance.GetComponent<VoteController>();
		if (!component)
		{
			return false;
		}
		int i = 0;
		for (int voteCount = component.GetVoteCount(); i < voteCount; i++)
		{
			UserVote vote = component.GetVote(i);
			if ((bool)vote.networkUserObject && vote.receivedVote)
			{
				NetworkUser component2 = vote.networkUserObject.GetComponent<NetworkUser>();
				if ((bool)component2 && component2.isLocalPlayer)
				{
					num++;
				}
			}
		}
		return num == NetworkUser.readOnlyLocalPlayersList.Count;
	}

	public void ClientSetReady()
	{
		if (PauseManager.isPaused)
		{
			return;
		}
		foreach (NetworkUser readOnlyLocalPlayers in NetworkUser.readOnlyLocalPlayersList)
		{
			if ((bool)readOnlyLocalPlayers && eventSystemLocator.eventSystem == readOnlyLocalPlayers.localUser.eventSystem)
			{
				readOnlyLocalPlayers.CallCmdSubmitVote(PreGameController.instance.gameObject, 0);
			}
			else
			{
				_ = (bool)readOnlyLocalPlayers;
			}
		}
	}

	public void ClientSetUnready()
	{
		foreach (NetworkUser readOnlyLocalPlayers in NetworkUser.readOnlyLocalPlayersList)
		{
			readOnlyLocalPlayers.CallCmdSubmitVote(PreGameController.instance.gameObject, -1);
		}
	}
}
