using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HG;
using HG.AsyncOperations;
using RoR2.EntitlementManagement;
using RoR2.ExpansionManagement;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace RoR2.ContentManagement;

public class AddressablesLoadHelper
{
	private class Operation
	{
		public float weight;

		public IEnumerator coroutine;

		public float progress;
	}

	public class AddressablesLoadAsyncOperationWrapper<TAsset> : BaseAsyncOperation<TAsset[]> where TAsset : UnityEngine.Object
	{
		private AsyncOperationHandle<IList<TAsset>>[] handles;

		private int completionCount;

		public override float progress => 0f;

		public AddressablesLoadAsyncOperationWrapper(IReadOnlyList<AsyncOperationHandle<IList<TAsset>>> handles)
		{
			if (handles.Count == 0)
			{
				this.handles = Array.Empty<AsyncOperationHandle<IList<TAsset>>>();
				Complete(Array.Empty<TAsset>());
				return;
			}
			Action<AsyncOperationHandle<IList<TAsset>>> value = OnChildOperationCompleted;
			this.handles = new AsyncOperationHandle<IList<TAsset>>[handles.Count];
			for (int i = 0; i < handles.Count; i++)
			{
				this.handles[i] = handles[i];
				AsyncOperationHandle<IList<TAsset>> asyncOperationHandle = handles[i];
				asyncOperationHandle.Completed += value;
			}
		}

		private void OnChildOperationCompleted(AsyncOperationHandle<IList<TAsset>> completedOperationHandle)
		{
			completionCount++;
			if (completionCount != handles.Length)
			{
				return;
			}
			List<TAsset> list = new List<TAsset>();
			AsyncOperationHandle<IList<TAsset>>[] array = handles;
			for (int i = 0; i < array.Length; i++)
			{
				AsyncOperationHandle<IList<TAsset>> asyncOperationHandle = array[i];
				if (asyncOperationHandle.Result != null)
				{
					list.AddRange(asyncOperationHandle.Result);
				}
			}
			Complete(list.ToArray());
		}
	}

	private readonly IResourceLocator[] resourceLocators = Array.Empty<IResourceLocator>();

	private readonly object[] requiredKeys = Array.Empty<object>();

	public int maxConcurrentOperations = 2;

	public float timeoutDuration = float.PositiveInfinity;

	private const int loadOperationsStartedMax = 28;

	private int loadOperationsStartedCount;

	private readonly List<Operation> allOperations = new List<Operation>();

	private readonly Queue<Operation> pendingLoadOperations = new Queue<Operation>();

	private readonly List<Operation> runningLoadOperations = new List<Operation>();

	private readonly List<Operation> allGenericOperations = new List<Operation>();

	public ReadableProgress<float> progress { get; private set; }

	public IEnumerator coroutine { get; private set; }

	private float loadOperationsStartedProgress01 => Mathf.Clamp01((float)loadOperationsStartedCount / 28f);

	public AddressablesLoadHelper(IReadOnlyList<IResourceLocator> resourceLocators, object[] requiredKeys = null)
	{
		ArrayUtils.CloneTo(resourceLocators, ref this.resourceLocators);
		if (requiredKeys != null)
		{
			ArrayUtils.CloneTo(requiredKeys, ref this.requiredKeys);
		}
		progress = new ReadableProgress<float>();
		coroutine = Coroutine(progress);
	}

	public AddressablesLoadHelper(IResourceLocator resourceLocator, object[] requiredKeys = null)
		: this(new IResourceLocator[1] { resourceLocator }, requiredKeys)
	{
	}

	public static AddressablesLoadHelper CreateUsingDefaultResourceLocator(object[] requiredKeys = null)
	{
		return new AddressablesLoadHelper(Addressables.ResourceLocators.First(), requiredKeys);
	}

	public static AddressablesLoadHelper CreateUsingDefaultResourceLocator(object requiredKey)
	{
		return CreateUsingDefaultResourceLocator(new object[1] { requiredKey });
	}

	public IEnumerator AddContentPackLoadOperationWithYields(ContentPack contentPack)
	{
		yield return FindLocationsThenAddLoadOperation<GameObject>(AddressablesLabels.characterBody, contentPack.bodyPrefabs.Add);
		yield return FindLocationsThenAddLoadOperation<GameObject>(AddressablesLabels.characterMaster, contentPack.masterPrefabs.Add);
		yield return FindLocationsThenAddLoadOperation<GameObject>(AddressablesLabels.projectile, contentPack.projectilePrefabs.Add);
		yield return FindLocationsThenAddLoadOperation<GameObject>(AddressablesLabels.gameMode, contentPack.gameModePrefabs.Add);
		yield return FindLocationsThenAddLoadOperation<GameObject>(AddressablesLabels.networkedObject, contentPack.networkedObjectPrefabs.Add);
		yield return FindLocationsThenAddLoadOperation<SkillFamily>(AddressablesLabels.skillFamily, contentPack.skillFamilies.Add);
		yield return FindLocationsThenAddLoadOperation<SkillDef>(AddressablesLabels.skillDef, contentPack.skillDefs.Add);
		yield return FindLocationsThenAddLoadOperation<UnlockableDef>(AddressablesLabels.unlockableDef, contentPack.unlockableDefs.Add);
		yield return FindLocationsThenAddLoadOperation<SurfaceDef>(AddressablesLabels.surfaceDef, contentPack.surfaceDefs.Add);
		yield return FindLocationsThenAddLoadOperation<SceneDef>(AddressablesLabels.sceneDef, contentPack.sceneDefs.Add);
		yield return FindLocationsThenAddLoadOperation<NetworkSoundEventDef>(AddressablesLabels.networkSoundEventDef, contentPack.networkSoundEventDefs.Add);
		yield return FindLocationsThenAddLoadOperation<MusicTrackDef>(AddressablesLabels.musicTrackDef, contentPack.musicTrackDefs.Add);
		yield return FindLocationsThenAddLoadOperation<GameEndingDef>(AddressablesLabels.gameEndingDef, contentPack.gameEndingDefs.Add);
		yield return FindLocationsThenAddLoadOperation<ItemDef>(AddressablesLabels.itemDef, contentPack.itemDefs.Add);
		yield return FindLocationsThenAddLoadOperation<ItemTierDef>(AddressablesLabels.itemTierDef, contentPack.itemTierDefs.Add);
		yield return FindLocationsThenAddLoadOperation<ItemRelationshipProvider>(AddressablesLabels.itemRelationshipProvider, contentPack.itemRelationshipProviders.Add);
		yield return FindLocationsThenAddLoadOperation<ItemRelationshipType>(AddressablesLabels.itemRelationshipType, contentPack.itemRelationshipTypes.Add);
		yield return FindLocationsThenAddLoadOperation<EquipmentDef>(AddressablesLabels.equipmentDef, contentPack.equipmentDefs.Add);
		yield return FindLocationsThenAddLoadOperation<MiscPickupDef>(AddressablesLabels.miscPickupDef, contentPack.miscPickupDefs.Add);
		yield return FindLocationsThenAddLoadOperation<BuffDef>(AddressablesLabels.buffDef, contentPack.buffDefs.Add);
		yield return FindLocationsThenAddLoadOperation<EliteDef>(AddressablesLabels.eliteDef, contentPack.eliteDefs.Add);
		yield return FindLocationsThenAddLoadOperation<SurvivorDef>(AddressablesLabels.survivorDef, contentPack.survivorDefs.Add);
		yield return FindLocationsThenAddLoadOperation<ArtifactDef>(AddressablesLabels.artifactDef, contentPack.artifactDefs.Add);
		yield return FindLocationsThenAddLoadOperation(AddressablesLabels.effect, contentPack.effectDefs.Add, (GameObject asset) => new EffectDef(asset));
		yield return FindLocationsThenAddLoadOperation<EntityStateConfiguration>(AddressablesLabels.entityStateConfiguration, contentPack.entityStateConfigurations.Add);
		yield return FindLocationsThenAddLoadOperation<ExpansionDef>(AddressablesLabels.expansionDef, contentPack.expansionDefs.Add);
		yield return FindLocationsThenAddLoadOperation<EntitlementDef>(AddressablesLabels.entitlementDef, contentPack.entitlementDefs.Add);
		loadOperationsStartedCount = 28;
		AddGenericOperation(AddEntityStateTypes(), 0.05f);
		yield return null;
		IEnumerator AddEntityStateTypes()
		{
			contentPack.entityStateTypes.Add((from v in contentPack.entityStateConfigurations
				select (Type)v.targetType into v
				where v != null
				select v).ToArray());
			yield return null;
		}
	}

	public IEnumerator FindLocationsThenAddLoadOperation<TAssetSrc>(string key, Action<TAssetSrc[]> onComplete, float weight = 1f) where TAssetSrc : UnityEngine.Object
	{
		return FindLocationsThenAddLoadOperation<TAssetSrc, TAssetSrc>(key, onComplete, null, weight);
	}

	public IEnumerator FindLocationsThenAddLoadOperation<TAssetSrc, TAssetDest>(string key, Action<TAssetDest[]> onComplete, Func<TAssetSrc, TAssetDest> selector = null, float weight = 1f) where TAssetSrc : UnityEngine.Object
	{
		List<object> list = new List<object>();
		ListUtils.AddRange(list, requiredKeys);
		list.Add(key);
		AsyncOperationHandle<IList<IResourceLocation>> locationAsyncOperationHandle = Addressables.LoadResourceLocationsAsync((IEnumerable)list, Addressables.MergeMode.Intersection, (Type)null);
		while (!locationAsyncOperationHandle.IsDone)
		{
			yield return null;
		}
		IList<IResourceLocation> result = locationAsyncOperationHandle.Result;
		AddLoadOperation(result, onComplete, selector, weight);
		loadOperationsStartedCount++;
	}

	public void AddLoadOperation<TAssetSrc>(IList<IResourceLocation> locations, Action<TAssetSrc[]> onComplete, float weight = 1f) where TAssetSrc : UnityEngine.Object
	{
		AddLoadOperation<TAssetSrc, TAssetSrc>(locations, onComplete, null, weight);
	}

	public void AddLoadOperation<TAssetSrc, TAssetDest>(IList<IResourceLocation> locations, Action<TAssetDest[]> onComplete, Func<TAssetSrc, TAssetDest> selector = null, float weight = 1f) where TAssetSrc : UnityEngine.Object
	{
		Operation loadOperation = new Operation
		{
			weight = weight
		};
		loadOperation.coroutine = Coroutine();
		allOperations.Add(loadOperation);
		pendingLoadOperations.Enqueue(loadOperation);
		IEnumerator Coroutine()
		{
			yield return null;
			List<AsyncOperationHandle<IList<TAssetSrc>>> underlyingLoadOperations = new List<AsyncOperationHandle<IList<TAssetSrc>>>(resourceLocators.Length);
			int i = 0;
			while (i < resourceLocators.Length)
			{
				Action<AsyncOperationHandle, Exception> oldResourceManagerExceptionHandler = ResourceManager.ExceptionHandler;
				ResourceManager.ExceptionHandler = ResourceManagerExceptionHandler;
				try
				{
					List<IResourceLocation> locationsList = locations as List<IResourceLocation>;
					int sliceStart = 0;
					int arrayLength = locations.Count;
					do
					{
						int count = Mathf.Min(5, arrayLength - sliceStart);
						AsyncOperationHandle<IList<TAssetSrc>> item = Addressables.LoadAssetsAsync<TAssetSrc>(locationsList.GetRange(sliceStart, count), null);
						underlyingLoadOperations.Add(item);
						sliceStart += 5;
						yield return null;
					}
					while (sliceStart < arrayLength);
					Addressables.Release(locations);
				}
				finally
				{
					ResourceManager.ExceptionHandler = oldResourceManagerExceptionHandler;
				}
				int num = i + 1;
				i = num;
				void ResourceManagerExceptionHandler(AsyncOperationHandle operationHandle, Exception exception)
				{
					if (!(exception is InvalidKeyException))
					{
						oldResourceManagerExceptionHandler?.Invoke(operationHandle, exception);
					}
				}
			}
			AddressablesLoadAsyncOperationWrapper<TAssetSrc> combinedLoadOperation = new AddressablesLoadAsyncOperationWrapper<TAssetSrc>(underlyingLoadOperations);
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
			while (!combinedLoadOperation.isDone)
			{
				loadOperation.progress = combinedLoadOperation.progress;
				float num2 = (float)stopwatch.Elapsed.TotalSeconds;
				if (num2 > timeoutDuration)
				{
					throw new Exception($"Loading exceeded timeout. elapsedSeconds={num2} timeoutDuration={timeoutDuration}");
				}
				yield return null;
			}
			TAssetSrc[] loadedAssets = combinedLoadOperation.result.Where((TAssetSrc asset) => asset).ToArray();
			loadOperation.progress = 0.97f;
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
					i = 0;
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
			loadOperation.progress = 1f;
		}
	}

	public void AddGenericOperation(IEnumerator coroutine, float weight = 1f)
	{
		Operation item = new Operation
		{
			weight = weight,
			coroutine = coroutine
		};
		allOperations.Add(item);
		allGenericOperations.Add(item);
	}

	public void AddGenericOperation(IEnumerable coroutineProvider, float weight = 1f)
	{
		AddGenericOperation(coroutineProvider.GetEnumerator(), weight);
	}

	public void AddGenericOperation(Func<IEnumerator> coroutineMethod, float weight = 1f)
	{
		AddGenericOperation(coroutineMethod(), weight);
	}

	public void AddGenericOperation(Action action, float weight = 1f)
	{
		AddGenericOperation(Coroutine(), weight);
		IEnumerator Coroutine()
		{
			action();
			yield return null;
		}
	}

	private IEnumerator Coroutine(IProgress<float> progressReceiver)
	{
		while (pendingLoadOperations.Count > 0 || runningLoadOperations.Count > 0)
		{
			while (pendingLoadOperations.Count > 0 && runningLoadOperations.Count < maxConcurrentOperations)
			{
				runningLoadOperations.Add(pendingLoadOperations.Dequeue());
			}
			int i = 0;
			while (i < runningLoadOperations.Count)
			{
				Operation operation = runningLoadOperations[i];
				int num;
				if (operation.coroutine.MoveNext())
				{
					UpdateProgress();
					yield return operation.coroutine.Current;
				}
				else
				{
					runningLoadOperations.RemoveAt(i);
					num = i - 1;
					i = num;
				}
				num = i + 1;
				i = num;
			}
		}
		foreach (Operation genericOperation in allGenericOperations)
		{
			while (genericOperation.coroutine.MoveNext())
			{
				UpdateProgress();
				yield return genericOperation.coroutine.Current;
			}
		}
		progressReceiver.Report(1f);
		void UpdateProgress()
		{
			float num2 = 0f;
			float num3 = 0f;
			for (int j = 0; j < allOperations.Count; j++)
			{
				Operation operation2 = allOperations[j];
				num2 += operation2.weight;
				num3 += operation2.progress * operation2.weight;
			}
			if (num2 == 0f)
			{
				num2 = 1f;
				num3 = 0.5f;
			}
			float num4 = loadOperationsStartedProgress01;
			float num5 = num3 / num2;
			progressReceiver.Report(num4 * 0.25f + num5 * 0.75f);
		}
	}
}
