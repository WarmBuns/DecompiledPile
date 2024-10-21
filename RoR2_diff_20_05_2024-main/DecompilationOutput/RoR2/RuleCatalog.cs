using System;
using System.Collections.Generic;
using System.Text;
using RoR2.ConVar;
using RoR2.EntitlementManagement;
using RoR2.ExpansionManagement;
using UnityEngine;

namespace RoR2;

public static class RuleCatalog
{
	public enum RuleCategoryType
	{
		StripVote,
		VoteResultGrid
	}

	private static readonly List<RuleDef> allRuleDefs = new List<RuleDef>();

	private static readonly List<RuleChoiceDef> allChoicesDefs = new List<RuleChoiceDef>();

	public static readonly List<RuleCategoryDef> allCategoryDefs = new List<RuleCategoryDef>();

	public static RuleCategoryDef artifactRuleCategory;

	private static RuleChoiceDef[] _allChoiceDefsWithUnlocks = Array.Empty<RuleChoiceDef>();

	private static readonly Dictionary<string, RuleDef> ruleDefsByGlobalName = new Dictionary<string, RuleDef>();

	private static readonly Dictionary<string, RuleChoiceDef> ruleChoiceDefsByGlobalName = new Dictionary<string, RuleChoiceDef>();

	public static ResourceAvailability availability;

	private static readonly BoolConVar ruleShowItems = new BoolConVar("rule_show_items", ConVarFlags.Cheat, "0", "Whether or not to allow voting on items in the pregame rules.");

	public static IEnumerable<RuleChoiceDef> allChoiceDefsWithUnlocks => _allChoiceDefsWithUnlocks;

	public static int ruleCount => allRuleDefs.Count;

	public static int choiceCount => allChoicesDefs.Count;

	public static int categoryCount => allCategoryDefs.Count;

	public static int highestLocalChoiceCount { get; private set; }

	public static RuleDef GetRuleDef(int ruleDefIndex)
	{
		return allRuleDefs[ruleDefIndex];
	}

	public static RuleDef FindRuleDef(string ruleDefGlobalName)
	{
		ruleDefsByGlobalName.TryGetValue(ruleDefGlobalName, out var value);
		return value;
	}

	public static RuleChoiceDef FindChoiceDef(string ruleChoiceDefGlobalName)
	{
		ruleChoiceDefsByGlobalName.TryGetValue(ruleChoiceDefGlobalName, out var value);
		return value;
	}

	public static RuleChoiceDef GetChoiceDef(int ruleChoiceDefIndex)
	{
		return allChoicesDefs[ruleChoiceDefIndex];
	}

	public static RuleCategoryDef GetCategoryDef(int ruleCategoryDefIndex)
	{
		return allCategoryDefs[ruleCategoryDefIndex];
	}

	private static bool HiddenTestTrue()
	{
		return true;
	}

	private static bool HiddenTestFalse()
	{
		return false;
	}

	private static bool HiddenTestItemsConvar()
	{
		return !ruleShowItems.value;
	}

	private static RuleCategoryDef AddCategory(string displayToken, string subtitleToken, Color color)
	{
		return AddCategory(displayToken, subtitleToken, color, "", null, HiddenTestFalse, RuleCategoryType.StripVote);
	}

	private static RuleCategoryDef AddCategory(string displayToken, string subtitleToken, Color color, string emptyTipToken, string editToken, Func<bool> hiddenTest, RuleCategoryType ruleCategoryType)
	{
		RuleCategoryDef ruleCategoryDef = new RuleCategoryDef
		{
			position = allRuleDefs.Count,
			displayToken = displayToken,
			subtitleToken = subtitleToken,
			color = color,
			emptyTipToken = emptyTipToken,
			editToken = editToken,
			hiddenTest = hiddenTest,
			ruleCategoryType = ruleCategoryType
		};
		allCategoryDefs.Add(ruleCategoryDef);
		return ruleCategoryDef;
	}

	private static void AddRule(RuleDef ruleDef)
	{
		if (allCategoryDefs.Count > 0)
		{
			ruleDef.category = allCategoryDefs[allCategoryDefs.Count - 1];
			allCategoryDefs[allCategoryDefs.Count - 1].children.Add(ruleDef);
		}
		allRuleDefs.Add(ruleDef);
		if (highestLocalChoiceCount < ruleDef.choices.Count)
		{
			highestLocalChoiceCount = ruleDef.choices.Count;
		}
		ruleDefsByGlobalName[ruleDef.globalName] = ruleDef;
		foreach (RuleChoiceDef choice in ruleDef.choices)
		{
			ruleChoiceDefsByGlobalName[choice.globalName] = choice;
		}
	}

	[SystemInitializer(new Type[]
	{
		typeof(ItemCatalog),
		typeof(EquipmentCatalog),
		typeof(ArtifactCatalog),
		typeof(EntitlementCatalog)
	})]
	private static void Init()
	{
		AddCategory("RULE_HEADER_DIFFICULTY", "", new Color32(43, 124, 181, byte.MaxValue));
		AddRule(RuleDef.FromDifficulty());
		AddCategory("RULE_HEADER_EXPANSIONS", "RULE_HEADER_EXPANSIONS_SUBTITLE", new Color32(219, 114, 114, byte.MaxValue), null, "RULE_HEADER_EXPANSIONS_EDIT", HiddenTestFalse, RuleCategoryType.VoteResultGrid);
		foreach (ExpansionDef expansionDef in ExpansionCatalog.expansionDefs)
		{
			AddRule(RuleDef.FromExpansion(expansionDef));
		}
		int num = 0;
		artifactRuleCategory = AddCategory("RULE_HEADER_ARTIFACTS", "RULE_HEADER_ARTIFACTS_SUBTITLE", ColorCatalog.GetColor(ColorCatalog.ColorIndex.Artifact), "RULE_ARTIFACTS_EMPTY_TIP", "RULE_HEADER_ARTIFACTS_EDIT", HiddenTestFalse, RuleCategoryType.VoteResultGrid);
		num = ArtifactCatalog.artifactCount;
		for (ArtifactIndex artifactIndex = (ArtifactIndex)0; (int)artifactIndex < num; artifactIndex++)
		{
			AddRule(RuleDef.FromArtifact(artifactIndex));
		}
		num = ItemCatalog.itemCount;
		AddCategory("RULE_HEADER_ITEMS", "RULE_HEADER_ITEMSANDEQUIPMENT_SUBTITLE", new Color32(147, 225, 128, byte.MaxValue), null, "RULE_HEADER_ITEMSANDEQUIPMENT_EDIT", HiddenTestItemsConvar, RuleCategoryType.VoteResultGrid);
		ItemIndex[] array = new ItemIndex[num];
		ItemTier[] array2 = new ItemTier[num];
		Array values = Enum.GetValues(typeof(ItemTier));
		bool[] array3 = new bool[values.Length];
		foreach (ItemTier item in values)
		{
			ItemTierDef itemTierDef = ItemTierCatalog.GetItemTierDef(item);
			if (!(itemTierDef == null))
			{
				array3[(int)item] = itemTierDef.isDroppable;
			}
		}
		int i = 0;
		for (int num2 = num; i < num2; i++)
		{
			array2[i] = ItemCatalog.GetItemDef(array[i] = (ItemIndex)i).tier;
		}
		foreach (ItemTier item2 in values)
		{
			int num3 = (int)item2;
			if (!array3[num3])
			{
				continue;
			}
			for (int j = 0; j < num; j++)
			{
				if (array2[j] == item2)
				{
					AddRule(RuleDef.FromItem(array[j]));
				}
			}
		}
		AddCategory("RULE_HEADER_EQUIPMENT", "RULE_HEADER_ITEMSANDEQUIPMENT_SUBTITLE", new Color32(byte.MaxValue, 128, 0, byte.MaxValue), null, "RULE_HEADER_ITEMSANDEQUIPMENT_EDIT", HiddenTestItemsConvar, RuleCategoryType.VoteResultGrid);
		EquipmentIndex equipmentIndex = (EquipmentIndex)0;
		for (EquipmentIndex equipmentCount = (EquipmentIndex)EquipmentCatalog.equipmentCount; equipmentIndex < equipmentCount; equipmentIndex++)
		{
			if (EquipmentCatalog.GetEquipmentDef(equipmentIndex).canDrop)
			{
				AddRule(RuleDef.FromEquipment(equipmentIndex));
			}
		}
		AddCategory("RULE_HEADER_MISC", "", new Color32(192, 192, 192, byte.MaxValue), null, "", HiddenTestFalse, RuleCategoryType.VoteResultGrid);
		RuleDef ruleDef = new RuleDef("Misc.StartingMoney", "RULE_MISC_STARTING_MONEY");
		RuleChoiceDef ruleChoiceDef = ruleDef.AddChoice("0", 0u, excludeByDefault: true);
		ruleChoiceDef.tooltipNameToken = "RULE_STARTINGMONEY_CHOICE_0_NAME";
		ruleChoiceDef.tooltipBodyToken = "RULE_STARTINGMONEY_CHOICE_0_DESC";
		ruleChoiceDef.tooltipNameColor = ColorCatalog.GetColor(ColorCatalog.ColorIndex.LunarCoin);
		ruleChoiceDef.onlyShowInGameBrowserIfNonDefault = true;
		RuleChoiceDef ruleChoiceDef2 = ruleDef.AddChoice("15", 15u, excludeByDefault: true);
		ruleChoiceDef2.tooltipNameToken = "RULE_STARTINGMONEY_CHOICE_15_NAME";
		ruleChoiceDef2.tooltipBodyToken = "RULE_STARTINGMONEY_CHOICE_15_DESC";
		ruleChoiceDef2.tooltipNameColor = ColorCatalog.GetColor(ColorCatalog.ColorIndex.LunarCoin);
		ruleChoiceDef2.onlyShowInGameBrowserIfNonDefault = true;
		ruleDef.MakeNewestChoiceDefault();
		RuleChoiceDef ruleChoiceDef3 = ruleDef.AddChoice("50", 50u, excludeByDefault: true);
		ruleChoiceDef3.tooltipNameToken = "RULE_STARTINGMONEY_CHOICE_50_NAME";
		ruleChoiceDef3.tooltipBodyToken = "RULE_STARTINGMONEY_CHOICE_50_DESC";
		ruleChoiceDef3.spritePath = "Textures/MiscIcons/texRuleBonusStartingMoney";
		ruleChoiceDef3.tooltipNameColor = ColorCatalog.GetColor(ColorCatalog.ColorIndex.LunarCoin);
		ruleChoiceDef3.onlyShowInGameBrowserIfNonDefault = true;
		AddRule(ruleDef);
		RuleDef ruleDef2 = new RuleDef("Misc.StageOrder", "RULE_MISC_STAGE_ORDER");
		RuleChoiceDef ruleChoiceDef4 = ruleDef2.AddChoice("Normal", StageOrder.Normal, excludeByDefault: true);
		ruleChoiceDef4.tooltipNameToken = "RULE_STAGEORDER_CHOICE_NORMAL_NAME";
		ruleChoiceDef4.tooltipBodyToken = "RULE_STAGEORDER_CHOICE_NORMAL_DESC";
		ruleChoiceDef4.tooltipNameColor = ColorCatalog.GetColor(ColorCatalog.ColorIndex.LunarCoin);
		ruleChoiceDef4.onlyShowInGameBrowserIfNonDefault = true;
		ruleDef2.MakeNewestChoiceDefault();
		RuleChoiceDef ruleChoiceDef5 = ruleDef2.AddChoice("Random", StageOrder.Random, excludeByDefault: true);
		ruleChoiceDef5.tooltipNameToken = "RULE_STAGEORDER_CHOICE_RANDOM_NAME";
		ruleChoiceDef5.tooltipBodyToken = "RULE_STAGEORDER_CHOICE_RANDOM_DESC";
		ruleChoiceDef5.spritePath = "Textures/MiscIcons/texRuleMapIsRandom";
		ruleChoiceDef5.tooltipNameColor = ColorCatalog.GetColor(ColorCatalog.ColorIndex.LunarCoin);
		ruleChoiceDef5.onlyShowInGameBrowserIfNonDefault = true;
		AddRule(ruleDef2);
		RuleDef ruleDef3 = new RuleDef("Misc.KeepMoneyBetweenStages", "RULE_MISC_KEEP_MONEY_BETWEEN_STAGES");
		RuleChoiceDef ruleChoiceDef6 = ruleDef3.AddChoice("On", true, excludeByDefault: true);
		ruleChoiceDef6.tooltipNameToken = "";
		ruleChoiceDef6.tooltipBodyToken = "RULE_KEEPMONEYBETWEENSTAGES_CHOICE_ON_DESC";
		ruleChoiceDef6.onlyShowInGameBrowserIfNonDefault = true;
		RuleChoiceDef ruleChoiceDef7 = ruleDef3.AddChoice("Off", false, excludeByDefault: true);
		ruleChoiceDef7.tooltipNameToken = "";
		ruleChoiceDef7.tooltipBodyToken = "RULE_KEEPMONEYBETWEENSTAGES_CHOICE_OFF_DESC";
		ruleChoiceDef7.onlyShowInGameBrowserIfNonDefault = true;
		ruleDef3.MakeNewestChoiceDefault();
		AddRule(ruleDef3);
		RuleDef ruleDef4 = new RuleDef("Misc.AllowDropIn", "RULE_MISC_ALLOW_DROP_IN");
		RuleChoiceDef ruleChoiceDef8 = ruleDef4.AddChoice("On", true, excludeByDefault: true);
		ruleChoiceDef8.tooltipNameToken = "";
		ruleChoiceDef8.tooltipBodyToken = "RULE_ALLOWDROPIN_CHOICE_ON_DESC";
		ruleChoiceDef8.onlyShowInGameBrowserIfNonDefault = true;
		RuleChoiceDef ruleChoiceDef9 = ruleDef4.AddChoice("Off", false, excludeByDefault: true);
		ruleChoiceDef9.tooltipNameToken = "";
		ruleChoiceDef9.tooltipBodyToken = "RULE_ALLOWDROPIN_CHOICE_OFF_DESC";
		ruleChoiceDef9.onlyShowInGameBrowserIfNonDefault = true;
		ruleDef4.MakeNewestChoiceDefault();
		AddRule(ruleDef4);
		int num4 = 0;
		for (int k = 0; k < allRuleDefs.Count; k++)
		{
			RuleDef ruleDef5 = allRuleDefs[k];
			ruleDef5.globalIndex = k;
			for (int l = 0; l < ruleDef5.choices.Count; l++)
			{
				RuleChoiceDef ruleChoiceDef10 = ruleDef5.choices[l];
				ruleChoiceDef10.localIndex = l;
				ruleChoiceDef10.globalIndex = allChoicesDefs.Count;
				if ((bool)ruleChoiceDef10.unlockable)
				{
					num4++;
				}
				allChoicesDefs.Add(ruleChoiceDef10);
			}
		}
		_allChoiceDefsWithUnlocks = new RuleChoiceDef[num4];
		int num5 = 0;
		foreach (RuleChoiceDef allChoicesDef in allChoicesDefs)
		{
			if ((bool)allChoicesDef.unlockable)
			{
				_allChoiceDefsWithUnlocks[num5++] = allChoicesDef;
			}
		}
		availability.MakeAvailable();
	}

	[ConCommand(commandName = "rules_dump", flags = ConVarFlags.None, helpText = "Dump information about the rules system.")]
	private static void CCRulesDump(ConCommandArgs args)
	{
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		for (int i = 0; i < ruleCount; i++)
		{
			RuleDef ruleDef = GetRuleDef(i);
			for (int j = 0; j < ruleDef.choices.Count; j++)
			{
				RuleChoiceDef ruleChoiceDef = ruleDef.choices[j];
				string item = $"  {{localChoiceIndex={ruleChoiceDef.localIndex} globalChoiceIndex={ruleChoiceDef.globalIndex} localName={ruleChoiceDef.localName}}}";
				list2.Add(item);
			}
			string text = string.Join("\n", list2);
			list2.Clear();
			string text2 = $"[{i}] {ruleDef.globalName} defaultChoiceIndex={ruleDef.defaultChoiceIndex}\n";
			list.Add(text2 + text);
		}
	}

	[ConCommand(commandName = "rules_list_choices", flags = ConVarFlags.None, helpText = "Lists all rule choices.")]
	private static void CCRulesListChoices(ConCommandArgs args)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("* = default choice.");
		stringBuilder.AppendLine();
		foreach (RuleChoiceDef allChoicesDef in allChoicesDefs)
		{
			stringBuilder.Append(allChoicesDef.globalName);
			if (allChoicesDef.isDefaultChoice)
			{
				stringBuilder.Append("*");
			}
			stringBuilder.AppendLine();
		}
	}
}
