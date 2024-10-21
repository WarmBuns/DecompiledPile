using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Facepunch.Steamworks;
using HG;
using RoR2.Networking;

namespace RoR2.RemoteGameBrowser;

public class SteamworksLobbyRemoteGameProvider : BaseAsyncRemoteGameProvider
{
	private readonly List<LobbyList.Lobby> lobbyList = new List<LobbyList.Lobby>();

	private int waitingForLobbyCount;

	private static readonly char[] tagsSeparator = new char[1] { ',' };

	private LobbyList.Filter BuildFilter()
	{
		LobbyList.Filter filter = new LobbyList.Filter();
		if (!searchFilters.allowMismatchedMods)
		{
			filter.StringFilters.Add("_mh", NetworkModCompatibilityHelper.networkModHash);
		}
		return filter;
	}

	public override bool RequestRefresh()
	{
		LobbyList.Filter filter = BuildFilter();
		bool num = (PlatformSystems.lobbyManager as SteamworksLobbyManager).RequestLobbyList(this, filter, OnLobbyListReceived);
		if (num)
		{
			waitingForLobbyCount++;
		}
		return num;
	}

	private void OnLobbyListReceived(List<LobbyList.Lobby> lobbies)
	{
		if (base.disposed)
		{
			return;
		}
		waitingForLobbyCount--;
		lock (lobbyList)
		{
			lobbyList.Clear();
			lobbyList.AddRange(lobbies);
			SetDirty();
		}
	}

	protected override Task<RemoteGameInfo[]> CreateTask(CancellationToken cancellationToken)
	{
		LobbyList.Lobby[] lobbies;
		lock (lobbyList)
		{
			lobbies = lobbyList.ToArray();
		}
		return new Task<RemoteGameInfo[]>(delegate
		{
			RemoteGameInfo[] array = new RemoteGameInfo[lobbies.Length];
			for (int i = 0; i < lobbies.Length; i++)
			{
				CreateRemoteGameInfo(lobbies[i], out array[i]);
			}
			return array;
		}, cancellationToken);
	}

	private static void CreateRemoteGameInfo(LobbyList.Lobby lobby, out RemoteGameInfo result)
	{
		result = default(RemoteGameInfo);
		result.name = lobby.Name;
		result.lobbyName = lobby.Name;
		result.lobbyId = lobby.LobbyID;
		string text = GetString("_map");
		result.serverId = GetULong("server_id");
		result.serverAddress = GetAddressPortPair("server_address");
		result.serverName = GetString("_svnm");
		result.lobbyPlayerCount = GetInt("player_count", 1, int.MaxValue) ?? 1;
		result.lobbyMaxPlayers = GetInt("total_max_players", 1, int.MaxValue) ?? 1;
		result.serverPlayerCount = GetInt("_svplc", 0, int.MaxValue);
		result.serverMaxPlayers = GetInt("_svmpl", 0, int.MaxValue);
		result.inGame = result.serverId.HasValue || result.serverAddress.HasValue;
		result.currentSceneName = text;
		if (text != null)
		{
			result.currentSceneIndex = SceneCatalog.GetSceneDefFromSceneName(text)?.sceneDefIndex;
		}
		result.requestRefreshImplementation = RemoteGameInfoRequestRefresh;
		result.getPlayersImplementation = RemoteGameInfoGetPlayers;
		result.getRuleBookImplementation = RemoteGameInfoGetRuleBook;
		result.userData = lobby;
		result.hasPassword = GetBool("_pw");
		result.gameModeName = GetString("_svgm");
		result.buildId = GetString("build_id") ?? "UNKNOWN";
		result.modHash = GetString("_mh") ?? "UNKNOWN";
		result.SetTags(GetTags("_svtags"));
		result.CalcExtraFields();
		AddressPortPair? GetAddressPortPair(string key)
		{
			if (!AddressPortPair.TryParse(GetString(key), out var addressPortPair))
			{
				return null;
			}
			return addressPortPair;
		}
		bool? GetBool(string key)
		{
			return GetInt(key, int.MinValue, int.MaxValue) > 0;
		}
		int? GetInt(string key, int min, int max)
		{
			if (int.TryParse(GetString(key), out var result3) && min <= result3 && result3 <= max)
			{
				return result3;
			}
			return null;
		}
		string GetString(string key)
		{
			string data = lobby.GetData(key);
			if (!(data == string.Empty))
			{
				return data;
			}
			return null;
		}
		string[] GetTags(string key)
		{
			return GetString(key)?.Split(tagsSeparator, StringSplitOptions.None);
		}
		ulong? GetULong(string key)
		{
			if (!ulong.TryParse(GetString(key), out var result2))
			{
				return null;
			}
			return result2;
		}
	}

	private static void RemoteGameInfoRequestRefresh(in RemoteGameInfo remoteGameInfo, RemoteGameInfo.RequestRefreshSuccessCallback successCallback, Action failureCallback, bool fetchDetails)
	{
		if (!(remoteGameInfo.userData is LobbyList.Lobby lobby))
		{
			return;
		}
		Task<LobbyList.Lobby> lobby2 = Client.Instance.LobbyList.GetLobby(lobby.LobbyID);
		if (lobby2 == null)
		{
			failureCallback?.Invoke();
			return;
		}
		lobby2.ContinueWith(delegate(Task<LobbyList.Lobby> antecedentTask)
		{
			RoR2Application.onNextUpdate += delegate
			{
				HandleRefresh(antecedentTask);
			};
		});
		void HandleRefresh(Task<LobbyList.Lobby> antecedentTask)
		{
			LobbyList.Lobby result = antecedentTask.Result;
			if (result == null)
			{
				failureCallback?.Invoke();
			}
			else
			{
				CreateRemoteGameInfo(result, out var result2);
				successCallback?.Invoke(in result2);
			}
		}
	}

	private static bool RemoteGameInfoGetRuleBook(in RemoteGameInfo remoteGameInfo, RuleBook dest)
	{
		if (remoteGameInfo.userData is LobbyList.Lobby lobby)
		{
			KeyValueUnsplitter keyValueUnsplitter = new KeyValueUnsplitter("rulebook");
			List<KeyValuePair<string, string>> list = CollectionPool<KeyValuePair<string, string>, List<KeyValuePair<string, string>>>.RentCollection();
			try
			{
				int i = 0;
				for (int dataCount = lobby.GetDataCount(); i < dataCount; i++)
				{
					if (lobby.GetDataByIndex(i, out var key, out var value))
					{
						list.Add(new KeyValuePair<string, string>(key, value));
					}
				}
				string value2 = keyValueUnsplitter.GetValue(list);
				if (!string.IsNullOrEmpty(value2))
				{
					RuleBook.ReadBase64(value2, dest);
					return true;
				}
			}
			finally
			{
				list = CollectionPool<KeyValuePair<string, string>, List<KeyValuePair<string, string>>>.ReturnCollection(list);
			}
		}
		return false;
	}

	private static void RemoteGameInfoGetPlayers(in RemoteGameInfo remoteGameInfo, List<RemotePlayerInfo> output)
	{
		if (remoteGameInfo.userData is LobbyList.Lobby lobby)
		{
			for (int i = 0; i < lobby.NumMembers; i++)
			{
				output.Add(new RemotePlayerInfo
				{
					id = 0uL,
					name = "???"
				});
			}
		}
	}

	public override bool IsBusy()
	{
		if (!base.IsBusy())
		{
			return waitingForLobbyCount > 0;
		}
		return true;
	}
}
