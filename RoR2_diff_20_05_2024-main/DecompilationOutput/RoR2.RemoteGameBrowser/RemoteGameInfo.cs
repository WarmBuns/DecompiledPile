using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using RoR2.Networking;

namespace RoR2.RemoteGameBrowser;

public struct RemoteGameInfo
{
	public delegate void GetPlayersDelegate(in RemoteGameInfo remoteGameInfo, [NotNull] List<RemotePlayerInfo> output);

	public delegate void RequestRefreshDelegate(in RemoteGameInfo remoteGameInfo, [CanBeNull] RequestRefreshSuccessCallback successCallback, [CanBeNull] Action failureCallback, bool fetchDetails);

	public delegate void RequestRefreshSuccessCallback(in RemoteGameInfo remoteGameInfo);

	public delegate bool GetRuleBookDelegate(in RemoteGameInfo remoteGameInfo, RuleBook dest);

	public struct ArtifactEnumerator : IEnumerator<ArtifactDef>, IEnumerator, IDisposable
	{
		public readonly string tagSource;

		private int readPos;

		public ArtifactDef Current => ArtifactCatalog.GetArtifactDef((ArtifactIndex)(tagSource[readPos] - 48));

		object IEnumerator.Current => Current;

		public ArtifactEnumerator(string tagSource)
		{
			this.tagSource = tagSource;
			readPos = 1;
		}

		public bool MoveNext()
		{
			readPos++;
			return readPos < tagSource.Length;
		}

		public void Reset()
		{
			readPos = 1;
		}

		public void Dispose()
		{
		}
	}

	public struct ArtifactEnumerable
	{
		public readonly string tagSource;

		public ArtifactEnumerable(string tagSource)
		{
			this.tagSource = tagSource;
		}

		public ArtifactEnumerator GetEnumerator()
		{
			ArtifactEnumerator result = new ArtifactEnumerator(tagSource);
			result.Reset();
			return result;
		}
	}

	public float retrievalTime;

	[CanBeNull]
	public string name;

	[CanBeNull]
	public string modHash;

	[CanBeNull]
	public string buildId;

	[CanBeNull]
	public string serverName;

	[CanBeNull]
	public string lobbyName;

	public string lobbyIdStr;

	public ulong? lobbyId;

	public string serverIdStr;

	public ulong? serverId;

	public AddressPortPair? serverAddress;

	public int? ping;

	public string currentSceneName;

	public SceneIndex? currentSceneIndex;

	public DifficultyIndex? currentDifficultyIndex;

	public int? lobbyPlayerCount;

	public int? lobbyMaxPlayers;

	public int? serverPlayerCount;

	public int? serverMaxPlayers;

	public bool? hasPassword;

	[CanBeNull]
	public string gameModeName;

	public bool? inGame;

	public bool? joinable;

	public bool? isBlacklisted;

	public bool? isFavorite;

	public bool? didRespond;

	[CanBeNull]
	public object userData;

	[CanBeNull]
	public GetPlayersDelegate getPlayersImplementation;

	[CanBeNull]
	public RequestRefreshDelegate requestRefreshImplementation;

	[CanBeNull]
	public GetRuleBookDelegate getRuleBookImplementation;

	public int availableSlots;

	public int availableLobbySlots;

	public int availableServerSlots;

	public int lesserPlayerCount;

	public int greaterPlayerCount;

	public int lesserMaxPlayers;

	public int greaterMaxPlayers;

	[CanBeNull]
	public string[] tags { get; private set; }

	public bool IsLobbyIdValid()
	{
		if (lobbyIdStr == null || !(lobbyIdStr != string.Empty))
		{
			return lobbyId.HasValue;
		}
		return true;
	}

	public bool IsServerIdValid()
	{
		if (serverIdStr == null || !(lobbyIdStr != string.Empty))
		{
			return serverId.HasValue;
		}
		return true;
	}

	public void GetPlayers(List<RemotePlayerInfo> output)
	{
		getPlayersImplementation?.Invoke(in this, output);
	}

	public bool GetRuleBook(RuleBook dest)
	{
		return getRuleBookImplementation?.Invoke(in this, dest) ?? false;
	}

	public void RequestRefresh([CanBeNull] RequestRefreshSuccessCallback successCallback, [CanBeNull] Action failureCallback, bool fetchDetails)
	{
		if (requestRefreshImplementation == null)
		{
			failureCallback?.Invoke();
		}
		else
		{
			requestRefreshImplementation(in this, successCallback, failureCallback, fetchDetails);
		}
	}

	public void SetTags(string[] newTags)
	{
		tags = newTags;
		if (tags == null)
		{
			return;
		}
		for (int i = 0; i < tags.Length; i++)
		{
			switch (tags[i])
			{
			case "dz":
				currentDifficultyIndex = DifficultyIndex.Easy;
				break;
			case "rs":
				currentDifficultyIndex = DifficultyIndex.Normal;
				break;
			case "mn":
				currentDifficultyIndex = DifficultyIndex.Hard;
				break;
			}
		}
	}

	public void CalcExtraFields()
	{
		lesserPlayerCount = int.MinValue;
		greaterPlayerCount = int.MaxValue;
		lesserMaxPlayers = int.MinValue;
		greaterMaxPlayers = int.MaxValue;
		Pick(lobbyPlayerCount, ref lesserPlayerCount, ref greaterPlayerCount);
		Pick(serverPlayerCount, ref lesserPlayerCount, ref greaterPlayerCount);
		Pick(lobbyMaxPlayers, ref lesserMaxPlayers, ref greaterMaxPlayers);
		Pick(serverMaxPlayers, ref lesserMaxPlayers, ref greaterMaxPlayers);
		if (lesserPlayerCount == int.MinValue)
		{
			lesserPlayerCount = 0;
		}
		if (greaterPlayerCount == int.MaxValue)
		{
			greaterPlayerCount = 0;
		}
		if (lesserMaxPlayers == int.MinValue)
		{
			lesserMaxPlayers = 0;
		}
		if (greaterMaxPlayers == int.MaxValue)
		{
			greaterMaxPlayers = 0;
		}
		availableSlots = int.MaxValue;
		int? num = lobbyMaxPlayers - lobbyPlayerCount;
		int? num2 = serverMaxPlayers - serverPlayerCount;
		if (num.HasValue)
		{
			availableSlots = Math.Min(availableSlots, num.Value);
		}
		if (num2.HasValue)
		{
			availableSlots = Math.Min(availableSlots, num2.Value);
		}
		availableLobbySlots = (lobbyMaxPlayers - lobbyPlayerCount).GetValueOrDefault();
		availableServerSlots = (serverMaxPlayers - serverPlayerCount).GetValueOrDefault();
		if (availableSlots == int.MaxValue)
		{
			availableSlots = 0;
		}
		static void Pick(int? inputValue, ref int lesserResult, ref int greaterResult)
		{
			if (inputValue.HasValue)
			{
				lesserResult = Math.Max(lesserResult, inputValue.Value);
				greaterResult = Math.Min(greaterResult, inputValue.Value);
			}
		}
	}

	public bool HasTag(string tag)
	{
		if (tags == null)
		{
			return false;
		}
		return Array.IndexOf(tags, tag) != -1;
	}

	public ArtifactEnumerable GetEnabledArtifacts()
	{
		ArtifactEnumerable result = new ArtifactEnumerable("");
		if (tags != null)
		{
			for (int i = 0; i < tags.Length; i++)
			{
				if (tags[i].StartsWith("a="))
				{
					result = new ArtifactEnumerable(tags[i]);
					break;
				}
			}
		}
		return result;
	}
}
