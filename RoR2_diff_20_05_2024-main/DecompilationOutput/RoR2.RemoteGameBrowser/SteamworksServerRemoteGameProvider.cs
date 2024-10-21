using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Facepunch.Steamworks;
using RoR2.Networking;

namespace RoR2.RemoteGameBrowser;

public class SteamworksServerRemoteGameProvider : BaseAsyncRemoteGameProvider
{
	public new struct SearchFilters : IEquatable<SearchFilters>
	{
		public bool allowDedicatedServers;

		public bool allowListenServers;

		public bool mustHavePlayers;

		public bool mustNotBeFull;

		public bool allowInProgressGames;

		public bool allowMismatchedMods;

		public string requiredTags;

		public string forbiddenTags;

		public bool Equals(SearchFilters other)
		{
			if (allowDedicatedServers == other.allowDedicatedServers && allowListenServers == other.allowListenServers && mustHavePlayers == other.mustHavePlayers && mustNotBeFull == other.mustNotBeFull && allowInProgressGames == other.allowInProgressGames && allowMismatchedMods == other.allowMismatchedMods && string.Equals(requiredTags, other.requiredTags))
			{
				return string.Equals(forbiddenTags, other.forbiddenTags);
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			if (obj is SearchFilters other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (((((((((((((allowDedicatedServers.GetHashCode() * 397) ^ allowListenServers.GetHashCode()) * 397) ^ mustHavePlayers.GetHashCode()) * 397) ^ mustNotBeFull.GetHashCode()) * 397) ^ allowInProgressGames.GetHashCode()) * 397) ^ allowMismatchedMods.GetHashCode()) * 397) ^ ((requiredTags != null) ? requiredTags.GetHashCode() : 0)) * 397) ^ ((forbiddenTags != null) ? forbiddenTags.GetHashCode() : 0);
		}
	}

	public enum Mode
	{
		Internet,
		Favorites,
		History,
		Local,
		Friends
	}

	private struct FilterElement
	{
		public string key;

		public string value;

		public int operandCount;

		public bool isOperator;
	}

	private Mode mode;

	private ServerList.Filter filter;

	private ServerList.Request currentRequest;

	public new bool refreshOnFiltersChanged;

	private static readonly int k_cbMaxGameServerGameData = 2048;

	private static readonly int k_cbMaxGameServerGameDescription = 64;

	private static readonly int k_cbMaxGameServerGameDir = 32;

	private static readonly int k_cbMaxGameServerMapName = 32;

	private static readonly int k_cbMaxGameServerName = 64;

	private static readonly int k_cbMaxGameServerTags = 128;

	private new SearchFilters searchFilters;

	public SteamworksServerRemoteGameProvider(Mode mode)
	{
		this.mode = mode;
		filter = new ServerList.Filter();
	}

	public new SearchFilters GetSearchFilters()
	{
		return searchFilters;
	}

	public void SetSearchFilters(SearchFilters newSearchFilters)
	{
		if (!searchFilters.Equals(newSearchFilters))
		{
			searchFilters = newSearchFilters;
			BuildFilter();
			if (refreshOnFiltersChanged)
			{
				RequestRefresh();
			}
			SetDirty();
		}
	}

	private void BuildFilter()
	{
		filter.Clear();
		FilterElement[] elements = new FilterElement[64];
		int elementCount = 0;
		Stack<(int, int)> operatorIndicesAndCounts = new Stack<(int, int)>();
		int currentOperandCount = 0;
		PushOperation("and");
		AddOperand("appid", Client.Instance.AppId.ToString());
		if (searchFilters.allowDedicatedServers != searchFilters.allowListenServers)
		{
			if (searchFilters.allowListenServers)
			{
				PushOperation("nor");
				AddOperand("dedicated", "");
				PopOperation();
			}
			else
			{
				AddOperand("dedicated", "");
			}
		}
		if (searchFilters.mustHavePlayers)
		{
			AddOperand("hasplayers", "");
		}
		if (searchFilters.mustNotBeFull)
		{
			AddOperand("notfull", "");
		}
		if (!searchFilters.allowInProgressGames)
		{
			AddOperand("map", "lobby");
		}
		if (!string.IsNullOrEmpty(searchFilters.requiredTags))
		{
			AddOperand("gametagsand", searchFilters.requiredTags);
		}
		if (!string.IsNullOrEmpty(searchFilters.forbiddenTags))
		{
			AddOperand("gametagsnor", searchFilters.forbiddenTags);
		}
		AddOperand("gamedataand", ServerManagerBase<SteamworksServerManager>.GetVersionGameDataString());
		if (!searchFilters.allowMismatchedMods)
		{
			AddOperand("gamedataand", NetworkModCompatibilityHelper.steamworksGameserverGameDataValue);
		}
		PopOperation();
		for (int j = 0; j < elementCount; j++)
		{
			ref FilterElement reference = ref elements[j];
			filter.Add(reference.key, reference.isOperator ? reference.operandCount.ToString() : reference.value);
		}
		void AddOperand(string key, string value)
		{
			int num3 = currentOperandCount + 1;
			currentOperandCount = num3;
			GetElement(elementCount++) = new FilterElement
			{
				key = key,
				value = value,
				isOperator = false
			};
		}
		ref FilterElement GetElement(int i)
		{
			if (elements.Length < i)
			{
				Array.Resize(ref elements, elements.Length * 2);
			}
			return ref elements[i];
		}
		void PopOperation()
		{
			int num4 = currentOperandCount;
			int i2;
			(i2, currentOperandCount) = operatorIndicesAndCounts.Pop();
			currentOperandCount += num4;
			GetElement(i2).operandCount = num4;
		}
		void PushOperation(string operationCode)
		{
			int num = currentOperandCount + 1;
			currentOperandCount = num;
			int num2 = elementCount++;
			GetElement(num2) = new FilterElement
			{
				key = operationCode,
				isOperator = true
			};
			operatorIndicesAndCounts.Push((num2, currentOperandCount));
			currentOperandCount = 0;
		}
	}

	public override bool RequestRefresh()
	{
		CancelCurrentRequest();
		currentRequest = CreateRequest(mode, filter);
		currentRequest.OnUpdate = OnCurrentRequestUpdated;
		currentRequest.OnFinished = OnCurrentRequestFinished;
		currentRequest.OnServerResponded = OnCurrentRequestServerResponded;
		return true;
	}

	private static Func<ServerList.Filter, ServerList.Request> GetRequestMethod(Mode mode)
	{
		ServerList serverList = Client.Instance.ServerList;
		return mode switch
		{
			Mode.Internet => serverList.Internet, 
			Mode.Favorites => serverList.Favourites, 
			Mode.History => serverList.History, 
			Mode.Local => serverList.Local, 
			Mode.Friends => serverList.Friends, 
			_ => throw new ArgumentOutOfRangeException(), 
		};
	}

	private static ServerList.Request CreateRequest(Mode mode, ServerList.Filter filter)
	{
		return GetRequestMethod(mode)(filter);
	}

	private void OnCurrentRequestServerResponded(ServerList.Server server)
	{
		SetDirty();
	}

	private void OnCurrentRequestFinished()
	{
		SetDirty();
	}

	private void OnCurrentRequestUpdated()
	{
		SetDirty();
	}

	private void CancelCurrentRequest()
	{
		if (currentRequest != null)
		{
			currentRequest.OnFinished = null;
			currentRequest.OnServerResponded = null;
			currentRequest.OnUpdate = null;
			currentRequest.Dispose();
			currentRequest = null;
		}
	}

	private static RemoteGameInfo[] GenerateRemoteGameInfoCache(ServerList.Server[] knownServers, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		RemoteGameInfo[] array = new RemoteGameInfo[knownServers.Length];
		for (int i = 0; i < knownServers.Length; i++)
		{
			cancellationToken.ThrowIfCancellationRequested();
			ServerList.Server serverInfo = knownServers[i];
			GenerateRemoteGameInfo(ref array[i], serverInfo);
		}
		return array;
	}

	protected override Task<RemoteGameInfo[]> CreateTask(CancellationToken cancellationToken)
	{
		if (currentRequest == null)
		{
			return new Task<RemoteGameInfo[]>(() => Array.Empty<RemoteGameInfo>());
		}
		ServerList.Server[] servers = currentRequest.Responded.ToArray();
		return new Task<RemoteGameInfo[]>(() => GenerateRemoteGameInfoCache(servers, cancellationToken), cancellationToken);
	}

	public override void Dispose()
	{
		CancelCurrentRequest();
		base.Dispose();
	}

	public override bool IsBusy()
	{
		if (!base.IsBusy())
		{
			return !(currentRequest?.Finished ?? true);
		}
		return true;
	}

	private static void GenerateRemoteGameInfo(ref RemoteGameInfo remoteGameInfo, ServerList.Server serverInfo)
	{
		remoteGameInfo.name = serverInfo.Name;
		remoteGameInfo.serverAddress = new AddressPortPair(serverInfo.Address, (ushort)serverInfo.ConnectionPort);
		remoteGameInfo.serverName = serverInfo.Name;
		remoteGameInfo.serverId = serverInfo.SteamId;
		remoteGameInfo.hasPassword = serverInfo.Passworded;
		remoteGameInfo.serverPlayerCount = serverInfo.Players;
		remoteGameInfo.serverMaxPlayers = serverInfo.MaxPlayers;
		remoteGameInfo.ping = serverInfo.Ping;
		remoteGameInfo.SetTags(serverInfo.Tags);
		remoteGameInfo.isFavorite = serverInfo.Favourite;
		remoteGameInfo.userData = serverInfo;
		remoteGameInfo.getPlayersImplementation = RemoteGameInfoGetPlayers;
		remoteGameInfo.requestRefreshImplementation = RemoteGameInfoRequestRefresh;
		remoteGameInfo.getRuleBookImplementation = RemoteGameInfoGetRuleBook;
		remoteGameInfo.currentSceneName = serverInfo.Map;
		remoteGameInfo.currentSceneIndex = SceneCatalog.GetSceneDefFromSceneName(serverInfo.Map)?.sceneDefIndex;
		remoteGameInfo.CalcExtraFields();
	}

	private static void RemoteGameInfoRequestRefresh(in RemoteGameInfo remoteGameInfo, RemoteGameInfo.RequestRefreshSuccessCallback successCallback, Action failureCallback, bool fetchDetails)
	{
		if (remoteGameInfo.userData is ServerList.Server)
		{
			ServerList.Filter filter = new ServerList.Filter();
			filter.Add("and", "2");
			filter.Add("appid", Client.Instance.AppId.ToString());
			filter.Add("gameaddr", remoteGameInfo.serverAddress.ToString());
			ServerList.Request lanRefreshRequest = CreateRequest(Mode.Local, filter);
			lanRefreshRequest.OnFinished = delegate
			{
				ServerList.Server server = GetRefreshedServerInfoFromRequest(lanRefreshRequest);
				if (server != null)
				{
					HandleNewInfo(server);
				}
				else
				{
					ServerList.Request internetRefreshRequest = CreateRequest(Mode.Internet, filter);
					internetRefreshRequest.OnFinished = delegate
					{
						ServerList.Server server2 = GetRefreshedServerInfoFromRequest(internetRefreshRequest);
						if (server2 != null)
						{
							HandleNewInfo(server2);
						}
						else
						{
							failureCallback?.Invoke();
						}
					};
				}
			};
		}
		else
		{
			failureCallback?.Invoke();
		}
		static ServerList.Server GetRefreshedServerInfoFromRequest(ServerList.Request request)
		{
			if (request.Responded.Count > 0)
			{
				return request.Responded[0];
			}
			if (request.Unresponsive.Count > 0)
			{
				return request.Unresponsive[0];
			}
			return null;
		}
		void HandleNewInfo(ServerList.Server refreshedServerInfo)
		{
			RemoteGameInfo result = default(RemoteGameInfo);
			GenerateRemoteGameInfo(ref result, refreshedServerInfo);
			int awaitingRequests;
			if (fetchDetails)
			{
				awaitingRequests = 2;
				ServerList.Server server3 = refreshedServerInfo;
				server3.OnReceivedPlayerInfos = (Action<bool>)Delegate.Combine(server3.OnReceivedPlayerInfos, new Action<bool>(OnReceivedPlayerInfos));
				ServerList.Server server4 = refreshedServerInfo;
				server4.OnReceivedRules = (Action<bool>)Delegate.Combine(server4.OnReceivedRules, new Action<bool>(OnReceivedRules));
				refreshedServerInfo.FetchPlayerInfos();
				refreshedServerInfo.FetchRules();
			}
			else
			{
				successCallback?.Invoke(in result);
			}
			void OnReceivedPlayerInfos(bool success)
			{
				ServerList.Server server5 = refreshedServerInfo;
				server5.OnReceivedPlayerInfos = (Action<bool>)Delegate.Remove(server5.OnReceivedPlayerInfos, new Action<bool>(OnReceivedPlayerInfos));
				int num = awaitingRequests - 1;
				awaitingRequests = num;
				TryComplete();
			}
			void OnReceivedRules(bool success)
			{
				ServerList.Server server6 = refreshedServerInfo;
				server6.OnReceivedRules = (Action<bool>)Delegate.Remove(server6.OnReceivedRules, new Action<bool>(OnReceivedRules));
				if (success && refreshedServerInfo.HasRules)
				{
					refreshedServerInfo.Rules.TryGetValue("gameMode", out result.gameModeName);
					refreshedServerInfo.Rules.TryGetValue("buildId", out result.buildId);
					refreshedServerInfo.Rules.TryGetValue("modHash", out result.modHash);
					result.buildId = result.buildId ?? "UNKNOWN";
					result.modHash = result.modHash ?? "UNKNOWN";
				}
				int num2 = awaitingRequests - 1;
				awaitingRequests = num2;
				TryComplete();
			}
			void TryComplete()
			{
				if (awaitingRequests == 0)
				{
					successCallback?.Invoke(in result);
				}
			}
		}
	}

	private static bool RemoteGameInfoGetRuleBook(in RemoteGameInfo remoteGameInfo, RuleBook dest)
	{
		if (remoteGameInfo.userData is ServerList.Server { HasRules: not false } server)
		{
			dest.SetToDefaults();
			RuleBook.ReadBase64(new KeyValueUnsplitter("ruleBook").GetValue(server.Rules), dest);
			return true;
		}
		return false;
	}

	private static void RemoteGameInfoGetPlayers(in RemoteGameInfo remoteGameInfo, List<RemotePlayerInfo> output)
	{
		if (remoteGameInfo.userData is ServerList.Server { PlayerInfos: { } playerInfos })
		{
			for (int i = 0; i < playerInfos.Count; i++)
			{
				output.Add(new RemotePlayerInfo
				{
					name = playerInfos[i].name
				});
			}
		}
	}
}
