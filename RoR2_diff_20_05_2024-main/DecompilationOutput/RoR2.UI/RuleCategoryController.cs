using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI;

[RequireComponent(typeof(MPEventSystemLocator))]
public class RuleCategoryController : MonoBehaviour
{
	[Header("Header")]
	public Image[] headerColorImages;

	public LanguageTextMeshController categoryHeaderLanguageController;

	public GameObject tipPrefab;

	public RectTransform tipContainer;

	public GameObject editCategoryButtonObject;

	[Header("Rules, Strip")]
	public GameObject stripPrefab;

	public RectTransform stripContainer;

	public RectTransform framePanel;

	[Header("Rules, Grid +  Popout Panel")]
	public RectTransform voteResultGridContainer;

	public RectTransform voteResultIconPrefab;

	public RectTransform popoutPanelIconPrefab;

	public GameObject popoutPanelPrefab;

	public RectTransform popoutPanelContainer;

	public bool displayOnlyNonDefaultResults;

	private MPEventSystemLocator eventSystemLocator;

	private readonly List<RuleDef> rulesToDisplay = new List<RuleDef>(RuleCatalog.ruleCount);

	private RuleCatalog.RuleCategoryType ruleCategoryType;

	private GameObject tipObject;

	private HGPopoutPanel popoutPanelInstance;

	private UIElementAllocator<RectTransform> voteStripAllocator;

	private UIElementAllocator<RuleChoiceController> voteResultIconAllocator;

	private UIElementAllocator<RuleChoiceController> popoutButtonIconAllocator;

	private GameObject popoutRandomButtonContainer;

	private MPButton popoutRandomButton;

	private RuleChoiceMask cachedAvailability;

	private RuleCategoryDef currentCategory;

	public RectTransform popoutPanelContentContainer => popoutPanelInstance.popoutPanelContentContainer;

	public LanguageTextMeshController popoutPanelTitleText => popoutPanelInstance.popoutPanelTitleText;

	public LanguageTextMeshController popoutPanelSubtitleText => popoutPanelInstance.popoutPanelSubtitleText;

	public LanguageTextMeshController popoutPanelDescriptionText => popoutPanelInstance.popoutPanelDescriptionText;

	public bool shouldHide
	{
		get
		{
			if ((!isEmpty || (bool)tipObject) && currentCategory != null)
			{
				return currentCategory.isHidden;
			}
			return true;
		}
	}

	public bool isEmpty
	{
		get
		{
			if (voteStripAllocator.elements.Count == 0)
			{
				return voteResultIconAllocator.elements.Count == 0;
			}
			return false;
		}
	}

	private void Awake()
	{
		eventSystemLocator = GetComponent<MPEventSystemLocator>();
		cachedAvailability = new RuleChoiceMask();
		if ((bool)popoutPanelPrefab && (bool)popoutPanelContainer && (bool)editCategoryButtonObject)
		{
			popoutPanelInstance = Object.Instantiate(popoutPanelPrefab, popoutPanelContainer).GetComponent<HGPopoutPanel>();
			ChildLocator component = popoutPanelInstance.GetComponent<ChildLocator>();
			popoutRandomButtonContainer = component?.FindChild("RandomButtonContainer")?.gameObject;
			popoutRandomButton = component?.FindChild("RandomButton")?.GetComponent<HGButton>();
			if ((bool)popoutRandomButton)
			{
				popoutRandomButton.onClick.AddListener(SetRandomVotes);
			}
			editCategoryButtonObject.GetComponent<HGButton>().onClick.AddListener(TogglePopoutPanel);
		}
		voteStripAllocator = new UIElementAllocator<RectTransform>(stripContainer, stripPrefab.gameObject);
		voteResultIconAllocator = new UIElementAllocator<RuleChoiceController>(voteResultGridContainer, voteResultIconPrefab.gameObject);
		popoutButtonIconAllocator = new UIElementAllocator<RuleChoiceController>(popoutPanelContentContainer, popoutPanelIconPrefab.gameObject);
	}

	private void TogglePopoutPanel()
	{
		if ((bool)popoutPanelInstance)
		{
			popoutPanelInstance.gameObject.SetActive(!popoutPanelInstance.gameObject.activeSelf);
		}
	}

	public void SetRandomVotes()
	{
		PreGameRuleVoteController preGameRuleVoteController = PreGameRuleVoteController.FindForUser(eventSystemLocator.eventSystem?.localUser?.currentNetworkUser);
		if (!preGameRuleVoteController)
		{
			return;
		}
		List<RuleChoiceDef> list = new List<RuleChoiceDef>();
		foreach (RuleDef child in currentCategory.children)
		{
			list.Clear();
			foreach (RuleChoiceDef choice in child.choices)
			{
				if (cachedAvailability[choice.globalIndex])
				{
					list.Add(choice);
				}
			}
			int choiceValue = -1;
			if (list.Count > 0 && Random.value > 0.5f)
			{
				choiceValue = list[Random.Range(0, list.Count)].localIndex;
			}
			preGameRuleVoteController.SetVote(child.globalIndex, choiceValue);
		}
	}

	private void SetTip(string tipToken)
	{
		if (tipToken == null)
		{
			Object.Destroy(tipObject);
			tipObject = null;
			return;
		}
		stripContainer.gameObject.SetActive(value: false);
		voteResultGridContainer.gameObject.SetActive(value: false);
		if (!tipObject)
		{
			tipObject = Object.Instantiate(tipPrefab, tipContainer);
			tipObject.SetActive(value: true);
		}
		tipObject.GetComponentInChildren<LanguageTextMeshController>().token = tipToken;
	}

	private void AllocateStrips(int desiredCount)
	{
		voteStripAllocator.AllocateElements(desiredCount);
		framePanel.SetAsLastSibling();
	}

	private void AllocateResultIcons(int desiredCount)
	{
		voteResultIconAllocator.AllocateElements(desiredCount);
	}

	public void SetData(RuleCategoryDef categoryDef, RuleChoiceMask availability, RuleBook ruleBook)
	{
		currentCategory = categoryDef;
		ruleCategoryType = categoryDef.ruleCategoryType;
		cachedAvailability.Copy(availability);
		rulesToDisplay.Clear();
		bool active = false;
		List<RuleDef> children = categoryDef.children;
		for (int i = 0; i < children.Count; i++)
		{
			RuleDef ruleDef = children[i];
			bool flag = false;
			int num = ruleDef.AvailableChoiceCount(availability);
			if (!availability[ruleDef.choices[ruleDef.defaultChoiceIndex].globalIndex] && num != 0)
			{
				flag = true;
			}
			if (num > 1)
			{
				flag = true;
				active = true;
			}
			if (ruleDef.globalName == "Difficulty")
			{
				flag = true;
			}
			if (flag || ruleDef.forceLobbyDisplay)
			{
				rulesToDisplay.Add(children[i]);
			}
		}
		Image[] array = headerColorImages;
		for (int j = 0; j < array.Length; j++)
		{
			array[j].color = categoryDef.color;
		}
		categoryHeaderLanguageController.token = categoryDef.displayToken;
		switch (ruleCategoryType)
		{
		case RuleCatalog.RuleCategoryType.StripVote:
		{
			stripContainer.gameObject.SetActive(value: true);
			voteResultGridContainer.gameObject.SetActive(value: false);
			editCategoryButtonObject.SetActive(value: false);
			AllocateStrips(rulesToDisplay.Count);
			List<RuleChoiceDef> list = new List<RuleChoiceDef>();
			for (int m = 0; m < rulesToDisplay.Count; m++)
			{
				RuleDef ruleDef4 = rulesToDisplay[m];
				list.Clear();
				foreach (RuleChoiceDef choice2 in ruleDef4.choices)
				{
					if (availability[choice2.globalIndex])
					{
						list.Add(choice2);
					}
				}
				voteStripAllocator.elements[m].GetComponent<RuleBookViewerStrip>().SetData(list, ruleBook.GetRuleChoiceIndex(ruleDef4));
			}
			break;
		}
		case RuleCatalog.RuleCategoryType.VoteResultGrid:
		{
			stripContainer.gameObject.SetActive(value: false);
			voteResultGridContainer.gameObject.SetActive(value: true);
			Color color = categoryDef.color;
			color.a = 0.2f;
			editCategoryButtonObject.SetActive(value: true);
			editCategoryButtonObject.GetComponent<HGButton>().hoverToken = Language.GetStringFormatted("RULE_EDIT_FORMAT", Language.GetString(categoryDef.displayToken), Language.GetString(categoryDef.editToken), ColorUtility.ToHtmlStringRGBA(color));
			int count = rulesToDisplay.Count;
			AllocateResultIcons(count);
			for (int k = 0; k < rulesToDisplay.Count; k++)
			{
				RuleDef ruleDef2 = rulesToDisplay[k];
				RuleChoiceController ruleChoiceController = voteResultIconAllocator.elements[k];
				int ruleChoiceIndex = ruleBook.GetRuleChoiceIndex(ruleDef2);
				RuleChoiceDef choice = ruleDef2.choices[ruleChoiceIndex];
				ruleChoiceController.SetChoice(choice);
			}
			popoutPanelTitleText.token = categoryDef.displayToken;
			popoutPanelSubtitleText.token = categoryDef.subtitleToken;
			popoutButtonIconAllocator.AllocateElements(rulesToDisplay.Count);
			GridLayoutGroup component = popoutPanelContentContainer.GetComponent<GridLayoutGroup>();
			for (int l = 0; l < rulesToDisplay.Count; l++)
			{
				RuleDef ruleDef3 = rulesToDisplay[l];
				bool num2 = ruleDef3.choices.Count == 2;
				bool flag2 = ruleDef3.AvailableChoiceCount(availability) > 1;
				int ruleChoiceIndex2 = ruleBook.GetRuleChoiceIndex(ruleDef3);
				RuleChoiceDef ruleChoiceDef = ruleDef3.choices[ruleChoiceIndex2];
				RuleChoiceController ruleChoiceController2 = popoutButtonIconAllocator.elements[l];
				ruleChoiceController2.displayVoteCounter = false;
				ruleChoiceController2.SetChoice(ruleChoiceDef);
				ruleChoiceController2.cycleThroughOptions = true;
				if (ruleChoiceDef.extraData != null && typeof(ArtifactDef).IsAssignableFrom(ruleChoiceDef.extraData.GetType()) && (bool)((ArtifactDef)ruleChoiceDef.extraData).extraUIDisplayPrefab && popoutPanelInstance.popoutAdditionalInfoContainer != null)
				{
					ruleChoiceController2.targetExtraDisplayInfoContainer = popoutPanelInstance.popoutAdditionalInfoContainer;
				}
				ruleChoiceController2.requiredTopLayer = popoutPanelInstance.GetComponent<UILayerKey>();
				ruleChoiceController2.tooltipProvider.enabled = false;
				ruleChoiceController2.hgButton.updateTextOnHover = true;
				ruleChoiceController2.hgButton.hoverLanguageTextMeshController = popoutPanelDescriptionText;
				if (component != null && rulesToDisplay.Count > 1)
				{
					UnityEngine.UI.Navigation navigation = ruleChoiceController2.hgButton.navigation;
					navigation.mode = UnityEngine.UI.Navigation.Mode.Explicit;
					int index = (l + rulesToDisplay.Count - 1) % rulesToDisplay.Count;
					navigation.selectOnLeft = popoutButtonIconAllocator.elements[index].hgButton;
					int index2 = (l + rulesToDisplay.Count + 1) % rulesToDisplay.Count;
					navigation.selectOnRight = popoutButtonIconAllocator.elements[index2].hgButton;
					if (l - component.constraintCount >= 0)
					{
						navigation.selectOnUp = popoutButtonIconAllocator.elements[l - component.constraintCount].hgButton;
					}
					if (l + component.constraintCount < rulesToDisplay.Count)
					{
						navigation.selectOnDown = popoutButtonIconAllocator.elements[l + component.constraintCount].hgButton;
					}
					ruleChoiceController2.hgButton.navigation = navigation;
				}
				if (num2 && flag2)
				{
					ruleChoiceController2.canVote = true;
				}
				else
				{
					ruleChoiceController2.canVote = false;
				}
			}
			break;
		}
		}
		SetTip(isEmpty ? categoryDef.emptyTipToken : null);
		if ((bool)popoutRandomButtonContainer)
		{
			popoutRandomButtonContainer.SetActive(active);
		}
	}
}
