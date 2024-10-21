using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;

namespace RoR2;

public static class EditorUtil
{
	private static int recursionLimit;

	private static int recursionStep;

	private static readonly char[] directorySeparators;

	private static event Action _onNextUpdate;

	public static event Action onNextUpdate
	{
		add
		{
		}
		remove
		{
		}
	}

	static EditorUtil()
	{
		recursionLimit = 3;
		recursionStep = 0;
		directorySeparators = new char[2] { '/', '\\' };
	}

	private static void StaticUpdate()
	{
		Action action = EditorUtil._onNextUpdate;
		EditorUtil._onNextUpdate = null;
		action?.Invoke();
	}

	public static bool IsPrefabVariant(UnityEngine.Object obj)
	{
		return false;
	}

	public static void SetDirty(UnityEngine.Object obj)
	{
	}

	public static void NonSerializedObjectGUI<T>(ref T obj)
	{
	}

	[UsedImplicitly]
	private static void SetFieldGeneric<T>(FieldInfo fieldInfo, object instance, T value)
	{
		if (fieldInfo.FieldType.IsAssignableFrom(typeof(T)))
		{
			fieldInfo.SetValue(instance, value);
			return;
		}
		Debug.LogErrorFormat("Cannot assign value {0} of type {1} to field {2} of type {3}", value, value.GetType().Name, fieldInfo.Name, fieldInfo.FieldType.Name);
	}

	public static UnityEngine.Object PrefabUtilityGetNearestPrefabInstanceRoot(UnityEngine.Object obj)
	{
		throw new NotImplementedException();
	}

	public static UnityEngine.Object PrefabUtilityInstantiatePrefab(UnityEngine.Object prefab, Transform parent)
	{
		throw new NotImplementedException();
	}

	public static void CopyToScriptableObject<TSrc, TDest>(TSrc src, string folder) where TDest : ScriptableObject
	{
		TDest val = ScriptableObject.CreateInstance<TDest>();
		Dictionary<string, (Type, Func<TSrc, object>)> dictionary = new Dictionary<string, (Type, Func<TSrc, object>)>();
		Dictionary<string, (Type, Action<TDest, object>)> dictionary2 = new Dictionary<string, (Type, Action<TDest, object>)>();
		FieldInfo[] fields = typeof(TSrc).GetFields();
		foreach (FieldInfo srcField in fields)
		{
			dictionary[srcField.Name] = (srcField.FieldType, (TSrc s) => srcField.GetValue(s));
		}
		PropertyInfo[] properties = typeof(TSrc).GetProperties();
		foreach (PropertyInfo srcProperty in properties)
		{
			if (!(srcProperty.GetMethod == null))
			{
				dictionary[srcProperty.Name] = (srcProperty.PropertyType, (TSrc s) => srcProperty.GetValue(s));
			}
		}
		fields = typeof(TDest).GetFields();
		foreach (FieldInfo destField in fields)
		{
			dictionary2[destField.Name] = (destField.FieldType, delegate(TDest d, object v)
			{
				destField.SetValue(d, v);
			});
		}
		properties = typeof(TDest).GetProperties();
		foreach (PropertyInfo destProperty in properties)
		{
			if (!(destProperty.SetMethod == null))
			{
				dictionary2[destProperty.Name] = (destProperty.PropertyType, delegate(TDest d, object v)
				{
					destProperty.SetValue(d, v);
				});
			}
		}
		foreach (KeyValuePair<string, (Type, Func<TSrc, object>)> item in dictionary)
		{
			string key = item.Key;
			var (c, func) = item.Value;
			if (dictionary2.TryGetValue(key, out var value))
			{
				var (type, action) = value;
				if (type.IsAssignableFrom(c))
				{
					action(val, func(src));
				}
			}
		}
		CreateAsset(val, folder + val.name + ".asset");
	}

	private static void CreateAsset(UnityEngine.Object asset, string path)
	{
	}

	private static void AssetDatabaseSaveAssets()
	{
	}

	private static void AssetDatabaseRefresh()
	{
	}

	private static void AssetDatabaseMoveAsset(string oldPath, string newPath)
	{
	}

	private static void AssetDatabaseDeleteAsset(string path)
	{
	}

	private static void AssetDatabaseCreateFolder(string parentFolder, string newFolderName)
	{
	}

	public static void EnsureDirectoryExists(string directoryPath)
	{
		string[] array = directoryPath.Split(directorySeparators);
		string text = array[0].ToString();
		int i = 1;
		for (int num = array.Length; i < num; i++)
		{
			string text2 = array[i].ToString();
			string text3 = $"{text}{'/'}{text2}";
			if (!Directory.Exists(text3))
			{
				AssetDatabaseCreateFolder(text, text2);
			}
			text = text3;
		}
		if (!Directory.Exists(directoryPath))
		{
			Debug.LogWarning("Failed to ensure path \"" + directoryPath + "\"");
		}
	}

	public static void MoveAsset(string oldPath, string newPath, bool deleteEmpty = true)
	{
		string directoryName = System.IO.Path.GetDirectoryName(oldPath);
		EnsureDirectoryExists(System.IO.Path.GetDirectoryName(newPath));
		AssetDatabaseMoveAsset(oldPath, newPath);
		if (deleteEmpty)
		{
			DeleteDirectoryEmptyChildren(directoryName);
		}
	}

	public static void DeleteDirectoryIfEmpty(string directory)
	{
		DeleteDirectoryEmptyChildren(directory);
		DeleteDirectoryEmptyParents(directory);
	}

	private static bool DeleteDirectoryEmptyChildren(string directory)
	{
		bool flag = true;
		try
		{
			string[] directories = Directory.GetDirectories(directory);
			for (int i = 0; i < directories.Length; i++)
			{
				string fileName = System.IO.Path.GetFileName(directories[i]);
				if (!DeleteDirectoryEmptyChildren(directory + "/" + fileName))
				{
					flag = false;
				}
			}
			flag &= Directory.GetFiles(directory).Length == 0;
			if (flag)
			{
				AssetDatabaseDeleteAsset(directory);
			}
		}
		catch (Exception ex)
		{
			throw new Exception($"DeleteDirectoryEmptyChildren(\"{directory}\") error: {ex}", ex);
		}
		return flag;
	}

	private static void DeleteDirectoryEmptyParents(string directory)
	{
		string directoryName = System.IO.Path.GetDirectoryName(directory);
		bool flag = Directory.GetDirectories(directoryName).Length != 0;
		bool flag2 = Directory.GetFiles(directoryName).Length != 0;
		if (!flag && !flag2)
		{
			AssetDatabaseDeleteAsset(directory);
			DeleteDirectoryEmptyParents(directoryName);
		}
	}

	public static IEnumerable<string> GetAllAssetsInDirectory(string directory, bool includeSubdirectories)
	{
		DirectoryInfo directoryInfo = new DirectoryInfo(directory);
		FileSystemInfo[] fileSystemInfos = directoryInfo.GetFileSystemInfos();
		for (int i = 0; i < fileSystemInfos.Length; i++)
		{
			string name = fileSystemInfos[i].Name;
			if (!name.EndsWith(".meta"))
			{
				yield return directory + "/" + name;
			}
		}
		if (!includeSubdirectories)
		{
			yield break;
		}
		DirectoryInfo[] directories = directoryInfo.GetDirectories();
		foreach (DirectoryInfo directoryInfo2 in directories)
		{
			string directory2 = directory + "/" + directoryInfo2.Name;
			foreach (string item in GetAllAssetsInDirectory(directory2, includeSubdirectories))
			{
				yield return item;
			}
		}
	}
}
