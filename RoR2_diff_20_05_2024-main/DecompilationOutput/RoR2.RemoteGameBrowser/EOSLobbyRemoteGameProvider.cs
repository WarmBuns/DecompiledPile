using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;
using HG;
using RoR2.Networking;

namespace RoR2.RemoteGameBrowser;

public class EOSLobbyRemoteGameProvider : BaseAsyncRemoteGameProvider
{
	private readonly List<LobbyDetails> lobbyList = new List<LobbyDetails>();

	private int waitingForLobbyCount;

	private static readonly char[] tagsSeparator = new char[1] { ',' };

	private EOSLobbyManager.Filter BuildFilter()
	{
		EOSLobbyManager.Filter filter = new EOSLobbyManager.Filter();
		if (!searchFilters.allowMismatchedMods)
		{
			AttributeData attributeData = default(AttributeData);
			attributeData.Key = "_mh";
			attributeData.Value = NetworkModCompatibilityHelper.networkModHash;
			AttributeData item = attributeData;
			filter.SearchData.Add(item);
		}
		return filter;
	}

	public override bool RequestRefresh()
	{
		EOSLobbyManager.Filter filter = BuildFilter();
		bool num = (PlatformSystems.lobbyManager as EOSLobbyManager).RequestLobbyList(this, filter, OnLobbyListReceived);
		if (num)
		{
			waitingForLobbyCount++;
		}
		return num;
	}

	private void OnLobbyListReceived(List<LobbyDetails> lobbies)
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
		LobbyDetails[] lobbies;
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

	private static void CreateRemoteGameInfo(LobbyDetails lobby, out RemoteGameInfo result)
	{
		result = default(RemoteGameInfo);
		LobbyDetailsCopyInfoOptions options = default(LobbyDetailsCopyInfoOptions);
		if (lobby.CopyInfo(ref options, out var outLobbyDetailsInfo) == Result.Success)
		{
			result.name = outLobbyDetailsInfo.Value.LobbyOwnerUserId.ToString();
			result.lobbyName = outLobbyDetailsInfo.Value.LobbyId;
			result.lobbyIdStr = outLobbyDetailsInfo.Value.LobbyId;
			string lobbyStringValue = EOSLobbyManager.GetLobbyStringValue(lobby, "_map");
			result.serverIdStr = EOSLobbyManager.GetLobbyStringValue(lobby, "server_id");
			result.serverAddress = GetAddressPortPair("server_address");
			result.serverName = EOSLobbyManager.GetLobbyStringValue(lobby, "_svnm");
			result.lobbyPlayerCount = GetInt("player_count", 1, int.MaxValue) ?? 1;
			result.lobbyMaxPlayers = GetInt("total_max_players", 1, int.MaxValue) ?? 1;
			result.serverPlayerCount = GetInt("_svplc", 0, int.MaxValue);
			result.serverMaxPlayers = GetInt("_svmpl", 0, int.MaxValue);
			result.inGame = result.IsServerIdValid() || result.serverAddress.HasValue;
			result.currentSceneName = lobbyStringValue;
			if (lobbyStringValue != null)
			{
				result.currentSceneIndex = SceneCatalog.GetSceneDefFromSceneName(lobbyStringValue)?.sceneDefIndex;
			}
			result.requestRefreshImplementation = RemoteGameInfoRequestRefresh;
			result.getPlayersImplementation = RemoteGameInfoGetPlayers;
			result.getRuleBookImplementation = RemoteGameInfoGetRuleBook;
			result.userData = lobby;
			result.hasPassword = GetBool("_pw");
			result.gameModeName = EOSLobbyManager.GetLobbyStringValue(lobby, "_svgm");
			result.buildId = EOSLobbyManager.GetLobbyStringValue(lobby, "build_id") ?? "UNKNOWN";
			result.modHash = EOSLobbyManager.GetLobbyStringValue(lobby, "_mh") ?? "UNKNOWN";
			result.SetTags(GetTags("_svtags"));
			result.CalcExtraFields();
		}
		AddressPortPair? GetAddressPortPair(string key)
		{
			if (!AddressPortPair.TryParse(EOSLobbyManager.GetLobbyStringValue(lobby, key), out var addressPortPair))
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
			if (int.TryParse(EOSLobbyManager.GetLobbyStringValue(lobby, key), out var result2) && min <= result2 && result2 <= max)
			{
				return result2;
			}
			return null;
		}
		string[] GetTags(string key)
		{
			return EOSLobbyManager.GetLobbyStringValue(lobby, key)?.Split(tagsSeparator, StringSplitOptions.None);
		}
	}

	private static void RemoteGameInfoRequestRefresh(in RemoteGameInfo remoteGameInfo, RemoteGameInfo.RequestRefreshSuccessCallback successCallback, Action failureCallback, bool fetchDetails)
	{
		if (remoteGameInfo.userData is LobbyDetails)
		{
			CreateRemoteGameInfo(remoteGameInfo.userData as LobbyDetails, out var result);
			successCallback?.Invoke(in result);
		}
	}

	private static bool RemoteGameInfoGetRuleBook(in RemoteGameInfo remoteGameInfo, RuleBook dest)
	{
		if (remoteGameInfo.userData is LobbyDetails lobbyDetails)
		{
			KeyValueUnsplitter keyValueUnsplitter = new KeyValueUnsplitter("rulebook");
			List<KeyValuePair<string, string>> list = CollectionPool<KeyValuePair<string, string>, List<KeyValuePair<string, string>>>.RentCollection();
			try
			{
				LobbyDetailsGetAttributeCountOptions options = default(LobbyDetailsGetAttributeCountOptions);
				uint num = 0u;
				for (uint attributeCount = lobbyDetails.GetAttributeCount(ref options); num < attributeCount; num++)
				{
					LobbyDetailsCopyAttributeByIndexOptions lobbyDetailsCopyAttributeByIndexOptions = default(LobbyDetailsCopyAttributeByIndexOptions);
					lobbyDetailsCopyAttributeByIndexOptions.AttrIndex = num;
					LobbyDetailsCopyAttributeByIndexOptions options2 = lobbyDetailsCopyAttributeByIndexOptions;
					if (lobbyDetails.CopyAttributeByIndex(ref options2, out var outAttribute) == Result.Success)
					{
						AttributeData value = outAttribute.Value.Data.Value;
						list.Add(new KeyValuePair<string, string>(value.Key.ToString().ToLower(), value.Value.AsUtf8.ToString().ToLower()));
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
		if (remoteGameInfo.userData is LobbyDetails lobbyDetails)
		{
			LobbyDetailsGetMemberCountOptions options = default(LobbyDetailsGetMemberCountOptions);
			uint memberCount = lobbyDetails.GetMemberCount(ref options);
			for (uint num = 0u; num < memberCount; num++)
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
