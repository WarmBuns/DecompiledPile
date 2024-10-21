using System;
using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;
using Facepunch.Steamworks;
using RoR2.ConVar;
using RoR2.ExpansionManagement;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class NetworkSession : NetworkBehaviour
{
	[Flags]
	public enum Flags
	{
		None = 0,
		HasPassword = 1,
		IsDedicatedServer = 2
	}

	[SyncVar(hook = "OnSyncSteamId")]
	private ulong serverSteamId;

	[SyncVar]
	public ulong lobbySteamId;

	[SyncVar]
	private uint _flags;

	[SyncVar]
	public string tagsString;

	[SyncVar]
	public uint maxPlayers;

	[SyncVar]
	public string serverName;

	private TagManager serverManager;

	private static readonly BoolConVar cvSteamLobbyAllowPersistence = new BoolConVar("steam_lobby_allow_persistence", ConVarFlags.None, "1", "Whether or not the application should attempt to reestablish an active game session's Steamworks lobby if it's been lost.");

	public static NetworkSession instance { get; private set; }

	public Flags flags
	{
		get
		{
			return (Flags)_flags;
		}
		set
		{
			Network_flags = (uint)value;
		}
	}

	public ulong NetworkserverSteamId
	{
		get
		{
			return serverSteamId;
		}
		[param: In]
		set
		{
			if (NetworkServer.localClientActive && !base.syncVarHookGuard)
			{
				base.syncVarHookGuard = true;
				OnSyncSteamId(value);
				base.syncVarHookGuard = false;
			}
			SetSyncVar(value, ref serverSteamId, 1u);
		}
	}

	public ulong NetworklobbySteamId
	{
		get
		{
			return lobbySteamId;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref lobbySteamId, 2u);
		}
	}

	public uint Network_flags
	{
		get
		{
			return _flags;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref _flags, 4u);
		}
	}

	public string NetworktagsString
	{
		get
		{
			return tagsString;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref tagsString, 8u);
		}
	}

	public uint NetworkmaxPlayers
	{
		get
		{
			return maxPlayers;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref maxPlayers, 16u);
		}
	}

	public string NetworkserverName
	{
		get
		{
			return serverName;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref serverName, 32u);
		}
	}

	private void SetFlag(Flags flag, bool flagEnabled)
	{
		if (flagEnabled)
		{
			flags |= flag;
		}
		else
		{
			flags &= ~flag;
		}
	}

	public bool HasFlag(Flags flag)
	{
		return (flags & flag) == flag;
	}

	public override void OnStartServer()
	{
		base.OnStartServer();
		SetFlag(Flags.IsDedicatedServer, flagEnabled: false);
		NetworkManagerSystem.SvPasswordConVar.instance.onValueChanged += UpdatePasswordFlag;
		UpdatePasswordFlag(NetworkManagerSystem.SvPasswordConVar.instance.value);
		RegisterTags();
		NetworkmaxPlayers = (uint)NetworkManagerSystem.SvMaxPlayersConVar.instance.intValue;
		NetworkManagerSystem.SvHostNameConVar.instance.onValueChanged += UpdateServerName;
		UpdateServerName(NetworkManagerSystem.SvHostNameConVar.instance.GetString());
	}

	private void RegisterTags()
	{
		if (PlatformSystems.ShouldUseEpicOnlineSystems)
		{
			serverManager = ServerManagerBase<EOSServerManager>.instance;
		}
		else
		{
			serverManager = ServerManagerBase<SteamworksServerManager>.instance;
			NetworkserverSteamId = NetworkManagerSystem.singleton.serverP2PId.ID;
		}
		if (serverManager != null)
		{
			TagManager tagManager = serverManager;
			tagManager.onTagsStringUpdated = (Action<string>)Delegate.Combine(tagManager.onTagsStringUpdated, new Action<string>(UpdateTagsString));
			UpdateTagsString(serverManager.tagsString ?? string.Empty);
		}
	}

	private void OnDestroy()
	{
		NetworkManagerSystem.SvHostNameConVar.instance.onValueChanged -= UpdateServerName;
		NetworkManagerSystem.SvPasswordConVar.instance.onValueChanged -= UpdatePasswordFlag;
		UnregisterTags();
	}

	private void UnregisterTags()
	{
		if (serverManager != null)
		{
			TagManager tagManager = serverManager;
			tagManager.onTagsStringUpdated = (Action<string>)Delegate.Remove(tagManager.onTagsStringUpdated, new Action<string>(UpdateTagsString));
		}
	}

	private void UpdateTagsString(string tagsString)
	{
		NetworktagsString = tagsString;
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		SteamworksAdvertiseGame();
	}

	private void UpdatePasswordFlag(string password)
	{
		SetFlag(Flags.HasPassword, !string.IsNullOrEmpty(password));
	}

	private void OnSyncSteamId(ulong newValue)
	{
		NetworkserverSteamId = newValue;
		SteamworksAdvertiseGame();
	}

	private void SteamworksAdvertiseGame()
	{
		if (RoR2Application.instance.steamworksClient != null)
		{
			ulong num = serverSteamId;
			uint num2 = 0u;
			ushort num3 = 0;
			CallMethod(GetField(GetField(Client.Instance, "native"), "user"), "AdvertiseGame", new object[3] { num, num2, num3 });
		}
		static void CallMethod(object obj, string methodName, object[] args)
		{
			obj.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(obj, args);
		}
		static object GetField(object obj, string fieldName)
		{
			return obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(obj);
		}
	}

	private void OnEnable()
	{
		instance = SingletonHelper.Assign(instance, this);
	}

	private void OnDisable()
	{
		instance = SingletonHelper.Unassign(instance, this);
	}

	private void Start()
	{
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
		if (NetworkServer.active)
		{
			NetworkServer.Spawn(base.gameObject);
		}
		StartCoroutine(SteamworksLobbyPersistenceCoroutine());
	}

	private void UpdateServerName(string newHostName)
	{
		NetworkserverName = newHostName;
	}

	[Server]
	public Run BeginRun(Run runPrefabComponent, RuleBook ruleBook, ulong seed)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'RoR2.Run RoR2.NetworkSession::BeginRun(RoR2.Run,RoR2.RuleBook,System.UInt64)' called on client");
			return null;
		}
		if (!Run.instance)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(runPrefabComponent.gameObject);
			Run component = gameObject.GetComponent<Run>();
			component.SetRuleBook(ruleBook);
			component.seed = seed;
			NetworkServer.Spawn(gameObject);
			{
				foreach (ExpansionDef expansionDef in ExpansionCatalog.expansionDefs)
				{
					if (component.IsExpansionEnabled(expansionDef) && (bool)expansionDef.runBehaviorPrefab)
					{
						NetworkServer.Spawn(UnityEngine.Object.Instantiate(expansionDef.runBehaviorPrefab, gameObject.transform));
					}
				}
				return component;
			}
		}
		return null;
	}

	[Server]
	public void EndRun()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.NetworkSession::EndRun()' called on client");
		}
		else if ((bool)Run.instance)
		{
			UnityEngine.Object.Destroy(Run.instance.gameObject);
		}
	}

	private IEnumerator SteamworksLobbyPersistenceCoroutine()
	{
		while (true)
		{
			UpdateSteamworksLobbyPersistence();
			yield return new WaitForSecondsRealtime(4f);
		}
	}

	private void UpdateSteamworksLobbyPersistence()
	{
		if (Client.Instance == null || !cvSteamLobbyAllowPersistence.value || NetworkServer.dontListen || PlatformSystems.lobbyManager.awaitingJoin || PlatformSystems.lobbyManager.awaitingCreate)
		{
			return;
		}
		ulong currentLobby = Client.Instance.Lobby.CurrentLobby;
		if (NetworkServer.active)
		{
			if (PlatformSystems.lobbyManager.isInLobby)
			{
				NetworklobbySteamId = currentLobby;
			}
			else
			{
				PlatformSystems.lobbyManager.CreateLobby();
			}
		}
		else if (lobbySteamId != 0L && lobbySteamId != currentLobby)
		{
			PlatformSystems.lobbyManager.JoinLobby(new PlatformID(lobbySteamId));
		}
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.WritePackedUInt64(serverSteamId);
			writer.WritePackedUInt64(lobbySteamId);
			writer.WritePackedUInt32(_flags);
			writer.Write(tagsString);
			writer.WritePackedUInt32(maxPlayers);
			writer.Write(serverName);
			return true;
		}
		bool flag = false;
		if ((base.syncVarDirtyBits & (true ? 1u : 0u)) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.WritePackedUInt64(serverSteamId);
		}
		if ((base.syncVarDirtyBits & 2u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.WritePackedUInt64(lobbySteamId);
		}
		if ((base.syncVarDirtyBits & 4u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.WritePackedUInt32(_flags);
		}
		if ((base.syncVarDirtyBits & 8u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(tagsString);
		}
		if ((base.syncVarDirtyBits & 0x10u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.WritePackedUInt32(maxPlayers);
		}
		if ((base.syncVarDirtyBits & 0x20u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(serverName);
		}
		if (!flag)
		{
			writer.WritePackedUInt32(base.syncVarDirtyBits);
		}
		return flag;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
		if (initialState)
		{
			serverSteamId = reader.ReadPackedUInt64();
			lobbySteamId = reader.ReadPackedUInt64();
			_flags = reader.ReadPackedUInt32();
			tagsString = reader.ReadString();
			maxPlayers = reader.ReadPackedUInt32();
			serverName = reader.ReadString();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			OnSyncSteamId(reader.ReadPackedUInt64());
		}
		if (((uint)num & 2u) != 0)
		{
			lobbySteamId = reader.ReadPackedUInt64();
		}
		if (((uint)num & 4u) != 0)
		{
			_flags = reader.ReadPackedUInt32();
		}
		if (((uint)num & 8u) != 0)
		{
			tagsString = reader.ReadString();
		}
		if (((uint)num & 0x10u) != 0)
		{
			maxPlayers = reader.ReadPackedUInt32();
		}
		if (((uint)num & 0x20u) != 0)
		{
			serverName = reader.ReadString();
		}
	}

	public override void PreStartClient()
	{
	}
}
