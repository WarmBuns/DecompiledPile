using System.Collections.Generic;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RoR2.UI;

public class RuleChoiceController : MonoBehaviour
{
	private static readonly List<RuleChoiceController> instancesList;

	[HideInInspector]
	public RuleBookViewerStrip strip;

	public HGButton hgButton;

	public Image image;

	public TooltipProvider tooltipProvider;

	public TextMeshProUGUI voteCounter;

	public GameObject chosenDisplayObject;

	public GameObject disabledDisplayObject;

	public GameObject cantVoteDisplayObject;

	public UILayerKey requiredTopLayer;

	public bool displayVoteCounter = true;

	public bool canVote;

	public GameObject extraDisplayInfo;

	[HideInInspector]
	public GameObject targetExtraDisplayInfoContainer;

	public bool cycleThroughOptions;

	private RuleChoiceDef choiceDef;

	private void OnEnable()
	{
		instancesList.Add(this);
	}

	private void OnDisable()
	{
		instancesList.Remove(this);
	}

	static RuleChoiceController()
	{
		instancesList = new List<RuleChoiceController>();
		PreGameRuleVoteController.onVotesUpdated += delegate
		{
			foreach (RuleChoiceController instances in instancesList)
			{
				instances.UpdateFromVotes();
			}
		};
	}

	private void Start()
	{
		UpdateFromVotes();
		UpdateChoiceDisplay(choiceDef);
		if ((bool)requiredTopLayer && (bool)hgButton)
		{
			hgButton.requiredTopLayer = requiredTopLayer;
		}
	}

	public void UpdateFromVotes()
	{
		int num = PreGameRuleVoteController.votesForEachChoice[choiceDef.globalIndex];
		bool isInSinglePlayer = RoR2Application.isInSinglePlayer;
		if ((bool)voteCounter)
		{
			if (displayVoteCounter && num > 0 && !isInSinglePlayer)
			{
				voteCounter.enabled = true;
				voteCounter.text = num.ToString();
			}
			else
			{
				voteCounter.enabled = false;
			}
		}
		bool flag = false;
		NetworkUser networkUser = FindNetworkUser();
		if ((bool)networkUser)
		{
			PreGameRuleVoteController preGameRuleVoteController = PreGameRuleVoteController.FindForUser(networkUser);
			if ((bool)preGameRuleVoteController)
			{
				flag = preGameRuleVoteController.IsChoiceVoted(choiceDef);
			}
		}
		bool flag2 = choiceDef.globalName.Contains(".Off");
		if ((bool)chosenDisplayObject)
		{
			chosenDisplayObject.SetActive(flag && !flag2);
		}
		if ((bool)disabledDisplayObject)
		{
			disabledDisplayObject.SetActive(flag && flag2);
		}
		if ((bool)cantVoteDisplayObject)
		{
			cantVoteDisplayObject.SetActive(!canVote);
		}
	}

	public void SetChoice([NotNull] RuleChoiceDef newChoiceDef)
	{
		if (newChoiceDef != choiceDef)
		{
			choiceDef = newChoiceDef;
			UpdateChoiceDisplay(choiceDef);
			UpdateFromVotes();
		}
	}

	private void UpdateChoiceDisplay(RuleChoiceDef displayChoiceDef)
	{
		base.gameObject.name = "Choice (" + displayChoiceDef.globalName + ")";
		image.sprite = displayChoiceDef.sprite;
		bool flag = displayChoiceDef.extraData != null && displayChoiceDef.extraData.GetType() == typeof(ArtifactDef);
		if ((bool)tooltipProvider)
		{
			if (displayChoiceDef.tooltipNameToken == null)
			{
				Debug.LogErrorFormat("Rule choice {0} .tooltipNameToken is null", displayChoiceDef.globalName);
			}
			if (displayChoiceDef.tooltipBodyToken == null)
			{
				Debug.LogErrorFormat("Rule choice {0} .tooltipBodyToken is null", displayChoiceDef.tooltipBodyToken);
			}
			tooltipProvider.overrideTitleText = displayChoiceDef.getTooltipName(displayChoiceDef);
			tooltipProvider.titleColor = displayChoiceDef.tooltipNameColor;
			tooltipProvider.bodyToken = displayChoiceDef.tooltipBodyToken;
			tooltipProvider.bodyColor = displayChoiceDef.tooltipBodyColor;
			if (flag)
			{
				ArtifactDef artifactDef = (ArtifactDef)displayChoiceDef.extraData;
				if ((bool)artifactDef)
				{
					tooltipProvider.extraUIDisplayPrefab = artifactDef.extraUIDisplayPrefab;
				}
			}
		}
		if (!hgButton)
		{
			return;
		}
		if (hgButton.updateTextOnHover && (bool)hgButton.hoverLanguageTextMeshController)
		{
			string @string = Language.GetString(displayChoiceDef.tooltipNameToken);
			string string2 = Language.GetString(displayChoiceDef.tooltipBodyToken);
			Color tooltipNameColor = displayChoiceDef.tooltipNameColor;
			tooltipNameColor.a = 0.2f;
			string stringFormatted = Language.GetStringFormatted("RULE_DESCRIPTION_FORMAT", @string, string2, ColorUtility.ToHtmlStringRGBA(tooltipNameColor));
			hgButton.hoverToken = stringFormatted;
			hgButton.hoverLanguageTextMeshController.token = stringFormatted;
			if (flag)
			{
				ArtifactDef artifactDef2 = (ArtifactDef)displayChoiceDef.extraData;
				if ((bool)artifactDef2 && (bool)artifactDef2.extraUIDisplayPrefab && (bool)targetExtraDisplayInfoContainer && !extraDisplayInfo)
				{
					extraDisplayInfo = Object.Instantiate(artifactDef2.extraUIDisplayPrefab, targetExtraDisplayInfoContainer.transform);
					extraDisplayInfo.transform.SetSiblingIndex(targetExtraDisplayInfoContainer.transform.childCount);
					extraDisplayInfo.SetActive(value: false);
					hgButton.onSelect.AddListener(OnSelected);
					hgButton.onDeselect.AddListener(OnDeselected);
					targetExtraDisplayInfoContainer.SetActive(value: true);
					if (chosenDisplayObject.activeInHierarchy)
					{
						extraDisplayInfo.SetActive(value: true);
					}
				}
			}
		}
		hgButton.uiClickSoundOverride = choiceDef.selectionUISound;
	}

	private void OnSelected()
	{
		if ((bool)extraDisplayInfo)
		{
			extraDisplayInfo.SetActive(value: true);
		}
	}

	private void OnDeselected()
	{
		if ((bool)extraDisplayInfo && !chosenDisplayObject.activeInHierarchy)
		{
			extraDisplayInfo.SetActive(value: false);
		}
	}

	private NetworkUser FindNetworkUser()
	{
		return ((MPEventSystem)EventSystem.current).localUser?.currentNetworkUser;
	}

	public void OnClick()
	{
		if (!canVote)
		{
			return;
		}
		NetworkUser networkUser = FindNetworkUser();
		if (!networkUser)
		{
			return;
		}
		PreGameRuleVoteController preGameRuleVoteController = PreGameRuleVoteController.FindForUser(networkUser);
		if (!preGameRuleVoteController)
		{
			return;
		}
		RuleChoiceDef ruleChoiceDef;
		RuleChoiceDef ruleChoiceDef2 = (ruleChoiceDef = choiceDef);
		RuleDef ruleDef = ruleChoiceDef2.ruleDef;
		int count = ruleDef.choices.Count;
		int localIndex = ruleChoiceDef2.localIndex;
		bool flag = false;
		Debug.LogFormat("maxRuleCount={0}, currentChoiceIndex={1}", count, localIndex);
		if (cycleThroughOptions)
		{
			localIndex = (preGameRuleVoteController.IsChoiceVoted(choiceDef) ? (localIndex + 1) : 0);
			if (localIndex > count - 1)
			{
				localIndex = ruleDef.defaultChoiceIndex;
				flag = true;
			}
			ruleChoiceDef = ruleDef.choices[localIndex];
		}
		else if (preGameRuleVoteController.IsChoiceVoted(choiceDef))
		{
			flag = true;
		}
		SetChoice(ruleChoiceDef);
		preGameRuleVoteController.SetVote(ruleChoiceDef.ruleDef.globalIndex, flag ? (-1) : ruleChoiceDef.localIndex);
	}
}
