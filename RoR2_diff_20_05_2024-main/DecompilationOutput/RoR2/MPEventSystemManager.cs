using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using Rewired;
using Rewired.Integration.UnityUI;
using RoR2.UI;
using UnityEngine;

namespace RoR2;

public class MPEventSystemManager : MonoBehaviour
{
	private static readonly Dictionary<int, MPEventSystem> eventSystems;

	public static bool initialized;

	public static ResourceAvailability availability;

	public static MPEventSystem combinedEventSystem { get; private set; }

	public static MPEventSystem kbmEventSystem { get; private set; }

	public static MPEventSystem primaryEventSystem { get; private set; }

	public static MPEventSystem FindEventSystem(Player inputPlayer)
	{
		eventSystems.TryGetValue(inputPlayer.id, out var value);
		return value;
	}

	public static void Initialize()
	{
		if (initialized)
		{
			return;
		}
		GameObject original = LegacyResourcesAPI.Load<GameObject>("Prefabs/UI/MPEventSystem");
		IList<Player> players = ReInput.players.Players;
		for (int i = 0; i < players.Count; i++)
		{
			GameObject gameObject = Object.Instantiate(original, RoR2Application.instance.transform);
			gameObject.name = string.Format(CultureInfo.InvariantCulture, "MPEventSystem Player{0}", i);
			MPEventSystem component = gameObject.GetComponent<MPEventSystem>();
			RewiredStandaloneInputModule component2 = gameObject.GetComponent<RewiredStandaloneInputModule>();
			Player player = players[i];
			component2.RewiredPlayerIds = new int[1] { player.id };
			gameObject.GetComponent<MPInput>().player = player;
			if (i == 0)
			{
				kbmEventSystem = component;
				component.allowCursorPush = false;
			}
			component.player = players[i];
			eventSystems[players[i].id] = component;
		}
		combinedEventSystem = eventSystems[0];
		combinedEventSystem.isCombinedEventSystem = true;
		RefreshEventSystems();
		initialized = true;
	}

	private static void RefreshEventSystems()
	{
		int count = LocalUserManager.readOnlyLocalUsersList.Count;
		ReadOnlyCollection<MPEventSystem> readOnlyInstancesList = MPEventSystem.readOnlyInstancesList;
		for (int i = 1; i < readOnlyInstancesList.Count; i++)
		{
			readOnlyInstancesList[i].enabled = readOnlyInstancesList[i].localUser != null;
		}
		int num = 0;
		for (int j = 0; j < readOnlyInstancesList.Count; j++)
		{
			readOnlyInstancesList[j].playerSlot = (readOnlyInstancesList[j].enabled ? num++ : (-1));
		}
		primaryEventSystem = ((count > 0) ? LocalUserManager.readOnlyLocalUsersList[0].eventSystem : combinedEventSystem);
		availability.MakeAvailable();
	}

	private void Update()
	{
		if (!initialized)
		{
			if (!ReInput.isReady)
			{
				return;
			}
			Initialize();
		}
		if (!Application.isBatchMode && kbmEventSystem != null)
		{
			CursorLockMode cursorLockMode = ((!kbmEventSystem.isCursorVisible && !combinedEventSystem.isCursorVisible) ? CursorLockMode.Locked : CursorLockMode.Confined);
			if (cursorLockMode != Cursor.lockState)
			{
				Cursor.lockState = cursorLockMode;
			}
			Cursor.visible = false;
		}
	}

	static MPEventSystemManager()
	{
		eventSystems = new Dictionary<int, MPEventSystem>();
		initialized = false;
		LocalUserManager.onLocalUsersUpdated += RefreshEventSystems;
	}
}
