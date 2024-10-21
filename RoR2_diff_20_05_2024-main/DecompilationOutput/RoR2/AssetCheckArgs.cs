using System.IO;
using JetBrains.Annotations;
using UnityEngine;

namespace RoR2;

public class AssetCheckArgs
{
	[NotNull]
	public ProjectIssueChecker projectIssueChecker;

	[NotNull]
	public Object asset;

	[CanBeNull]
	public Component assetComponent => asset as Component;

	[CanBeNull]
	public GameObject gameObject => assetComponent?.gameObject;

	[CanBeNull]
	public GameObject gameObjectRoot => gameObject?.transform.root?.gameObject;

	public bool isPrefab => GameObjectIsPrefab(gameObjectRoot);

	public bool isPrefabVariant => GameObjectIsPrefabVariant(gameObjectRoot);

	[CanBeNull]
	public GameObject prefabRoot
	{
		get
		{
			GameObject result = gameObjectRoot;
			if (GameObjectIsPrefab(result))
			{
				return result;
			}
			return null;
		}
	}

	private static bool GameObjectIsPrefab(GameObject gameObject)
	{
		return false;
	}

	private static bool GameObjectIsPrefabVariant(GameObject gameObject)
	{
		return false;
	}

	public void UpdatePrefab()
	{
		_ = (bool)prefabRoot;
	}

	public void Log(string str, Object context = null)
	{
		projectIssueChecker.Log(str, context);
	}

	public void LogError(string str, Object context = null)
	{
		projectIssueChecker.LogError(str, context);
	}

	public void LogFormat(Object context, string format, params object[] formatArgs)
	{
		projectIssueChecker.LogFormat(context, format, formatArgs);
	}

	public void LogErrorFormat(Object context, string format, params object[] formatArgs)
	{
		projectIssueChecker.LogErrorFormat(context, format, formatArgs);
	}

	public void EnsurePath(string path)
	{
		Directory.Exists(path);
	}

	public T LoadAsset<T>(string fullFilePath) where T : Object
	{
		File.Exists(fullFilePath);
		return null;
	}

	public static void CreateAsset<T>(string path)
	{
	}
}
