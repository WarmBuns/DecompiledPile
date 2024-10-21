using System;
using System.Collections.Generic;
using System.Linq;
using HG;
using HG.AssetManagement;
using HG.AsyncOperations;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace RoR2.ContentManagement;

public class AddressablesDirectory : IAssetRepository
{
	public class AddressablesLoadAsyncOperationWrapper<TAsset> : BaseAsyncOperation<TAsset[]> where TAsset : UnityEngine.Object
	{
		private AsyncOperationHandle<TAsset>[] handles;

		private int completionCount;

		private List<TAsset> results = new List<TAsset>();

		public override float progress
		{
			get
			{
				float num = 0f;
				if (handles.Length == 0)
				{
					return 0f;
				}
				float num2 = 1f / (float)handles.Length;
				for (int i = 0; i < handles.Length; i++)
				{
					num += handles[i].PercentComplete * num2;
				}
				return num;
			}
		}

		public AddressablesLoadAsyncOperationWrapper(IReadOnlyList<AsyncOperationHandle<TAsset>> handles)
		{
			if (handles.Count == 0)
			{
				this.handles = Array.Empty<AsyncOperationHandle<TAsset>>();
				Complete(Array.Empty<TAsset>());
				return;
			}
			results = new List<TAsset>(handles.Count);
			this.handles = new AsyncOperationHandle<TAsset>[handles.Count];
			Action<AsyncOperationHandle<TAsset>> value = OnChildOperationCompleted;
			for (int i = 0; i < handles.Count; i++)
			{
				this.handles[i] = handles[i];
				AsyncOperationHandle<TAsset> asyncOperationHandle = handles[i];
				asyncOperationHandle.Completed += value;
			}
		}

		private void OnChildOperationCompleted(AsyncOperationHandle<TAsset> handle)
		{
			if ((bool)handle.Result)
			{
				results.Add(handle.Result);
			}
			completionCount++;
			if (completionCount == handles.Length)
			{
				Complete(results.ToArray());
			}
		}
	}

	private string baseDirectory;

	private FilePathTree folderTree;

	private IResourceLocator[] resourceLocators = Array.Empty<IResourceLocator>();

	public AddressablesDirectory(string baseDirectory, IEnumerable<IResourceLocator> resourceLocators)
	{
		baseDirectory = baseDirectory ?? string.Empty;
		if (baseDirectory.Length > 0 && !baseDirectory.EndsWith("/"))
		{
			throw new ArgumentException("'baseDirectory' must be empty or end with '/'. baseDirectory=\"" + baseDirectory + "\"");
		}
		this.baseDirectory = baseDirectory;
		this.resourceLocators = resourceLocators.ToArray();
		List<string> list = new List<string>();
		IResourceLocator[] array = this.resourceLocators;
		for (int i = 0; i < array.Length; i++)
		{
			foreach (object key in array[i].Keys)
			{
				if (key is string text && text.StartsWith(baseDirectory))
				{
					list.Add(text);
				}
			}
		}
		folderTree = FilePathTree.Create(list);
	}

	public BaseAsyncOperation<TAsset[]> LoadAllAsync<TAsset>(string folderPath) where TAsset : UnityEngine.Object
	{
		List<string> list = new List<string>();
		string folderPath2 = baseDirectory + folderPath;
		folderTree.GetEntriesInFolder(folderPath2, list);
		_ = list.Count;
		return IssueLoadOperationAsGroup<TAsset>(list);
	}

	private BaseAsyncOperation<TAsset[]> IssueLoadOperationAsGroup<TAsset>(List<string> entriesToLoad) where TAsset : UnityEngine.Object
	{
		List<IResourceLocation> locations = FilterPathsByType<TAsset>(entriesToLoad);
		List<TAsset> results = new List<TAsset>();
		AsyncOperationHandle<IList<TAsset>> loadOperationHandle = Addressables.LoadAssetsAsync(locations, delegate(TAsset result)
		{
			results.Add(result);
		});
		ScriptedAsyncOperation<TAsset[]> resultOperation = new ScriptedAsyncOperation<TAsset[]>(() => loadOperationHandle.PercentComplete);
		loadOperationHandle.Completed += delegate
		{
			resultOperation.Complete(results.ToArray());
		};
		return resultOperation;
	}

	private BaseAsyncOperation<TAsset[]> IssueLoadOperationsIndividually<TAsset>(List<string> entriesToLoad) where TAsset : UnityEngine.Object
	{
		List<IResourceLocation> list = FilterPathsByType<TAsset>(entriesToLoad);
		AsyncOperationHandle<TAsset>[] array = new AsyncOperationHandle<TAsset>[list.Count];
		for (int i = 0; i < list.Count; i++)
		{
			AsyncOperationHandle<TAsset> asyncOperationHandle = Addressables.LoadAssetAsync<TAsset>(list[i]);
			array[i] = asyncOperationHandle;
		}
		return new AddressablesLoadAsyncOperationWrapper<TAsset>(array);
	}

	private List<IResourceLocation> FilterPathsByType<TAsset>(List<string> paths)
	{
		Type typeFromHandle = typeof(TAsset);
		bool[] array = new bool[paths.Count];
		List<IResourceLocation> list = new List<IResourceLocation>();
		IResourceLocator[] array2 = resourceLocators;
		foreach (IResourceLocator resourceLocator in array2)
		{
			for (int j = 0; j < paths.Count; j++)
			{
				if (array[j] || !resourceLocator.Locate(paths[j], null, out var locations))
				{
					continue;
				}
				for (int k = 0; k < locations.Count; k++)
				{
					IResourceLocation resourceLocation = locations[k];
					if (typeFromHandle.IsAssignableFrom(resourceLocation.ResourceType))
					{
						list.Add(resourceLocation);
						array[j] = true;
						break;
					}
				}
			}
		}
		return list;
	}
}
