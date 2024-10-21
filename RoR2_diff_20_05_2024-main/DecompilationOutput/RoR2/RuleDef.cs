using System.Collections.Generic;
using RoR2.ExpansionManagement;
using UnityEngine;

namespace RoR2;

public class RuleDef
{
	public readonly string globalName;

	public int globalIndex;

	public readonly string displayToken;

	public readonly List<RuleChoiceDef> choices = new List<RuleChoiceDef>();

	public int defaultChoiceIndex;

	public RuleCategoryDef category;

	public bool forceLobbyDisplay;

	private const string pathToOffChoiceMaterial = "Materials/UI/matRuleChoiceOff";

	public RuleChoiceDef AddChoice(string choiceName, object extraData = null, bool excludeByDefault = false)
	{
		RuleChoiceDef ruleChoiceDef = new RuleChoiceDef();
		ruleChoiceDef.ruleDef = this;
		ruleChoiceDef.localName = choiceName;
		ruleChoiceDef.globalName = globalName + "." + choiceName;
		ruleChoiceDef.localIndex = choices.Count;
		ruleChoiceDef.extraData = extraData;
		ruleChoiceDef.excludeByDefault = excludeByDefault;
		choices.Add(ruleChoiceDef);
		return ruleChoiceDef;
	}

	public int AvailableChoiceCount(RuleChoiceMask availability)
	{
		int num = 0;
		for (int i = 0; i < choices.Count; i++)
		{
			if (availability[choices[i].globalIndex])
			{
				num++;
			}
		}
		return num;
	}

	public RuleChoiceDef FindChoice(string choiceLocalName)
	{
		int i = 0;
		for (int count = choices.Count; i < count; i++)
		{
			if (choices[i].localName == choiceLocalName)
			{
				return choices[i];
			}
		}
		return null;
	}

	public void MakeNewestChoiceDefault()
	{
		defaultChoiceIndex = choices.Count - 1;
	}

	public RuleDef(string globalName, string displayToken)
	{
		this.globalName = globalName;
		this.displayToken = displayToken;
	}

	public static RuleDef FromDifficulty()
	{
		RuleDef ruleDef = new RuleDef("Difficulty", "RULE_NAME_DIFFICULTY");
		for (DifficultyIndex difficultyIndex = DifficultyIndex.Easy; difficultyIndex < DifficultyIndex.Count; difficultyIndex++)
		{
			DifficultyDef difficultyDef = DifficultyCatalog.GetDifficultyDef(difficultyIndex);
			RuleChoiceDef ruleChoiceDef = ruleDef.AddChoice(difficultyIndex.ToString());
			ruleChoiceDef.spritePath = difficultyDef.iconPath;
			ruleChoiceDef.tooltipNameToken = difficultyDef.nameToken;
			ruleChoiceDef.tooltipNameColor = difficultyDef.color;
			ruleChoiceDef.tooltipBodyToken = difficultyDef.descriptionToken;
			ruleChoiceDef.difficultyIndex = difficultyIndex;
			ruleChoiceDef.serverTag = difficultyDef.serverTag;
			ruleChoiceDef.excludeByDefault = (int)difficultyIndex >= DifficultyCatalog.standardDifficultyCount;
		}
		ruleDef.defaultChoiceIndex = 1;
		return ruleDef;
	}

	public static RuleDef FromArtifact(ArtifactIndex artifactIndex)
	{
		ArtifactDef artifactDef = ArtifactCatalog.GetArtifactDef(artifactIndex);
		RuleDef ruleDef = new RuleDef("Artifacts." + artifactDef.cachedName, artifactDef.nameToken);
		RuleChoiceDef ruleChoiceDef = ruleDef.AddChoice("On");
		ruleChoiceDef.sprite = artifactDef.smallIconSelectedSprite;
		ruleChoiceDef.tooltipNameToken = artifactDef.nameToken;
		ruleChoiceDef.tooltipBodyToken = artifactDef.descriptionToken;
		ruleChoiceDef.unlockable = artifactDef.unlockableDef;
		ruleChoiceDef.tooltipNameColor = ColorCatalog.GetColor(ColorCatalog.ColorIndex.Artifact);
		ruleChoiceDef.artifactIndex = artifactIndex;
		ruleChoiceDef.selectionUISound = "Play_UI_artifactSelect";
		ruleChoiceDef.requiredExpansionDef = artifactDef.requiredExpansion;
		ruleChoiceDef.extraData = artifactDef;
		RuleChoiceDef ruleChoiceDef2 = ruleDef.AddChoice("Off");
		ruleChoiceDef2.sprite = artifactDef.smallIconDeselectedSprite;
		ruleChoiceDef2.tooltipNameToken = artifactDef.nameToken;
		ruleChoiceDef2.getTooltipName = RuleChoiceDef.GetOffTooltipNameFromToken;
		ruleChoiceDef2.tooltipBodyToken = artifactDef.descriptionToken;
		ruleChoiceDef2.tooltipNameColor = ColorCatalog.GetColor(ColorCatalog.ColorIndex.Unaffordable);
		ruleChoiceDef2.selectionUISound = "Play_UI_artifactDeselect";
		ruleChoiceDef2.extraData = artifactDef;
		ruleDef.MakeNewestChoiceDefault();
		return ruleDef;
	}

	public static RuleDef FromItem(ItemIndex itemIndex)
	{
		ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
		RuleDef ruleDef = new RuleDef("Items." + itemDef.name, itemDef.nameToken);
		RuleChoiceDef ruleChoiceDef = ruleDef.AddChoice("On");
		ruleChoiceDef.sprite = itemDef.pickupIconSprite;
		ruleChoiceDef.tooltipNameToken = itemDef.nameToken;
		ruleChoiceDef.tooltipBodyToken = "RULE_ITEM_ON_DESCRIPTION";
		ruleChoiceDef.unlockable = itemDef.unlockableDef;
		ruleChoiceDef.itemIndex = itemIndex;
		ruleChoiceDef.onlyShowInGameBrowserIfNonDefault = true;
		ruleChoiceDef.requiredExpansionDef = itemDef.requiredExpansion;
		ruleDef.MakeNewestChoiceDefault();
		RuleChoiceDef ruleChoiceDef2 = ruleDef.AddChoice("Off");
		ruleChoiceDef2.spritePath = "Textures/MiscIcons/texUnlockIcon";
		ruleChoiceDef2.tooltipNameToken = itemDef.nameToken;
		ruleChoiceDef2.getTooltipName = RuleChoiceDef.GetOffTooltipNameFromToken;
		ruleChoiceDef2.tooltipBodyToken = "RULE_ITEM_OFF_DESCRIPTION";
		ruleChoiceDef2.onlyShowInGameBrowserIfNonDefault = true;
		return ruleDef;
	}

	public static RuleDef FromEquipment(EquipmentIndex equipmentIndex)
	{
		EquipmentDef equipmentDef = EquipmentCatalog.GetEquipmentDef(equipmentIndex);
		RuleDef ruleDef = new RuleDef("Equipment." + equipmentDef.name, equipmentDef.nameToken);
		RuleChoiceDef ruleChoiceDef = ruleDef.AddChoice("On");
		ruleChoiceDef.sprite = equipmentDef.pickupIconSprite;
		ruleChoiceDef.tooltipNameToken = equipmentDef.nameToken;
		ruleChoiceDef.tooltipBodyToken = "RULE_ITEM_ON_DESCRIPTION";
		ruleChoiceDef.unlockable = equipmentDef.unlockableDef;
		ruleChoiceDef.equipmentIndex = equipmentIndex;
		ruleChoiceDef.availableInMultiPlayer = equipmentDef.appearsInMultiPlayer;
		ruleChoiceDef.availableInSinglePlayer = equipmentDef.appearsInSinglePlayer;
		ruleChoiceDef.onlyShowInGameBrowserIfNonDefault = true;
		ruleChoiceDef.requiredExpansionDef = equipmentDef.requiredExpansion;
		ruleDef.MakeNewestChoiceDefault();
		RuleChoiceDef ruleChoiceDef2 = ruleDef.AddChoice("Off");
		ruleChoiceDef2.sprite = equipmentDef.pickupIconSprite;
		ruleChoiceDef2.tooltipNameToken = equipmentDef.nameToken;
		ruleChoiceDef2.getTooltipName = RuleChoiceDef.GetOffTooltipNameFromToken;
		ruleChoiceDef2.tooltipBodyToken = "RULE_ITEM_OFF_DESCRIPTION";
		ruleChoiceDef2.onlyShowInGameBrowserIfNonDefault = true;
		return ruleDef;
	}

	public static RuleDef FromExpansion(ExpansionDef expansionDef)
	{
		RuleDef obj = new RuleDef("Expansions." + expansionDef.name, expansionDef.nameToken)
		{
			forceLobbyDisplay = true
		};
		RuleChoiceDef ruleChoiceDef = obj.AddChoice("On", expansionDef);
		ruleChoiceDef.sprite = expansionDef.iconSprite;
		ruleChoiceDef.tooltipNameToken = expansionDef.nameToken;
		ruleChoiceDef.tooltipNameColor = new Color32(219, 114, 114, byte.MaxValue);
		ruleChoiceDef.tooltipBodyToken = expansionDef.descriptionToken;
		ruleChoiceDef.requiredEntitlementDef = expansionDef.requiredEntitlement;
		obj.MakeNewestChoiceDefault();
		expansionDef.enabledChoice = ruleChoiceDef;
		RuleChoiceDef ruleChoiceDef2 = obj.AddChoice("Off");
		ruleChoiceDef2.sprite = expansionDef.disabledIconSprite;
		ruleChoiceDef2.tooltipNameToken = expansionDef.nameToken;
		ruleChoiceDef2.tooltipNameColor = ColorCatalog.GetColor(ColorCatalog.ColorIndex.Unaffordable);
		ruleChoiceDef2.getTooltipName = RuleChoiceDef.GetOffTooltipNameFromToken;
		ruleChoiceDef2.tooltipBodyToken = expansionDef.descriptionToken;
		return obj;
	}
}
