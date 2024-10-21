using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using HG;
using HG.AssetManagement;
using HG.AsyncOperations;
using HG.Coroutines;
using UnityEngine;

namespace RoR2.ContentManagement;

public class ContentLoadHelper
{
	public delegate ref T GetRefDelegate<T>();

	public delegate void AcceptResultDelegate<T>(T result);

	public IAssetRepository assetRepository;

	public ReadableProgress<float> progress;

	public ParallelProgressCoroutine coroutine;

	public ContentLoadHelper()
	{
		progress = new ReadableProgress<float>();
		coroutine = new ParallelProgressCoroutine(progress);
	}

	public void DispatchLoadConvert<TAssetSrc, TAssetDest>(string folderPath, Action<TAssetDest[]> onComplete, Func<TAssetSrc, TAssetDest> selector = null) where TAssetSrc : UnityEngine.Object
	{
		ReadableProgress<float> progressReceiver = new ReadableProgress<float>();
		coroutine.Add(Coroutine(), progressReceiver);
		IEnumerator Coroutine()
		{
			BaseAsyncOperation<TAssetSrc[]> loadFolderOperation = assetRepository.LoadAllAsync<TAssetSrc>(folderPath);
			while (!loadFolderOperation.isDone)
			{
				progressReceiver.Report(Util.Remap(progressReceiver.value, 0f, 1f, 0f, 0.95f));
				yield return null;
			}
			TAssetSrc[] loadedAssets = loadFolderOperation.result.Where((TAssetSrc asset) => asset).ToArray();
			progressReceiver.Report(0.97f);
			yield return null;
			if (onComplete != null)
			{
				TAssetDest[] convertedAssets;
				if (selector == null)
				{
					if (!(typeof(TAssetSrc) == typeof(TAssetDest)))
					{
						throw new ArgumentNullException("selector", "Converter must be provided when TAssetSrc and TAssetDest differ.");
					}
					convertedAssets = (TAssetDest[])(object)loadedAssets;
				}
				else
				{
					convertedAssets = new TAssetDest[loadedAssets.Length];
					int i = 0;
					while (i < loadedAssets.Length)
					{
						yield return null;
						convertedAssets[i] = selector(loadedAssets[i]);
						int num = i + 1;
						i = num;
					}
				}
				yield return null;
				string[] assetNames = new string[loadedAssets.Length];
				for (int j = 0; j < loadedAssets.Length; j++)
				{
					assetNames[j] = loadedAssets[j].name;
				}
				yield return null;
				Array.Sort(assetNames, convertedAssets, StringComparer.Ordinal);
				onComplete?.Invoke(convertedAssets);
			}
			progressReceiver.Report(1f);
		}
	}

	public void DispatchLoad<TAsset>(string folderPath, Action<TAsset[]> onComplete) where TAsset : UnityEngine.Object
	{
		DispatchLoadConvert<TAsset, TAsset>(folderPath, onComplete);
	}

	public static void PopulateTypeFields<TAsset>(Type typeToPopulate, NamedAssetCollection<TAsset> assets, Func<string, string> fieldNameToAssetNameConverter = null) where TAsset : UnityEngine.Object
	{
		string[] array = new string[assets.Length];
		for (int i = 0; i < assets.Length; i++)
		{
			array[i] = assets.GetAssetName(assets[i]);
		}
		FieldInfo[] fields = typeToPopulate.GetFields(BindingFlags.Static | BindingFlags.Public);
		foreach (FieldInfo fieldInfo in fields)
		{
			if (fieldInfo.FieldType == typeof(TAsset))
			{
				TargetAssetNameAttribute customAttribute = CustomAttributeExtensions.GetCustomAttribute<TargetAssetNameAttribute>(fieldInfo);
				string text = ((customAttribute != null) ? customAttribute.targetAssetName : ((fieldNameToAssetNameConverter == null) ? fieldInfo.Name : fieldNameToAssetNameConverter(fieldInfo.Name)));
				TAsset val = assets.Find(text);
				if ((object)val != null)
				{
					fieldInfo.SetValue(null, val);
					continue;
				}
				Debug.LogWarning("Failed to assign " + fieldInfo.DeclaringType.Name + "." + fieldInfo.Name + ": Asset \"" + text + "\" not found.");
			}
		}
	}
}
