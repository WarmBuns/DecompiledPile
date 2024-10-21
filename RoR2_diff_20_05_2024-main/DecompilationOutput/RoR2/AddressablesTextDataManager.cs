using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace RoR2;

public class AddressablesTextDataManager : TextDataManager
{
	private bool configInitialized;

	private bool locInitialized;

	private List<TextAsset> configFiles = new List<TextAsset>();

	public override bool InitializedConfigFiles => configInitialized;

	public override bool InitializedLocFiles => locInitialized;

	public AddressablesTextDataManager()
	{
		AsyncOperationHandle<IList<IResourceLocation>> asyncOperationHandle = Addressables.LoadResourceLocationsAsync("Config");
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<IList<IResourceLocation>> addressesResult)
		{
			AsyncOperationHandle<IList<TextAsset>> asyncOperationHandle2 = Addressables.LoadAssetsAsync(addressesResult.Result, delegate(TextAsset conf)
			{
				configFiles.Add(conf);
			});
			asyncOperationHandle2.Completed += delegate
			{
				configInitialized = true;
			};
		};
		locInitialized = true;
	}

	public override string GetConfFile(string fileName, string path)
	{
		string fileNameLower = fileName.ToLower();
		TextAsset textAsset = configFiles.Find((TextAsset x) => x.name.ToLower().Contains(fileNameLower));
		if (textAsset != null)
		{
			return textAsset.text;
		}
		return null;
	}

	public override void GetLocFiles(string folderPath, Action<string[]> callback)
	{
		AsyncOperationHandle<IList<IResourceLocation>> asyncOperationHandle = Addressables.LoadResourceLocationsAsync("Localization");
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<IList<IResourceLocation>> addressesResult)
		{
			List<IResourceLocation> list = new List<IResourceLocation>();
			for (int num = addressesResult.Result.Count - 1; num >= 0; num--)
			{
				if (addressesResult.Result[num].PrimaryKey.StartsWith(folderPath))
				{
					list.Add(addressesResult.Result[num]);
				}
			}
			List<TextAsset> textAssetResults = new List<TextAsset>(list.Count);
			AsyncOperationHandle<IList<TextAsset>> asyncOperationHandle2 = Addressables.LoadAssetsAsync(list, delegate(TextAsset x)
			{
				if (x != null)
				{
					textAssetResults.Add(x);
				}
			});
			asyncOperationHandle2.Completed += delegate
			{
				int count = textAssetResults.Count;
				string[] array = new string[count];
				for (int i = 0; i < count; i++)
				{
					array[i] = textAssetResults[i].text;
				}
				callback?.Invoke(array);
			};
		};
	}
}
