using System;
using System.IO;
using System.Text;
using Facepunch.Steamworks;
using HG;
using RoR2.Networking;
using SteamAPIValidator;
using UnityEngine;

namespace RoR2;

public sealed class SteamworksClientManager : IDisposable
{
	private bool disposed;

	public static SteamworksClientManager instance { get; private set; }

	public Client steamworksClient { get; private set; }

	private static event Action _onLoaded;

	public static event Action onLoaded
	{
		add
		{
			if (instance == null)
			{
				_onLoaded += value;
			}
			else if (!instance.disposed)
			{
				value();
			}
		}
		remove
		{
			_onLoaded -= value;
		}
	}

	private SteamworksClientManager()
	{
		if (!Application.isEditor && File.Exists("steam_appid.txt"))
		{
			try
			{
				File.Delete("steam_appid.txt");
			}
			catch (Exception)
			{
			}
			if (File.Exists("steam_appid.txt"))
			{
				Dispose();
				return;
			}
		}
		Config.ForUnity(Application.platform.ToString());
		steamworksClient = new Client(632360u);
		if (!steamworksClient.IsValid)
		{
			RoR2Application.HardStopPlayInEditor("Steam Init Failed", "Steam Init failed. Steam is likely not running.");
			Dispose();
			return;
		}
		if (!Application.isEditor)
		{
			if (Client.RestartIfNecessary(632360u) || !steamworksClient.IsValid || !SteamApiValidator.IsValidSteamApiDll())
			{
				Dispose();
				return;
			}
			if (!steamworksClient.App.IsSubscribed(632360u))
			{
				Dispose();
				return;
			}
		}
		RoR2Application.steamBuildId = TextSerialization.ToStringInvariant(steamworksClient.BuildId);
		RoR2Application.onUpdate += Update;
		RoR2Application.cloudStorage = new SteamworksRemoteStorageFileSystem();
		(PlatformSystems.lobbyManager as SteamworksLobbyManager)?.Init();
	}

	private void Update()
	{
		steamworksClient.Update();
	}

	public void Dispose()
	{
		if (disposed)
		{
			return;
		}
		disposed = true;
		RoR2Application.onUpdate -= Update;
		if (steamworksClient != null)
		{
			if ((bool)NetworkManagerSystem.singleton)
			{
				NetworkManagerSystem.singleton.ForceCloseAllConnections();
			}
			steamworksClient.Lobby.Leave();
			if (Server.Instance != null)
			{
				Server.Instance.Dispose();
			}
			steamworksClient.Update();
			steamworksClient.Dispose();
			steamworksClient = null;
		}
	}

	public static void Init()
	{
		RoR2Application.loadSteamworksClient = delegate
		{
			instance = new SteamworksClientManager();
			if (!instance.disposed)
			{
				SteamworksClientManager._onLoaded?.Invoke();
				SteamworksClientManager._onLoaded = null;
				return true;
			}
			return false;
		};
		RoR2Application.unloadSteamworksClient = delegate
		{
			instance?.Dispose();
			instance = null;
		};
	}

	[ConCommand(commandName = "steamworks_client_print_p2p_connection_status", flags = ConVarFlags.None, helpText = "Prints debug information for any established P2P connection to the specified Steam ID.")]
	private static void CCSteamworksClientPrintP2PConnectionStatus(ConCommandArgs args)
	{
		Facepunch.Steamworks.Networking.P2PSessionState sessionState = default(Facepunch.Steamworks.Networking.P2PSessionState);
		if (Client.Instance.Networking.GetP2PSessionState(args.GetArgPlatformID(0).ID, ref sessionState))
		{
			StringBuilder stringBuilder = HG.StringBuilderPool.RentStringBuilder();
			stringBuilder.Append("BytesQueuedForSend").Append("=").Append(sessionState.BytesQueuedForSend)
				.AppendLine();
			stringBuilder.Append("Connecting").Append("=").Append(sessionState.Connecting)
				.AppendLine();
			stringBuilder.Append("ConnectionActive").Append("=").Append(sessionState.ConnectionActive)
				.AppendLine();
			stringBuilder.Append("PacketsQueuedForSend").Append("=").Append(sessionState.PacketsQueuedForSend)
				.AppendLine();
			stringBuilder.Append("P2PSessionError").Append("=").Append(sessionState.P2PSessionError)
				.AppendLine();
			stringBuilder.Append("UsingRelay").Append("=").Append(sessionState.UsingRelay)
				.AppendLine();
			HG.StringBuilderPool.ReturnStringBuilder(stringBuilder);
		}
	}
}
