using UnityEngine;

namespace RoR2;

public static class WwiseIntegrationManager
{
	private static GameObject wwiseGlobalObjectInstance;

	private static GameObject wwiseAudioObjectInstance;

	public static bool noAudio => false;

	public static void Init()
	{
		if (noAudio)
		{
			return;
		}
		if ((bool)Object.FindObjectOfType<AkInitializer>())
		{
			Debug.LogError("Attempting to initialize wwise when AkInitializer already exists! This will cause a crash!");
			return;
		}
		LegacyResourcesAPI.LoadAsyncCallback("Prefabs/WwiseGlobal", delegate(GameObject x)
		{
			wwiseGlobalObjectInstance = Object.Instantiate(x);
		});
		LegacyResourcesAPI.LoadAsyncCallback("Prefabs/AudioManager", delegate(GameObject x)
		{
			wwiseAudioObjectInstance = Object.Instantiate(x);
		});
	}
}
