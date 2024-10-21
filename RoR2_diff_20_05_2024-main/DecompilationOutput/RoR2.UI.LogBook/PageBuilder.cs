using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Text;
using HG;
using RoR2.Stats;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI.LogBook;

public class PageBuilder
{
	private static readonly StringBuilder sharedStringBuilder = new StringBuilder();

	public UserProfile userProfile;

	public RectTransform container;

	public Entry entry;

	public readonly List<GameObject> managedObjects = new List<GameObject>();

	private StatSheet statSheet => userProfile.statSheet;

	public void Destroy()
	{
		foreach (GameObject managedObject in managedObjects)
		{
			UnityEngine.Object.Destroy(managedObject);
		}
	}

	public void AddSimpleTextPanel(string text)
	{
		AddPrefabInstance(LegacyResourcesAPI.Load<GameObject>("Prefabs/UI/Logbook/SimpleTextPanel")).GetComponent<ChildLocator>().FindChild("MainLabel").GetComponent<TextMeshProUGUI>()
			.text = text;
	}

	public GameObject AddPrefabInstance(GameObject prefab)
	{
		GameObject gameObject = UnityEngine.Object.Instantiate(prefab, container);
		managedObjects.Add(gameObject);
		return gameObject;
	}

	public void AddSimpleTextPanel(params string[] textLines)
	{
		AddSimpleTextPanel(string.Join("\n", textLines));
	}

	public void AddSimplePickup(PickupIndex pickupIndex)
	{
		PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
		ItemIndex itemIndex = pickupDef?.itemIndex ?? ItemIndex.None;
		EquipmentIndex equipmentIndex = pickupDef?.equipmentIndex ?? EquipmentIndex.None;
		string token = null;
		if (itemIndex != ItemIndex.None)
		{
			ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
			AddDescriptionPanel(Language.GetString(itemDef.descriptionToken));
			token = itemDef.loreToken;
			ulong statValueULong = statSheet.GetStatValueULong(PerItemStatDef.totalCollected.FindStatDef(itemIndex));
			ulong statValueULong2 = statSheet.GetStatValueULong(PerItemStatDef.highestCollected.FindStatDef(itemIndex));
			string stringFormatted = Language.GetStringFormatted("GENERIC_PREFIX_FOUND", statValueULong);
			string stringFormatted2 = Language.GetStringFormatted("ITEM_PREFIX_STACKCOUNT", statValueULong2);
			AddSimpleTextPanel(stringFormatted, stringFormatted2);
		}
		else if (equipmentIndex != EquipmentIndex.None)
		{
			EquipmentDef equipmentDef = EquipmentCatalog.GetEquipmentDef(equipmentIndex);
			AddDescriptionPanel(Language.GetString(equipmentDef.descriptionToken));
			token = equipmentDef.loreToken;
			string stringFormatted3 = Language.GetStringFormatted("EQUIPMENT_PREFIX_COOLDOWN", equipmentDef.cooldown);
			string stringFormatted4 = Language.GetStringFormatted("EQUIPMENT_PREFIX_TOTALTIMEHELD", statSheet.GetStatDisplayValue(PerEquipmentStatDef.totalTimeHeld.FindStatDef(equipmentIndex)));
			string stringFormatted5 = Language.GetStringFormatted("EQUIPMENT_PREFIX_USECOUNT", statSheet.GetStatDisplayValue(PerEquipmentStatDef.totalTimesFired.FindStatDef(equipmentIndex)));
			AddSimpleTextPanel(stringFormatted3);
			AddSimpleTextPanel(stringFormatted4, stringFormatted5);
		}
		AddNotesPanel(Language.IsTokenInvalid(token) ? Language.GetString("EARLY_ACCESS_LORE") : Language.GetString(token));
	}

	public void AddDescriptionPanel(string content)
	{
		AddSimpleTextPanel(Language.GetStringFormatted("DESCRIPTION_PREFIX_FORMAT", content));
	}

	public void AddNotesPanel(string content)
	{
		AddSimpleTextPanel(Language.GetStringFormatted("NOTES_PREFIX_FORMAT", content));
	}

	public void AddBodyStatsPanel(CharacterBody bodyPrefabComponent)
	{
		float baseMaxHealth = bodyPrefabComponent.baseMaxHealth;
		float levelMaxHealth = bodyPrefabComponent.levelMaxHealth;
		float baseDamage = bodyPrefabComponent.baseDamage;
		float levelDamage = bodyPrefabComponent.levelDamage;
		float baseArmor = bodyPrefabComponent.baseArmor;
		float baseRegen = bodyPrefabComponent.baseRegen;
		float levelRegen = bodyPrefabComponent.levelRegen;
		float baseMoveSpeed = bodyPrefabComponent.baseMoveSpeed;
		AddSimpleTextPanel(Language.GetStringFormatted("BODY_HEALTH_FORMAT", Language.GetStringFormatted("BODY_STATS_FORMAT", baseMaxHealth.ToString(), levelMaxHealth.ToString())) + "\n" + Language.GetStringFormatted("BODY_DAMAGE_FORMAT", Language.GetStringFormatted("BODY_STATS_FORMAT", baseDamage.ToString(), levelDamage.ToString())) + "\n" + ((baseRegen >= Mathf.Epsilon) ? (Language.GetStringFormatted("BODY_REGEN_FORMAT", Language.GetStringFormatted("BODY_STATS_FORMAT", baseRegen.ToString(), levelRegen.ToString())) + "\n") : "") + Language.GetStringFormatted("BODY_MOVESPEED_FORMAT", baseMoveSpeed) + "\n" + Language.GetStringFormatted("BODY_ARMOR_FORMAT", baseArmor.ToString()));
	}

	public void AddMonsterPanel(CharacterBody bodyPrefabComponent)
	{
		ulong statValueULong = statSheet.GetStatValueULong(PerBodyStatDef.killsAgainst, bodyPrefabComponent.gameObject.name);
		ulong statValueULong2 = statSheet.GetStatValueULong(PerBodyStatDef.killsAgainstElite, bodyPrefabComponent.gameObject.name);
		ulong statValueULong3 = statSheet.GetStatValueULong(PerBodyStatDef.deathsFrom, bodyPrefabComponent.gameObject.name);
		string stringFormatted = Language.GetStringFormatted("MONSTER_PREFIX_KILLED", statValueULong);
		string stringFormatted2 = Language.GetStringFormatted("MONSTER_PREFIX_ELITESKILLED", statValueULong2);
		string stringFormatted3 = Language.GetStringFormatted("MONSTER_PREFIX_DEATH", statValueULong3);
		AddSimpleTextPanel(stringFormatted, stringFormatted2, stringFormatted3);
	}

	public void AddSurvivorPanel(CharacterBody bodyPrefabComponent)
	{
		string statDisplayValue = statSheet.GetStatDisplayValue(PerBodyStatDef.longestRun.FindStatDef(bodyPrefabComponent.name));
		ulong statValueULong = statSheet.GetStatValueULong(PerBodyStatDef.timesPicked.FindStatDef(bodyPrefabComponent.name));
		ulong statValueULong2 = statSheet.GetStatValueULong(StatDef.totalGamesPlayed);
		double num = 0.0;
		if (statValueULong2 != 0L)
		{
			num = (double)statValueULong / (double)statValueULong2 * 100.0;
		}
		sharedStringBuilder.Clear();
		sharedStringBuilder.AppendLine(Language.GetStringFormatted("SURVIVOR_PREFIX_LONGESTRUN", statDisplayValue));
		sharedStringBuilder.AppendLine(Language.GetStringFormatted("SURVIVOR_PREFIX_TIMESPICKED", statValueULong));
		sharedStringBuilder.AppendLine(Language.GetStringFormatted("SURVIVOR_PREFIX_PICKPERCENTAGE", num));
		AddSimpleTextPanel(sharedStringBuilder.ToString());
	}

	public void AddSimpleBody(CharacterBody bodyPrefabComponent)
	{
		AddBodyStatsPanel(bodyPrefabComponent);
	}

	public void AddBodyLore(CharacterBody characterBody)
	{
		bool flag = false;
		string token = "";
		string baseNameToken = characterBody.baseNameToken;
		if (!string.IsNullOrEmpty(baseNameToken))
		{
			token = baseNameToken.Replace("_NAME", "_LORE");
			if (!Language.IsTokenInvalid(token))
			{
				flag = true;
			}
		}
		if (flag)
		{
			AddNotesPanel(Language.GetString(token));
		}
		else
		{
			AddNotesPanel(Language.GetString("EARLY_ACCESS_LORE"));
		}
	}

	public void AddStagePanel(SceneDef sceneDef)
	{
		string statDisplayValue = userProfile.statSheet.GetStatDisplayValue(PerStageStatDef.totalTimesVisited.FindStatDef(sceneDef.baseSceneName));
		string statDisplayValue2 = userProfile.statSheet.GetStatDisplayValue(PerStageStatDef.totalTimesCleared.FindStatDef(sceneDef.baseSceneName));
		string stringFormatted = Language.GetStringFormatted("STAGE_PREFIX_TOTALTIMESVISITED", statDisplayValue);
		string stringFormatted2 = Language.GetStringFormatted("STAGE_PREFIX_TOTALTIMESCLEARED", statDisplayValue2);
		sharedStringBuilder.Clear();
		sharedStringBuilder.Append(stringFormatted);
		sharedStringBuilder.Append("\n");
		sharedStringBuilder.Append(stringFormatted2);
		AddSimpleTextPanel(sharedStringBuilder.ToString());
	}

	public void AddPieChart(PieChartMeshController.SliceInfo[] sliceInfos)
	{
		GameObject gameObject = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/UI/PieChartPanel"), container);
		gameObject.GetComponent<PieChartMeshController>().SetSlices(sliceInfos);
		managedObjects.Add(gameObject);
	}

	public static void Stage(PageBuilder builder)
	{
		SceneDef sceneDef = (SceneDef)builder.entry.extraData;
		builder.AddStagePanel(sceneDef);
		builder.AddNotesPanel(Language.IsTokenInvalid(sceneDef.loreToken) ? Language.GetString("EARLY_ACCESS_LORE") : Language.GetString(sceneDef.loreToken));
	}

	public static void SimplePickup(PageBuilder builder)
	{
		builder.AddSimplePickup((PickupIndex)builder.entry.extraData);
	}

	public static void SimpleBody(PageBuilder builder)
	{
		builder.AddSimpleBody((CharacterBody)builder.entry.extraData);
	}

	public static void MonsterBody(PageBuilder builder)
	{
		CharacterBody characterBody = (CharacterBody)builder.entry.extraData;
		builder.AddSimpleBody(characterBody);
		builder.AddMonsterPanel(characterBody);
		builder.AddBodyLore(characterBody);
	}

	public static void SurvivorBody(PageBuilder builder)
	{
		CharacterBody characterBody = (CharacterBody)builder.entry.extraData;
		builder.AddSimpleBody(characterBody);
		builder.AddSurvivorPanel(characterBody);
		builder.AddBodyLore(characterBody);
	}

	public static void StatsPanel(PageBuilder builder)
	{
		UserProfile userProfile = (UserProfile)builder.entry.extraData;
		GameCompletionStatsHelper gameCompletionStatsHelper = new GameCompletionStatsHelper();
		StatSheet statSheet = userProfile.statSheet;
		CalcAllBodyStatTotalDouble(PerBodyStatDef.totalTimeAlive);
		_ = (double)CalcAllBodyStatTotalULong(PerBodyStatDef.timesPicked);
		double value = (double)CalcAllBodyStatTotalULong(PerBodyStatDef.totalWins);
		_ = (double)CalcAllBodyStatTotalULong(PerBodyStatDef.deathsAs);
		ChildLocator component = builder.AddPrefabInstance(LegacyResourcesAPI.Load<GameObject>("Prefabs/UI/Logbook/ProfileStatsPanel")).GetComponent<ChildLocator>();
		RectTransform rectTransform = (RectTransform)component.FindChild("CharacterPieChart");
		RectTransform rectTransform2 = (RectTransform)component.FindChild("CompletionBarPanel");
		RectTransform rectTransform3 = (RectTransform)component.FindChild("CompletionLabel");
		RectTransform rectTransform4 = (RectTransform)component.FindChild("CharacterStatsCarousel");
		RectTransform rectTransform5 = (RectTransform)component.FindChild("TotalsStatsList");
		RectTransform rectTransform6 = (RectTransform)component.FindChild("RecordsStatsList");
		RectTransform rectTransform7 = (RectTransform)component.FindChild("MiscStatsList");
		RectTransform rectTransform8 = (RectTransform)component.FindChild("CompletionStatsList");
		PieChartMeshController characterPieChartMeshController = rectTransform.GetComponent<PieChartMeshController>();
		CarouselNavigationController carousel = rectTransform4.GetComponent<CarouselNavigationController>();
		List<string> statNames = new List<string>();
		List<Action> callbacks = new List<Action>();
		AddPerBodyStat(PerBodyStatDef.totalWins);
		AddPerBodyStat(PerBodyStatDef.timesPicked);
		AddPerBodyStat(PerBodyStatDef.totalTimeAlive);
		AddPerBodyStat(PerBodyStatDef.longestRun);
		AddPerBodyStat(PerBodyStatDef.deathsAs);
		AddPerBodyStat(PerBodyStatDef.damageDealtAs);
		AddPerBodyStat(PerBodyStatDef.damageTakenAs);
		AddPerBodyStat(PerBodyStatDef.damageDealtTo);
		AddPerBodyStat(PerBodyStatDef.damageTakenFrom);
		AddPerBodyStat(PerBodyStatDef.killsAgainst);
		AddPerBodyStat(PerBodyStatDef.killsAgainstElite);
		AddPerBodyStat(PerBodyStatDef.deathsFrom);
		AddPerBodyStat(PerBodyStatDef.minionDamageDealtAs);
		AddPerBodyStat(PerBodyStatDef.minionKillsAs);
		AddPerBodyStat(PerBodyStatDef.killsAs);
		carousel.onPageChangeSubmitted += OnPageChangeSubmitted;
		carousel.SetDisplayData(new CarouselNavigationController.DisplayData(statNames.Count, 0));
		OnPageChangeSubmitted(0);
		GameObject statStripPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/UI/Logbook/LogbookStatStrip");
		StatDef statDef2 = PerBodyStatDef.longestRun.FindStatDef(statSheet.FindBodyWithHighestStat(PerBodyStatDef.longestRun)) ?? PerBodyStatDef.longestRun.FindStatDef(BodyCatalog.FindBodyIndex("CommandoBody"));
		CharacterBody bodyPrefabBodyComponent = BodyCatalog.GetBodyPrefabBodyComponent(statSheet.FindBodyWithHighestStat(PerBodyStatDef.deathsFrom));
		EquipmentDef equipmentDef = EquipmentCatalog.GetEquipmentDef(statSheet.FindEquipmentWithHighestStat(PerEquipmentStatDef.totalTimeHeld));
		(string, string, Texture)[] array = new(string, string, Texture)[20];
		(string, string, Texture2D) tuple = StatStripDataFromStatDef(StatDef.totalGamesPlayed);
		array[0] = (tuple.Item1, tuple.Item2, tuple.Item3);
		tuple = StatStripDataFromStatDef(StatDef.totalTimeAlive);
		array[1] = (tuple.Item1, tuple.Item2, tuple.Item3);
		tuple = StatStripDataFromStatDef(StatDef.totalDeaths);
		array[2] = (tuple.Item1, tuple.Item2, tuple.Item3);
		array[3] = (Language.GetString("STATNAME_TOTALWINS"), TextSerialization.ToStringNumeric(value), null);
		tuple = StatStripDataFromStatDef(StatDef.totalKills);
		array[4] = (tuple.Item1, tuple.Item2, tuple.Item3);
		tuple = StatStripDataFromStatDef(StatDef.totalEliteKills);
		array[5] = (tuple.Item1, tuple.Item2, tuple.Item3);
		tuple = StatStripDataFromStatDef(StatDef.totalDamageDealt);
		array[6] = (tuple.Item1, tuple.Item2, tuple.Item3);
		tuple = StatStripDataFromStatDef(StatDef.totalStagesCompleted);
		array[7] = (tuple.Item1, tuple.Item2, tuple.Item3);
		tuple = StatStripDataFromStatDef(StatDef.totalDamageTaken);
		array[8] = (tuple.Item1, tuple.Item2, tuple.Item3);
		tuple = StatStripDataFromStatDef(StatDef.totalHealthHealed);
		array[9] = (tuple.Item1, tuple.Item2, tuple.Item3);
		tuple = StatStripDataFromStatDef(StatDef.goldCollected);
		array[10] = (tuple.Item1, tuple.Item2, tuple.Item3);
		tuple = StatStripDataFromStatDef(StatDef.totalDistanceTraveled);
		array[11] = (tuple.Item1, tuple.Item2, tuple.Item3);
		tuple = StatStripDataFromStatDef(StatDef.totalPurchases);
		array[12] = (tuple.Item1, tuple.Item2, tuple.Item3);
		tuple = StatStripDataFromStatDef(StatDef.totalBloodPurchases);
		array[13] = (tuple.Item1, tuple.Item2, tuple.Item3);
		tuple = StatStripDataFromStatDef(StatDef.totalDronesPurchased);
		array[14] = (tuple.Item1, tuple.Item2, tuple.Item3);
		tuple = StatStripDataFromStatDef(StatDef.totalTurretsPurchased);
		array[15] = (tuple.Item1, tuple.Item2, tuple.Item3);
		tuple = StatStripDataFromStatDef(StatDef.totalCrocoInfectionsInflicted);
		array[16] = (tuple.Item1, tuple.Item2, tuple.Item3);
		tuple = StatStripDataFromStatDef(StatDef.totalMinionDamageDealt);
		array[17] = (tuple.Item1, tuple.Item2, tuple.Item3);
		tuple = StatStripDataFromStatDef(StatDef.totalMinionKills);
		array[18] = (tuple.Item1, tuple.Item2, tuple.Item3);
		tuple = StatStripDataFromStatDef(StatDef.totalDeathsWhileBurning);
		array[19] = (tuple.Item1, tuple.Item2, tuple.Item3);
		SetStats(rectTransform5, array);
		(string, string, Texture)[] obj = new(string, string, Texture)[6]
		{
			(Language.GetString("STATNAME_LONGESTRUN"), statSheet.GetStatDisplayValue(statDef2), null),
			default((string, string, Texture)),
			default((string, string, Texture)),
			default((string, string, Texture)),
			default((string, string, Texture)),
			default((string, string, Texture))
		};
		tuple = StatStripDataFromStatDef(StatDef.highestStagesCompleted);
		obj[1] = (tuple.Item1, tuple.Item2, tuple.Item3);
		tuple = StatStripDataFromStatDef(StatDef.highestLevel);
		obj[2] = (tuple.Item1, tuple.Item2, tuple.Item3);
		tuple = StatStripDataFromStatDef(StatDef.highestDamageDealt);
		obj[3] = (tuple.Item1, tuple.Item2, tuple.Item3);
		tuple = StatStripDataFromStatDef(StatDef.maxGoldCollected);
		obj[4] = (tuple.Item1, tuple.Item2, tuple.Item3);
		tuple = StatStripDataFromStatDef(StatDef.highestPurchases);
		obj[5] = (tuple.Item1, tuple.Item2, tuple.Item3);
		SetStats(rectTransform6, obj);
		SetStats(rectTransform7, new(string, string, Texture)[2]
		{
			(Language.GetString("STATNAME_NEMESIS"), bodyPrefabBodyComponent ? Language.GetString(bodyPrefabBodyComponent.baseNameToken) : string.Empty, bodyPrefabBodyComponent ? bodyPrefabBodyComponent.portraitIcon : null),
			(Language.GetString("STATNAME_FAVORITEEQUIPMENT"), (equipmentDef != null) ? Language.GetString(equipmentDef.nameToken) : string.Empty, equipmentDef?.pickupIconTexture)
		});
		(string, string, Texture)[] array2 = new(string, string, Texture)[5];
		tuple = StatStripDataFromCompletionFraction("STATNAME_COMPLETION_ACHIEVEMENTS", gameCompletionStatsHelper.GetAchievementCompletion(userProfile));
		array2[0] = (tuple.Item1, tuple.Item2, tuple.Item3);
		tuple = StatStripDataFromCompletionFraction("STATNAME_COMPLETION_COLLECTIBLES", gameCompletionStatsHelper.GetCollectibleCompletion(userProfile));
		array2[1] = (tuple.Item1, tuple.Item2, tuple.Item3);
		tuple = StatStripDataFromCompletionFraction("STATNAME_COMPLETION_PICKUPDISCOVERY", gameCompletionStatsHelper.GetPickupEncounterCompletion(userProfile));
		array2[2] = (tuple.Item1, tuple.Item2, tuple.Item3);
		tuple = StatStripDataFromCompletionFraction("STATNAME_COMPLETION_SURVIVORSPICKED", gameCompletionStatsHelper.GetSurvivorPickCompletion(userProfile));
		array2[3] = (tuple.Item1, tuple.Item2, tuple.Item3);
		tuple = StatStripDataFromCompletionFraction("STATNAME_COMPLETION_SURVIVORSWON", gameCompletionStatsHelper.GetSurvivorWinCompletion(userProfile));
		array2[4] = (tuple.Item1, tuple.Item2, tuple.Item3);
		SetStats(rectTransform8, array2);
		float num = (float)gameCompletionStatsHelper.GetTotalCompletion(userProfile);
		UnityEngine.Vector2 anchorMax = rectTransform2.anchorMax;
		anchorMax.x = num;
		rectTransform2.anchorMax = anchorMax;
		rectTransform3.GetComponent<TMP_Text>().SetText($"{num:0%}");
		void AddLine(StatDef statDef, string statNameToken, double? allBodyTotal)
		{
			string @string = Language.GetString("STAT_NAME_VALUE_FORMAT");
			string statDisplayValue = statSheet.GetStatDisplayValue(statDef);
			P_3.bodyTextStringBuilder.AppendFormat(@string, Language.GetString(statNameToken), statDisplayValue);
			if (allBodyTotal.HasValue)
			{
				double statValueAsDouble = statSheet.GetStatValueAsDouble(statDef);
				double num5 = 0.0;
				if (allBodyTotal != 0.0)
				{
					num5 = statValueAsDouble / allBodyTotal.Value;
				}
				P_3.bodyTextStringBuilder.Append(" ").AppendFormat(P_3.rateFormat, num5);
			}
			P_3.bodyTextStringBuilder.AppendLine();
		}
		void AddLineFromPerBodyStat(PerBodyStatDef perBodyStatDef, double? total)
		{
			if (!total.HasValue)
			{
				total = CalcAllBodyStatTotalDouble(perBodyStatDef);
			}
			StatDef statDef3 = perBodyStatDef.FindStatDef(P_2.bodyIndex);
			AddLine(statDef3, perBodyStatDef.nameToken, total);
		}
		void AddPerBodyStat(PerBodyStatDef perBodyStatDef)
		{
			statNames.Add(Language.GetString(perBodyStatDef.nameToken));
			callbacks.Add(Callback);
			void Callback()
			{
				BuildCharacterPieChart(characterPieChartMeshController, (BodyIndex bodyIndex) => GetStatWeight(perBodyStatDef.FindStatDef(bodyIndex)));
			}
		}
		TooltipContent BuildBodyTooltipContent(BodyIndex bodyIndex, Color bodyColor)
		{
			CharacterBody bodyPrefabBodyComponent3 = BodyCatalog.GetBodyPrefabBodyComponent(bodyIndex);
			StringBuilder bodyTextStringBuilder = HG.StringBuilderPool.RentStringBuilder();
			string rateFormat = Language.GetString("PERCENT_FORMAT_PARENTHESES");
			AddLineFromPerBodyStat(PerBodyStatDef.timesPicked, null);
			AddLineFromPerBodyStat(PerBodyStatDef.totalTimeAlive, null);
			AddLineFromPerBodyStat(PerBodyStatDef.longestRun, null);
			AddLineFromPerBodyStat(PerBodyStatDef.totalWins, null);
			AddLineFromPerBodyStat(PerBodyStatDef.deathsAs, null);
			AddLineFromPerBodyStat(PerBodyStatDef.damageDealtAs, null);
			AddLineFromPerBodyStat(PerBodyStatDef.damageTakenAs, null);
			AddLineFromPerBodyStat(PerBodyStatDef.damageDealtTo, null);
			AddLineFromPerBodyStat(PerBodyStatDef.damageTakenFrom, null);
			AddLineFromPerBodyStat(PerBodyStatDef.killsAgainst, null);
			AddLineFromPerBodyStat(PerBodyStatDef.killsAgainstElite, null);
			AddLineFromPerBodyStat(PerBodyStatDef.deathsFrom, null);
			AddLineFromPerBodyStat(PerBodyStatDef.minionDamageDealtAs, null);
			AddLineFromPerBodyStat(PerBodyStatDef.minionKillsAs, null);
			AddLineFromPerBodyStat(PerBodyStatDef.killsAs, null);
			TooltipContent tooltipContent = default(TooltipContent);
			tooltipContent.titleToken = bodyPrefabBodyComponent3.baseNameToken;
			tooltipContent.titleColor = bodyColor;
			tooltipContent.overrideBodyText = bodyTextStringBuilder.ToString();
			TooltipContent result2 = tooltipContent;
			HG.StringBuilderPool.ReturnStringBuilder(bodyTextStringBuilder);
			return result2;
		}
		void BuildCharacterPieChart(PieChartMeshController pieChartMeshController, Func<BodyIndex, float> bodyWeightGetter)
		{
			List<PieChartMeshController.SliceInfo> list = new List<PieChartMeshController.SliceInfo>();
			for (BodyIndex bodyIndex4 = (BodyIndex)0; (int)bodyIndex4 < BodyCatalog.bodyCount; bodyIndex4++)
			{
				float num3 = bodyWeightGetter(bodyIndex4);
				if (num3 != 0f)
				{
					PieChartMeshController.SliceInfo item = default(PieChartMeshController.SliceInfo);
					item.color = GetBodyColor(bodyIndex4);
					item.weight = num3;
					item.tooltipContent = BuildBodyTooltipContent(bodyIndex4, item.color);
					list.Add(item);
				}
			}
			pieChartMeshController.SetSlices(list.OrderBy((PieChartMeshController.SliceInfo slice) => 0f - slice.weight).ToArray());
		}
		double CalcAllBodyStatTotalDouble(PerBodyStatDef perBodyStatDef)
		{
			double num2 = 0.0;
			for (BodyIndex bodyIndex2 = (BodyIndex)0; (int)bodyIndex2 < BodyCatalog.bodyCount; bodyIndex2++)
			{
				num2 += statSheet.GetStatValueAsDouble(perBodyStatDef.FindStatDef(bodyIndex2));
			}
			return num2;
		}
		BigInteger CalcAllBodyStatTotalULong(PerBodyStatDef perBodyStatDef)
		{
			BigInteger result = 0;
			for (BodyIndex bodyIndex3 = (BodyIndex)0; (int)bodyIndex3 < BodyCatalog.bodyCount; bodyIndex3++)
			{
				result += (BigInteger)statSheet.GetStatValueULong(perBodyStatDef.FindStatDef(bodyIndex3));
			}
			return result;
		}
		static Color GetBodyColor(BodyIndex bodyIndex)
		{
			CharacterBody bodyPrefabBodyComponent2 = BodyCatalog.GetBodyPrefabBodyComponent(bodyIndex);
			if (bodyPrefabBodyComponent2.bodyColor != Color.clear)
			{
				return bodyPrefabBodyComponent2.bodyColor;
			}
			string bodyName = BodyCatalog.GetBodyName(bodyIndex);
			ulong num4 = 0uL;
			for (int i = 0; i < bodyName.Length; i++)
			{
				num4 += bodyName[i];
			}
			Xoroshiro128Plus xoroshiro128Plus = new Xoroshiro128Plus(num4);
			return Color.HSVToRGB(xoroshiro128Plus.nextNormalizedFloat, xoroshiro128Plus.RangeFloat(0.5f, 1f), xoroshiro128Plus.RangeFloat(0.6f, 0.8f));
		}
		float GetStatWeight(StatDef statDef)
		{
			return statDef.dataType switch
			{
				StatDataType.ULong => statSheet.GetStatValueULong(statDef), 
				StatDataType.Double => (float)statSheet.GetStatValueDouble(statDef), 
				_ => throw new ArgumentOutOfRangeException(), 
			};
		}
		void OnPageChangeSubmitted(int newPage)
		{
			callbacks[newPage]();
			carousel.GetComponent<ChildLocator>().FindChild("StatLabel").GetComponent<HGTextMeshProUGUI>()
				.text = statNames[newPage];
		}
		void SetStats(RectTransform container, (string name, string value, Texture texture)[] data)
		{
			UIElementAllocator<ChildLocator> uIElementAllocator = new UIElementAllocator<ChildLocator>(container, statStripPrefab, markElementsUnsavable: true, acquireExistingChildren: true);
			uIElementAllocator.AllocateElements(data.Length);
			ReadOnlyCollection<ChildLocator> elements = uIElementAllocator.elements;
			for (int j = 0; j < data.Length; j++)
			{
				(string name, string value, Texture texture) tuple2 = data[j];
				string item2 = tuple2.name;
				string item3 = tuple2.value;
				Texture item4 = tuple2.texture;
				ChildLocator childLocator = elements[j];
				childLocator.FindChild("NameLabel").GetComponent<TMP_Text>().SetText(item2);
				childLocator.FindChild("ValueLabel").GetComponent<TMP_Text>().SetText("<color=#FFFF7F>" + item3 + "</color>");
				RawImage component2 = childLocator.FindChild("IconRawImage").GetComponent<RawImage>();
				component2.transform.parent.gameObject.SetActive(item4);
				component2.texture = item4;
			}
		}
		static (string name, string value, Texture2D texture) StatStripDataFromCompletionFraction(string displayToken, IntFraction completionFraction)
		{
			(string, string, Texture2D) result4 = default((string, string, Texture2D));
			result4.Item1 = Language.GetString(displayToken);
			result4.Item2 = Language.GetStringFormatted("STAT_COMPLETION_VALUE_FORMAT", completionFraction.numerator, completionFraction.denominator, (float)completionFraction);
			result4.Item3 = null;
			return result4;
		}
		(string name, string value, Texture2D texture) StatStripDataFromStatDef(StatDef statDef)
		{
			(string, string, Texture2D) result3 = default((string, string, Texture2D));
			result3.Item1 = Language.GetString(statDef.displayToken);
			result3.Item2 = statSheet.GetStatDisplayValue(statDef);
			result3.Item3 = null;
			return result3;
		}
	}

	public void AddRunReportPanel(RunReport runReport)
	{
		GameObject gameObject = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/UI/GameEndReportPanelScrolling"), container);
		gameObject.GetComponent<GameEndReportPanelController>().SetDisplayData(new GameEndReportPanelController.DisplayData
		{
			runReport = runReport,
			playerIndex = 0
		});
		gameObject.GetComponent<MPEventSystemProvider>().fallBackToMainEventSystem = true;
		managedObjects.Add(gameObject);
	}

	public static void RunReportPanel(PageBuilder builder)
	{
		builder.AddRunReportPanel((RunReport)builder.entry.extraData);
	}
}
