using System.Collections;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public static class LoadHelper
{
	private static Dictionary<string, AsyncOperationHandle<SceneInstance>> currentSceneLoads = new Dictionary<string, AsyncOperationHandle<SceneInstance>>();

	public static AsyncOperationHandle StartLoadSceneAdditiveAsync(string _sceneGuid)
	{
		AsyncOperationHandle<SceneInstance> asyncOperationHandle = Addressables.LoadSceneAsync(_sceneGuid, LoadSceneMode.Additive, activateOnLoad: false);
		if (!currentSceneLoads.ContainsKey(_sceneGuid))
		{
			currentSceneLoads[_sceneGuid] = asyncOperationHandle;
		}
		return asyncOperationHandle;
	}

	public static IEnumerator LoadSceneAdditivelyAsync(string _sceneGuid)
	{
		AsyncOperationHandle<SceneInstance> handler;
		if (!currentSceneLoads.ContainsKey(_sceneGuid))
		{
			handler = Addressables.LoadSceneAsync(_sceneGuid, LoadSceneMode.Additive, activateOnLoad: false);
			yield return handler;
		}
		else
		{
			handler = currentSceneLoads[_sceneGuid];
			currentSceneLoads.Remove(_sceneGuid);
			yield return handler;
		}
		Scene oldScene = SceneManager.GetActiveScene();
		SceneInstance newScene = handler.Result;
		yield return newScene.ActivateAsync();
		SceneManager.SetActiveScene(newScene.Scene);
		SceneManager.UnloadSceneAsync(oldScene);
	}
}
