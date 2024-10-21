using System;
using System.Collections.Generic;
using HG;
using RoR2.ContentManagement;
using RoR2.Modding;
using UnityEngine;

namespace RoR2.Skills;

public static class SkillCatalog
{
	private static SkillDef[] _allSkillDefs = Array.Empty<SkillDef>();

	private static string[] _allSkillNames = Array.Empty<string>();

	private static SkillFamily[] _allSkillFamilies = Array.Empty<SkillFamily>();

	private static string[] _allSkillFamilyNames = Array.Empty<string>();

	public static ResourceAvailability skillsDefined;

	public static IEnumerable<SkillDef> allSkillDefs => _allSkillDefs;

	public static IEnumerable<SkillFamily> allSkillFamilies => _allSkillFamilies;

	[Obsolete("Use IContentPackProvider instead.")]
	public static event Action<List<SkillDef>> getAdditionalSkillDefs
	{
		add
		{
			LegacyModContentPackProvider.instance.HandleLegacyGetAdditionalEntries("RoR2.Skills.SkillCatalog.getAdditionalSkillDefs", value, LegacyModContentPackProvider.instance.registrationContentPack.skillDefs);
		}
		remove
		{
		}
	}

	[Obsolete("Use IContentPackProvider instead.")]
	public static event Action<List<SkillFamily>> getAdditionalSkillFamilies
	{
		add
		{
			LegacyModContentPackProvider.instance.HandleLegacyGetAdditionalEntries("RoR2.Skills.SkillCatalog.getAdditionalSkillFamilies", value, LegacyModContentPackProvider.instance.registrationContentPack.skillFamilies);
		}
		remove
		{
		}
	}

	public static SkillDef GetSkillDef(int skillDefIndex)
	{
		return ArrayUtils.GetSafe(_allSkillDefs, skillDefIndex);
	}

	public static string GetSkillName(int skillDefIndex)
	{
		return ArrayUtils.GetSafe(_allSkillNames, skillDefIndex);
	}

	public static SkillFamily GetSkillFamily(int skillFamilyIndex)
	{
		return ArrayUtils.GetSafe(_allSkillFamilies, skillFamilyIndex);
	}

	public static string GetSkillFamilyName(int skillFamilyIndex)
	{
		return ArrayUtils.GetSafe(_allSkillFamilyNames, skillFamilyIndex);
	}

	public static int FindSkillIndexByName(string skillName)
	{
		for (int i = 0; i < _allSkillDefs.Length; i++)
		{
			if (_allSkillDefs[i].skillName == skillName)
			{
				return i;
			}
		}
		return -1;
	}

	[SystemInitializer(new Type[] { typeof(BodyCatalog) })]
	private static void Init()
	{
		SetSkillDefs(ContentManager.skillDefs);
		SetSkillFamilies(ContentManager.skillFamilies);
		skillsDefined.MakeAvailable();
	}

	private static void SetSkillDefs(SkillDef[] newSkillDefs)
	{
		SkillDef[] array = _allSkillDefs;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].skillIndex = -1;
		}
		ArrayUtils.CloneTo(newSkillDefs, ref _allSkillDefs);
		Array.Resize(ref _allSkillNames, _allSkillDefs.Length);
		for (int j = 0; j < _allSkillDefs.Length; j++)
		{
			_allSkillDefs[j].skillIndex = j;
			_allSkillNames[j] = ((UnityEngine.Object)_allSkillDefs[j]).name;
		}
	}

	private static void SetSkillFamilies(SkillFamily[] newSkillFamilies)
	{
		SkillFamily[] array = _allSkillFamilies;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].catalogIndex = -1;
		}
		ArrayUtils.CloneTo(newSkillFamilies, ref _allSkillFamilies);
		Array.Resize(ref _allSkillFamilyNames, _allSkillDefs.Length);
		for (int j = 0; j < _allSkillFamilies.Length; j++)
		{
			_allSkillFamilies[j].catalogIndex = j;
			_allSkillFamilyNames[j] = ((UnityEngine.Object)_allSkillFamilies[j]).name;
		}
	}
}
