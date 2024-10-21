using System;
using System.Collections.Generic;
using System.Linq;
using HG;
using JetBrains.Annotations;
using RoR2.ContentManagement;
using RoR2.Modding;
using UnityEngine;

namespace RoR2;

public static class GameModeCatalog
{
	private static readonly Dictionary<string, int> nameToIndex = new Dictionary<string, int>();

	private static Run[] indexToPrefabComponents = Array.Empty<Run>();

	private static string[] indexToName = Array.Empty<string>();

	private static readonly Dictionary<string, Run> nameToPrefabComponents = new Dictionary<string, Run>();

	public static ResourceAvailability availability;

	public static int gameModeCount => indexToPrefabComponents.Length;

	[Obsolete("Use IContentPackProvider instead.")]
	public static event Action<List<GameObject>> getAdditionalEntries
	{
		add
		{
			LegacyModContentPackProvider.instance.HandleLegacyGetAdditionalEntries("RoR2.GameModeCatalog.getAdditionalEntries", value, LegacyModContentPackProvider.instance.registrationContentPack.gameModePrefabs);
		}
		remove
		{
		}
	}

	[SystemInitializer(new Type[] { typeof(RuleCatalog) })]
	private static void LoadGameModes()
	{
		SetGameModes(ContentManager.gameModePrefabs.Select((GameObject v) => v.GetComponent<Run>()).ToArray());
		availability.MakeAvailable();
	}

	private static void SetGameModes(Run[] newGameModePrefabComponents)
	{
		Run[] array = indexToPrefabComponents;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].gameModeIndex = GameModeIndex.Invalid;
		}
		ArrayUtils.CloneTo(newGameModePrefabComponents, ref indexToPrefabComponents);
		Array.Sort(newGameModePrefabComponents, (Run a, Run b) => string.CompareOrdinal(a.name, b.name));
		nameToIndex.Clear();
		nameToPrefabComponents.Clear();
		Array.Resize(ref indexToName, indexToPrefabComponents.Length);
		int j = 0;
		for (int num = indexToPrefabComponents.Length; j < num; j++)
		{
			Run run = indexToPrefabComponents[j];
			string name = run.gameObject.name;
			string key = name + "(Clone)";
			nameToIndex.Add(name, j);
			nameToIndex.Add(key, j);
			nameToPrefabComponents.Add(name, run);
			nameToPrefabComponents.Add(key, run);
			run.gameModeIndex = (GameModeIndex)j;
			indexToName[j] = name;
		}
	}

	[CanBeNull]
	public static Run FindGameModePrefabComponent([NotNull] string name)
	{
		nameToPrefabComponents.TryGetValue(name, out var value);
		return value;
	}

	[CanBeNull]
	public static Run GetGameModePrefabComponent(GameModeIndex index)
	{
		return ArrayUtils.GetSafe(indexToPrefabComponents, (int)index);
	}

	public static GameModeIndex FindGameModeIndex([NotNull] string name)
	{
		if (nameToIndex.TryGetValue(name, out var value))
		{
			return (GameModeIndex)value;
		}
		return GameModeIndex.Invalid;
	}

	[CanBeNull]
	public static string GetGameModeName(GameModeIndex index)
	{
		return ArrayUtils.GetSafe(indexToName, (int)index);
	}
}
