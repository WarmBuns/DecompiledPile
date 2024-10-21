using System;
using RoR2.UI;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RoR2;

public static class PauseManager
{
	private static GameObject pauseScreenInstance;

	private static GameObject pauseScreenSource;

	public static AsyncOperationHandle<GameObject> loadOperationHandle;

	public static bool IsAbleToPause = true;

	public static int PausingPlayerID = 0;

	public static Action onPauseStartGlobal;

	public static Action onPauseEndGlobal;

	public static bool isPaused = false;

	public static void InitializeLoad()
	{
		if (LegacyResourcesAPI.GetGuid("Prefabs/UI/PauseScreen", out var guid))
		{
			loadOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>(guid);
			loadOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> g)
			{
				pauseScreenSource = g.Result;
			};
		}
	}

	public static bool PauseScreenIsOpen()
	{
		return pauseScreenInstance != null;
	}

	[ConCommand(commandName = "pause", flags = ConVarFlags.None, helpText = "Toggles game pause state.")]
	private static void CCTogglePause(ConCommandArgs args)
	{
		TogglePauseScreen();
	}

	[ConCommand(commandName = "minimal_pause", flags = ConVarFlags.None, helpText = "Toggles game pause state.")]
	private static void CCToggleMinimalPause(ConCommandArgs args)
	{
		OpenMinimalPauseScreen();
	}

	public static void TogglePauseScreen()
	{
		if (!IsAbleToPause)
		{
			return;
		}
		if ((bool)pauseScreenInstance)
		{
			PauseScreenController component = pauseScreenInstance.GetComponent<PauseScreenController>();
			if (component.isMinimallyPause)
			{
				component.OpenGameMenu();
				return;
			}
			component.DestroyPauseScreen(shouldResumeOnDisable: true);
			pauseScreenInstance = null;
		}
		else if (NetworkManager.singleton.isNetworkActive)
		{
			GameObject gameObject = PauseScreenController.InstantiatePauseScreen(NetworkServer.dontListen);
			if (!(gameObject == null))
			{
				pauseScreenInstance = gameObject;
			}
		}
	}

	public static void ClosePauseScreen(bool shouldResume)
	{
		if ((bool)pauseScreenInstance)
		{
			PauseScreenController component = pauseScreenInstance.GetComponent<PauseScreenController>();
			if ((object)component != null)
			{
				component.DestroyPauseScreen(shouldResume);
				pauseScreenInstance = null;
			}
		}
	}

	public static void OpenMinimalPauseScreen()
	{
		if (!pauseScreenInstance && NetworkManager.singleton.isNetworkActive)
		{
			GameObject gameObject = PauseScreenController.InstantiateMinimalPauseScreen();
			if (!(gameObject == null))
			{
				pauseScreenInstance = gameObject;
			}
		}
	}

	public static void CloseMinimalPauseScreen()
	{
		if ((bool)pauseScreenInstance)
		{
			PauseScreenController component = pauseScreenInstance.GetComponent<PauseScreenController>();
			if (!component.isMinimallyPause)
			{
				component.GameplayResumed();
			}
			else if (NetworkManager.singleton.isNetworkActive)
			{
				component.DestroyPauseScreen(shouldResumeOnDisable: false);
				pauseScreenInstance = null;
			}
		}
	}
}
