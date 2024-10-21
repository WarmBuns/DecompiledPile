using System;
using System.Globalization;
using Facepunch.Steamworks;
using JetBrains.Annotations;
using RoR2.Networking;
using UnityEngine;

namespace RoR2;

internal static class SteamworksInviteManager
{
	private const string rpConnect = "connect";

	private const string rpStatus = "status";

	private static void SetKeyValue([NotNull] string key, [CanBeNull] string value)
	{
		Client.Instance.User.SetRichPresence(key, value);
	}

	private static void OnNetworkStart()
	{
		string text = null;
		PlatformID platformID = PlatformID.nil;
		PlatformID platformID2 = PlatformID.nil;
		string[] commandLineArgs = Environment.GetCommandLineArgs();
		for (int i = 0; i < commandLineArgs.Length - 1; i++)
		{
			switch (commandLineArgs[i].ToLower(CultureInfo.InvariantCulture))
			{
			case "+connect":
			{
				if (AddressPortPair.TryParse(commandLineArgs[i + 1], out var addressPortPair))
				{
					text = addressPortPair.address + ":" + addressPortPair.port;
				}
				break;
			}
			case "+connect_steamworks_p2p":
			{
				if (PlatformID.TryParse(commandLineArgs[i + 1], out var result2))
				{
					platformID = result2;
				}
				break;
			}
			case "+steam_lobby_join":
			{
				if (PlatformID.TryParse(commandLineArgs[i + 1], out var result))
				{
					platformID2 = result;
				}
				break;
			}
			}
		}
		if (platformID2 != PlatformID.nil)
		{
			Console.instance.SubmitCmd(null, "steam_lobby_join " + platformID2.value);
		}
		else if (platformID != PlatformID.nil)
		{
			Console.instance.SubmitCmd(null, "connect_steamworks_p2p " + platformID.value);
		}
		else if (text != null)
		{
			Console.instance.SubmitCmd(null, "connect " + text);
		}
	}

	private static void OnLobbyChanged()
	{
		if (PlatformSystems.lobbyManager.isInLobby)
		{
			SetKeyValue("connect", "+steam_lobby_join " + PlatformSystems.lobbyManager.GetLobbyID());
		}
		else
		{
			SetKeyValue("connect", null);
		}
	}

	private static void OnInvitedToGame(SteamFriend steamFriend, string connectString)
	{
		Debug.LogFormat("OnGameRichPresenceJoinRequested connectString=\"{0}\" steamFriend=\"{1}\"", connectString, steamFriend.Name);
		string[] array = connectString.Split(' ');
		if (array.Length < 2)
		{
			return;
		}
		if (array[0] == "+connect_steamworks_p2p" && PlatformID.TryParse(array[1], out var result))
		{
			if (!PlatformSystems.lobbyManager.ownsLobby)
			{
				PlatformSystems.lobbyManager.LeaveLobby();
			}
			QuitConfirmationHelper.IssueQuitCommand(null, "connect_steamworks_p2p " + result.value);
		}
		if (!(array[0] == "+steam_lobby_join"))
		{
			return;
		}
		if (!(PlatformSystems.lobbyManager as PCLobbyManager).CheckLobbyIdValidity(array[1]))
		{
			if (PlatformSystems.ShouldUseEpicOnlineSystems)
			{
				PCLobbyManager.ShowEnableCrossPlayPopup(isLobbyCrossplay: false);
			}
			else
			{
				PCLobbyManager.ShowEnableCrossPlayPopup(isLobbyCrossplay: true);
			}
			return;
		}
		bool shouldUseEpicOnlineSystems = PlatformSystems.ShouldUseEpicOnlineSystems;
		if (PlatformID.TryParse(array[1], out var result2) || shouldUseEpicOnlineSystems)
		{
			if (!PlatformSystems.lobbyManager.ownsLobby)
			{
				PlatformSystems.lobbyManager.LeaveLobby();
			}
			string text = (shouldUseEpicOnlineSystems ? array[1] : result2.value.ToString());
			QuitConfirmationHelper.IssueQuitCommand(null, "steam_lobby_join " + text);
		}
	}

	private static void OnGameServerChangeRequested(string address, string password)
	{
		Debug.LogFormat("OnGameServerChangeRequested address=\"{0}\"", address);
		if (!PlatformSystems.lobbyManager.ownsLobby)
		{
			PlatformSystems.lobbyManager.LeaveLobby();
		}
		string consoleCmd = $"cl_password \"{Util.EscapeQuotes(password)}\"; connect \"{Util.EscapeQuotes(address)}\"";
		QuitConfirmationHelper.IssueQuitCommand(null, consoleCmd);
	}

	private static void SetupCallbacks()
	{
		NetworkManagerSystem.onStartGlobal += OnNetworkStart;
		LobbyManager lobbyManager = PlatformSystems.lobbyManager;
		lobbyManager.onLobbyChanged = (Action)Delegate.Combine(lobbyManager.onLobbyChanged, new Action(OnLobbyChanged));
		if (Client.Instance != null)
		{
			Client.Instance.Friends.OnInvitedToGame += OnInvitedToGame;
			Client.Instance.Friends.OnGameServerChangeRequested = OnGameServerChangeRequested;
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void Init()
	{
		SteamworksClientManager.onLoaded += SetupCallbacks;
	}
}
