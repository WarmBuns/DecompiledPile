using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using HG;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class EclipseRun : Run
{
	public static int minEclipseLevel = 1;

	public static int maxEclipseLevel = 8;

	private static Dictionary<SurvivorDef, List<UnlockableDef>> survivorToEclipseUnlockables = new Dictionary<SurvivorDef, List<UnlockableDef>>();

	public static int minUnlockableEclipseLevel => minEclipseLevel + 1;

	public static DifficultyIndex GetEclipseDifficultyIndex(int eclipseLevel)
	{
		return (DifficultyIndex)(DifficultyCatalog.standardDifficultyCount - 1 + eclipseLevel);
	}

	private static List<UnlockableDef> GetEclipseLevelUnlockablesForSurvivor(SurvivorDef survivorDef)
	{
		if (!survivorToEclipseUnlockables.TryGetValue(survivorDef, out var value))
		{
			value = new List<UnlockableDef>();
			survivorToEclipseUnlockables[survivorDef] = value;
			if (BodyCatalog.GetBodyName(BodyCatalog.FindBodyIndex(survivorDef.bodyPrefab)) != null)
			{
				int num = minUnlockableEclipseLevel;
				StringBuilder stringBuilder = HG.StringBuilderPool.RentStringBuilder();
				while (true)
				{
					stringBuilder.Clear();
					stringBuilder.Append("Eclipse.").Append(survivorDef.cachedName).Append(".")
						.AppendInt(num);
					UnlockableDef unlockableDef = UnlockableCatalog.GetUnlockableDef(stringBuilder.ToString());
					if (!unlockableDef)
					{
						break;
					}
					value.Add(unlockableDef);
					num++;
				}
				HG.StringBuilderPool.ReturnStringBuilder(stringBuilder);
			}
		}
		return value;
	}

	public static int GetLocalUserSurvivorCompletedEclipseLevel(LocalUser localUser, SurvivorDef survivorDef)
	{
		List<UnlockableDef> eclipseLevelUnlockablesForSurvivor = GetEclipseLevelUnlockablesForSurvivor(survivorDef);
		int num = 1;
		for (int i = 0; i < eclipseLevelUnlockablesForSurvivor.Count && localUser.userProfile.HasUnlockable(eclipseLevelUnlockablesForSurvivor[i]); i++)
		{
			num = minUnlockableEclipseLevel + i;
		}
		return Mathf.Clamp(num - 1, 0, maxEclipseLevel);
	}

	public static int GetNetworkUserSurvivorCompletedEclipseLevel(NetworkUser networkUser, SurvivorDef survivorDef)
	{
		List<UnlockableDef> eclipseLevelUnlockablesForSurvivor = GetEclipseLevelUnlockablesForSurvivor(survivorDef);
		int num = 1;
		for (int i = 0; i < eclipseLevelUnlockablesForSurvivor.Count && networkUser.unlockables.Contains(eclipseLevelUnlockablesForSurvivor[i]); i++)
		{
			num = minUnlockableEclipseLevel + i;
		}
		return Mathf.Clamp(num - 1, 0, maxEclipseLevel);
	}

	public static int GetEclipseLevelFromRuleBook(RuleBook ruleBook)
	{
		return (int)(ruleBook.FindDifficulty() - DifficultyCatalog.standardDifficultyCount + 1);
	}

	public override void OnClientGameOver(RunReport runReport)
	{
		base.OnClientGameOver(runReport);
		if (!runReport.gameEnding.isWin)
		{
			return;
		}
		int num = GetEclipseLevelFromRuleBook(base.ruleBook) + 1;
		ReadOnlyCollection<PlayerCharacterMasterController> instances = PlayerCharacterMasterController.instances;
		for (int i = 0; i < instances.Count; i++)
		{
			_ = instances[i];
			NetworkUser networkUser = instances[i].networkUser;
			if (!networkUser)
			{
				continue;
			}
			LocalUser localUser = networkUser.localUser;
			if (localUser == null)
			{
				continue;
			}
			SurvivorDef survivorPreference = networkUser.GetSurvivorPreference();
			if ((bool)survivorPreference)
			{
				UnlockableDef safe = ListUtils.GetSafe(GetEclipseLevelUnlockablesForSurvivor(survivorPreference), num - minUnlockableEclipseLevel);
				if ((bool)safe)
				{
					localUser.userProfile.GrantUnlockable(safe);
				}
			}
		}
	}

	public override void OverrideRuleChoices(RuleChoiceMask mustInclude, RuleChoiceMask mustExclude, ulong runSeed)
	{
		base.OverrideRuleChoices(mustInclude, mustExclude, base.seed);
		int num = 0;
		ReadOnlyCollection<NetworkUser> readOnlyInstancesList = NetworkUser.readOnlyInstancesList;
		for (int i = 0; i < readOnlyInstancesList.Count; i++)
		{
			NetworkUser networkUser = readOnlyInstancesList[i];
			SurvivorDef survivorPreference = networkUser.GetSurvivorPreference();
			if ((bool)survivorPreference)
			{
				int num2 = GetNetworkUserSurvivorCompletedEclipseLevel(networkUser, survivorPreference) + 1;
				num = ((num > 0) ? Math.Min(num, num2) : num2);
			}
		}
		num = Math.Min(num, maxEclipseLevel);
		ForceChoice(mustInclude, mustExclude, $"Difficulty.{GetEclipseDifficultyIndex(num).ToString()}");
		ForceChoice(mustInclude, mustExclude, "Items." + RoR2Content.Items.LunarTrinket.name + ".Off");
		for (int j = 0; j < ArtifactCatalog.artifactCount; j++)
		{
			ForceChoice(mustInclude, mustExclude, FindRuleForArtifact((ArtifactIndex)j).FindChoice("Off"));
		}
		static RuleDef FindRuleForArtifact(ArtifactIndex artifactIndex)
		{
			ArtifactDef artifactDef = ArtifactCatalog.GetArtifactDef(artifactIndex);
			return RuleCatalog.FindRuleDef("Artifacts." + artifactDef.cachedName);
		}
	}

	protected override void HandlePostRunDestination()
	{
		Console.instance.SubmitCmd(null, "transition_command \"disconnect\";");
	}

	protected new void Start()
	{
		base.Start();
		if (NetworkServer.active)
		{
			SetEventFlag("NoArtifactWorld");
			SetEventFlag("NoMysterySpace");
			SetEventFlag("NoVoidStage");
		}
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		bool flag = base.OnSerialize(writer, forceAll);
		bool flag2 = default(bool);
		return flag2 || flag;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
		base.OnDeserialize(reader, initialState);
	}

	public override void PreStartClient()
	{
		base.PreStartClient();
	}
}
