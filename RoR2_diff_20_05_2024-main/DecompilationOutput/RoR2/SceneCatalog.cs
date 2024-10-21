using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using HG;
using JetBrains.Annotations;
using RoR2.ContentManagement;
using RoR2.Modding;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RoR2;

public static class SceneCatalog
{
	private static SceneDef[] indexToSceneDef = Array.Empty<SceneDef>();

	private static SceneDef[] _stageSceneDefs = Array.Empty<SceneDef>();

	private static string[] _baseSceneNames = Array.Empty<string>();

	private static readonly Dictionary<string, SceneIndex> nameToIndex = new Dictionary<string, SceneIndex>(StringComparer.OrdinalIgnoreCase);

	private static SceneDef currentSceneDef;

	public static ResourceAvailability availability;

	public static int sceneDefCount => indexToSceneDef.Length;

	public static ReadOnlyArray<SceneDef> allSceneDefs => indexToSceneDef;

	public static ReadOnlyArray<SceneDef> allStageSceneDefs => _stageSceneDefs;

	public static ReadOnlyArray<string> allBaseSceneNames => _baseSceneNames;

	[NotNull]
	public static SceneDef mostRecentSceneDef { get; private set; }

	public static event Action<SceneDef> onMostRecentSceneDefChanged;

	[Obsolete("Use IContentPackProvider instead.")]
	public static event Action<List<SceneDef>> getAdditionalEntries
	{
		add
		{
			LegacyModContentPackProvider.instance.HandleLegacyGetAdditionalEntries("RoR2.SceneCatalog.getAdditionalEntries", value, LegacyModContentPackProvider.instance.registrationContentPack.sceneDefs);
		}
		remove
		{
		}
	}

	public static IEnumerator Init()
	{
		SceneManager.activeSceneChanged += OnActiveSceneChanged;
		yield return SetSceneDefs(ContentManager.sceneDefs);
		availability.MakeAvailable();
	}

	private static IEnumerator SetSceneDefs([NotNull] SceneDef[] newSceneDefs)
	{
		SceneDef[] array = indexToSceneDef;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].sceneDefIndex = SceneIndex.Invalid;
		}
		ArrayUtils.CloneTo(newSceneDefs, ref indexToSceneDef);
		Array.Sort(indexToSceneDef, (SceneDef a, SceneDef b) => string.CompareOrdinal(a.cachedName, b.cachedName));
		for (int j = 0; j < indexToSceneDef.Length; j++)
		{
			indexToSceneDef[j].sceneDefIndex = (SceneIndex)j;
		}
		yield return null;
		nameToIndex.Clear();
		for (int k = 0; k < indexToSceneDef.Length; k++)
		{
			SceneDef sceneDef = indexToSceneDef[k];
			nameToIndex[sceneDef.cachedName] = (SceneIndex)k;
		}
		List<string> list = new List<string>();
		List<SceneDef> list2 = new List<SceneDef>();
		array = indexToSceneDef;
		foreach (SceneDef sceneDef2 in array)
		{
			if (!list.Contains(sceneDef2.baseSceneName))
			{
				list.Add(sceneDef2.baseSceneName);
			}
			if (sceneDef2.sceneType == SceneType.Stage)
			{
				list2.Add(sceneDef2);
			}
		}
		int num = 0;
		_stageSceneDefs = new SceneDef[list2.Count];
		foreach (SceneDef item in list2)
		{
			_stageSceneDefs[num++] = item;
		}
		num = 0;
		_baseSceneNames = new string[list.Count];
		foreach (string item2 in list)
		{
			_baseSceneNames[num++] = item2;
		}
		currentSceneDef = GetSceneDefFromSceneName(SceneManager.GetActiveScene().name);
		mostRecentSceneDef = currentSceneDef;
		yield return null;
	}

	private static void OnActiveSceneChanged(Scene oldScene, Scene newScene)
	{
		currentSceneDef = GetSceneDefFromSceneName(newScene.name);
		if ((object)currentSceneDef != null)
		{
			mostRecentSceneDef = currentSceneDef;
			SceneCatalog.onMostRecentSceneDefChanged?.Invoke(mostRecentSceneDef);
		}
	}

	[NotNull]
	public static UnlockableDef GetUnlockableLogFromBaseSceneName([NotNull] string baseSceneName)
	{
		return UnlockableCatalog.GetUnlockableDef(string.Format(CultureInfo.InvariantCulture, "Logs.Stages.{0}", baseSceneName));
	}

	[CanBeNull]
	public static SceneDef GetSceneDefForCurrentScene()
	{
		return GetSceneDefFromScene(SceneManager.GetActiveScene());
	}

	[CanBeNull]
	public static SceneDef GetSceneDef(SceneIndex sceneIndex)
	{
		return ArrayUtils.GetSafe(indexToSceneDef, (int)sceneIndex);
	}

	[CanBeNull]
	public static SceneDef GetSceneDefFromSceneName([NotNull] string sceneName)
	{
		SceneDef sceneDef = FindSceneDef(sceneName);
		if ((object)sceneDef != null)
		{
			return sceneDef;
		}
		Debug.LogWarningFormat("Could not find scene with name \"{0}\".", sceneName);
		return null;
	}

	[CanBeNull]
	public static SceneDef GetSceneDefFromScene(Scene scene)
	{
		return GetSceneDefFromSceneName(scene.name);
	}

	public static SceneIndex FindSceneIndex([NotNull] string sceneName)
	{
		if (nameToIndex.TryGetValue(sceneName, out var value))
		{
			return value;
		}
		return SceneIndex.Invalid;
	}

	public static SceneDef FindSceneDef([NotNull] string sceneName)
	{
		return ArrayUtils.GetSafe(indexToSceneDef, (int)FindSceneIndex(sceneName));
	}
}
