using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using HG;
using RoR2.ContentManagement;
using RoR2.ConVar;
using RoR2.Orbs;
using RoR2.UI;
using Unity;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace RoR2.Networking;

public abstract class NetworkManagerSystem : NetworkManager
{
	public enum CanPlayOnlineState
	{
		Pending,
		Yes,
		No
	}

	private class NetLogLevelConVar : BaseConVar
	{
		private static NetLogLevelConVar cvNetLogLevel = new NetLogLevelConVar("net_loglevel", ConVarFlags.Engine, null, "Network log verbosity.");

		public NetLogLevelConVar(string name, ConVarFlags flags, string defaultValue, string helpText)
			: base(name, flags, defaultValue, helpText)
		{
		}

		public override void SetString(string newValue)
		{
			if (TextSerialization.TryParseInvariant(newValue, out int result))
			{
				LogFilter.currentLogLevel = result;
			}
		}

		public override string GetString()
		{
			return TextSerialization.ToStringInvariant(LogFilter.currentLogLevel);
		}
	}

	private class SvListenConVar : BaseConVar
	{
		private static SvListenConVar cvSvListen = new SvListenConVar("sv_listen", ConVarFlags.Engine, null, "Whether or not the server will accept connections from other players.");

		public SvListenConVar(string name, ConVarFlags flags, string defaultValue, string helpText)
			: base(name, flags, defaultValue, helpText)
		{
		}

		public override void SetString(string newValue)
		{
			if (!NetworkServer.active && TextSerialization.TryParseInvariant(newValue, out int result))
			{
				NetworkServer.dontListen = result == 0;
			}
		}

		public override string GetString()
		{
			if (!NetworkServer.dontListen)
			{
				return "1";
			}
			return "0";
		}
	}

	public class SvMaxPlayersConVar : BaseConVar
	{
		public static readonly SvMaxPlayersConVar instance = new SvMaxPlayersConVar("sv_maxplayers", ConVarFlags.Engine, null, "Maximum number of players allowed.");

		public int intValue => NetworkManager.singleton.maxConnections;

		public SvMaxPlayersConVar(string name, ConVarFlags flags, string defaultValue, string helpText)
			: base(name, flags, defaultValue, helpText)
		{
		}

		public override void SetString(string newValue)
		{
			if (NetworkServer.active)
			{
				throw new ConCommandException("Cannot change this convar while the server is running.");
			}
			if ((bool)NetworkManager.singleton && TextSerialization.TryParseInvariant(newValue, out int result))
			{
				NetworkManager.singleton.maxConnections = Math.Min(Math.Max(result, 1), RoR2Application.hardMaxPlayers);
			}
		}

		public override string GetString()
		{
			if (!NetworkManager.singleton)
			{
				return "1";
			}
			return TextSerialization.ToStringInvariant(NetworkManager.singleton.maxConnections);
		}
	}

	private class KickMessage : MessageBase
	{
		public BaseKickReason kickReason;

		public KickMessage()
		{
		}

		public KickMessage(BaseKickReason kickReason)
		{
			this.kickReason = kickReason;
		}

		public override void Serialize(NetworkWriter writer)
		{
			base.Serialize(writer);
			string text = kickReason?.GetType().FullName ?? string.Empty;
			writer.Write(text);
			if (!(text == string.Empty))
			{
				kickReason.Serialize(writer);
			}
		}

		public override void Deserialize(NetworkReader reader)
		{
			base.Deserialize(reader);
			string typeName = reader.ReadString();
			Type type = null;
			try
			{
				type = Type.GetType(typeName);
			}
			catch
			{
			}
			if (type == null || !typeof(BaseKickReason).IsAssignableFrom(type) || type.IsAbstract)
			{
				kickReason = null;
				return;
			}
			kickReason = (BaseKickReason)Activator.CreateInstance(type);
			kickReason.Deserialize(reader);
		}

		public bool TryGetDisplayTokenAndFormatParams(out string token, out object[] formatArgs)
		{
			if (kickReason == null)
			{
				token = null;
				formatArgs = null;
				return false;
			}
			kickReason.GetDisplayTokenAndFormatParams(out token, out formatArgs);
			return true;
		}
	}

	public abstract class BaseKickReason : MessageBase
	{
		public BaseKickReason()
		{
		}

		public abstract void GetDisplayTokenAndFormatParams(out string token, out object[] formatArgs);
	}

	public class SimpleLocalizedKickReason : BaseKickReason
	{
		public string baseToken;

		public string[] formatArgs;

		public SimpleLocalizedKickReason()
		{
		}

		public SimpleLocalizedKickReason(string baseToken, params string[] formatArgs)
		{
			this.baseToken = baseToken;
			this.formatArgs = formatArgs;
		}

		public override void GetDisplayTokenAndFormatParams(out string token, out object[] formatArgs)
		{
			token = baseToken;
			object[] array = this.formatArgs;
			formatArgs = array;
		}

		public override void Serialize(NetworkWriter writer)
		{
			writer.Write(baseToken);
			GeneratedNetworkCode._WriteArrayString_None(writer, formatArgs);
		}

		public override void Deserialize(NetworkReader reader)
		{
			baseToken = reader.ReadString();
			formatArgs = GeneratedNetworkCode._ReadArrayString_None(reader);
		}
	}

	public class ModMismatchKickReason : BaseKickReason
	{
		public string[] serverModList = Array.Empty<string>();

		public ModMismatchKickReason()
		{
		}

		public ModMismatchKickReason(IEnumerable<string> serverModList)
		{
			this.serverModList = serverModList.ToArray();
		}

		public override void GetDisplayTokenAndFormatParams(out string token, out object[] formatArgs)
		{
			IEnumerable<string> networkModList = NetworkModCompatibilityHelper.networkModList;
			IEnumerable<string> values = networkModList.Except(serverModList);
			IEnumerable<string> values2 = serverModList.Except(networkModList);
			token = "KICK_REASON_MOD_MISMATCH";
			object[] array = new string[2]
			{
				string.Join("\n", values),
				string.Join("\n", values2)
			};
			formatArgs = array;
		}

		public override void Serialize(NetworkWriter writer)
		{
			GeneratedNetworkCode._WriteArrayString_None(writer, serverModList);
		}

		public override void Deserialize(NetworkReader reader)
		{
			serverModList = GeneratedNetworkCode._ReadArrayString_None(reader);
		}
	}

	protected class AddPlayerMessage : MessageBase
	{
		public PlatformID id;

		public byte[] steamAuthTicketData;

		public override void Serialize(NetworkWriter writer)
		{
			GeneratedNetworkCode._WritePlatformID_None(writer, id);
			writer.WriteBytesFull(steamAuthTicketData);
		}

		public override void Deserialize(NetworkReader reader)
		{
			id = GeneratedNetworkCode._ReadPlatformID_None(reader);
			steamAuthTicketData = reader.ReadBytesAndSize();
		}
	}

	public class SvHostNameConVar : BaseConVar
	{
		public static readonly SvHostNameConVar instance = new SvHostNameConVar("sv_hostname", ConVarFlags.None, "", "The public name to use for the server if hosting.");

		private string value = "NAME";

		public event Action<string> onValueChanged;

		public SvHostNameConVar(string name, ConVarFlags flags, string defaultValue, string helpText)
			: base(name, flags, defaultValue, helpText)
		{
		}

		public override void SetString(string newValue)
		{
			value = newValue;
			this.onValueChanged?.Invoke(newValue);
		}

		public override string GetString()
		{
			return value;
		}
	}

	public class SvPortConVar : BaseConVar
	{
		public static readonly SvPortConVar instance = new SvPortConVar("sv_port", ConVarFlags.Engine, null, "The port to use for the server if hosting.");

		public ushort value
		{
			get
			{
				if (!singleton)
				{
					return 0;
				}
				return (ushort)singleton.networkPort;
			}
		}

		public SvPortConVar(string name, ConVarFlags flags, string defaultValue, string helpText)
			: base(name, flags, defaultValue, helpText)
		{
		}

		public override void SetString(string newValueString)
		{
			if (NetworkServer.active)
			{
				throw new ConCommandException("Cannot change this convar while the server is running.");
			}
			if (TextSerialization.TryParseInvariant(newValueString, out ushort result))
			{
				singleton.networkPort = result;
			}
		}

		public override string GetString()
		{
			return value.ToString();
		}
	}

	public class SvIPConVar : BaseConVar
	{
		public static readonly SvIPConVar instance = new SvIPConVar("sv_ip", ConVarFlags.Engine, null, "The IP for the server to bind to if hosting.");

		public SvIPConVar(string name, ConVarFlags flags, string defaultValue, string helpText)
			: base(name, flags, defaultValue, helpText)
		{
		}

		public override void SetString(string newValueString)
		{
			if (NetworkServer.active)
			{
				throw new ConCommandException("Cannot change this convar while the server is running.");
			}
			singleton.serverBindAddress = newValueString;
		}

		public override string GetString()
		{
			if (!singleton)
			{
				return string.Empty;
			}
			return singleton.serverBindAddress;
		}
	}

	public class SvPasswordConVar : BaseConVar
	{
		public static readonly SvPasswordConVar instance = new SvPasswordConVar("sv_password", ConVarFlags.None, "", "The password to use for the server if hosting.");

		public string value { get; private set; }

		public event Action<string> onValueChanged;

		public SvPasswordConVar(string name, ConVarFlags flags, string defaultValue, string helpText)
			: base(name, flags, defaultValue, helpText)
		{
		}

		public override void SetString(string newValue)
		{
			if (newValue == null)
			{
				newValue = "";
			}
			if (!(value == newValue))
			{
				value = newValue;
				this.onValueChanged?.Invoke(value);
			}
		}

		public override string GetString()
		{
			return value;
		}
	}

	protected class AddressablesChangeSceneAsyncOperation : IChangeSceneAsyncOperation
	{
		private static AsyncOperationHandle<SceneInstance>? previousLoadOperation;

		private AsyncOperationHandle<SceneInstance> srcOperation;

		private bool srcOperationIsDone;

		private SceneInstance sceneInstance;

		private bool isActivated;

		private bool _allowSceneActivation;

		public bool isDone { get; private set; }

		public bool allowSceneActivation
		{
			get
			{
				return _allowSceneActivation;
			}
			set
			{
				if (_allowSceneActivation != value)
				{
					_allowSceneActivation = value;
					ActivateIfReady();
				}
			}
		}

		public AddressablesChangeSceneAsyncOperation(object key, LoadSceneMode loadMode, bool activateOnLoad)
		{
			srcOperation = Addressables.LoadSceneAsync(key, loadMode, activateOnLoad);
			previousLoadOperation = srcOperation;
			srcOperation.Completed += delegate(AsyncOperationHandle<SceneInstance> v)
			{
				srcOperationIsDone = true;
				sceneInstance = v.Result;
				ActivateIfReady();
			};
			allowSceneActivation = activateOnLoad;
		}

		private void ActivateIfReady()
		{
			if (srcOperationIsDone && !isActivated)
			{
				sceneInstance.ActivateAsync().completed += delegate
				{
					isDone = true;
				};
			}
		}
	}

	protected static readonly FieldInfo loadingSceneAsyncFieldInfo;

	protected float _unpredictedServerFixedTime;

	protected float _unpredictedServerFixedTimeSmoothed;

	protected float unpredictedServerFixedTimeVelocity;

	protected float _unpredictedServerFrameTime;

	protected float _unpredictedServerFrameTimeSmoothed;

	protected float unpredictedServerFrameTimeVelocity;

	protected static FloatConVar cvNetTimeSmoothRate;

	public float debugServerTime;

	public float debugRTT;

	protected bool isSinglePlayer;

	protected bool actedUponDesiredHost;

	protected float lastDesiredHostSetTime = float.NegativeInfinity;

	protected HostDescription _desiredHost;

	private static bool wasFading;

	protected static readonly string[] sceneWhiteList;

	public static readonly StringConVar cvClPassword;

	protected GameObject serverNetworkSessionInstance;

	protected GameObject serverPauseStopControllerInstance;

	private static NetworkReader DefaultNetworkReader;

	private static readonly FloatConVar svTimeTransmitInterval;

	private float timeTransmitTimer;

	protected bool serverShuttingDown;

	private static readonly Queue<NetworkConnection> clientsReadyDuringLevelTransition;

	public static readonly StringConVar cvSvCustomTags;

	protected static bool isLoadingScene
	{
		get
		{
			IChangeSceneAsyncOperation changeSceneAsyncOperation = (IChangeSceneAsyncOperation)loadingSceneAsyncFieldInfo.GetValue(null);
			if (changeSceneAsyncOperation != null)
			{
				return !changeSceneAsyncOperation.isDone;
			}
			return false;
		}
	}

	public new static NetworkManagerSystem singleton => (NetworkManagerSystem)NetworkManager.singleton;

	public float unpredictedServerFixedTime => _unpredictedServerFixedTime;

	public float unpredictedServerFixedTimeSmoothed => _unpredictedServerFixedTimeSmoothed;

	public float serverFixedTime => unpredictedServerFixedTimeSmoothed + filteredClientRttFixed;

	public float unpredictedServerFrameTime => _unpredictedServerFrameTime;

	public float unpredictedServerFrameTimeSmoothed => _unpredictedServerFrameTimeSmoothed;

	public float serverFrameTime => unpredictedServerFrameTimeSmoothed + filteredClientRttFrame;

	public HostDescription desiredHost
	{
		get
		{
			return _desiredHost;
		}
		set
		{
			if (!_desiredHost.Equals(value))
			{
				_desiredHost = value;
				actedUponDesiredHost = false;
				lastDesiredHostSetTime = Time.unscaledTime;
				Debug.LogFormat("NetworkManagerSystem.desiredHost={0}", _desiredHost.ToString());
			}
		}
	}

	public bool clientHasConfirmedQuit { get; private set; }

	protected bool clientIsConnecting
	{
		get
		{
			if (client?.connection != null)
			{
				return !client.isConnected;
			}
			return false;
		}
	}

	public float clientRttFixed { get; private set; }

	public float clientRttFrame { get; private set; }

	public float filteredClientRttFixed { get; private set; }

	public float filteredClientRttFrame { get; private set; }

	public bool isHost { get; protected set; }

	public PlatformID clientP2PId { get; protected set; } = PlatformID.nil;

	public PlatformID serverP2PId { get; protected set; } = PlatformID.nil;

	public static event Action onStartGlobal;

	public static event Action<NetworkClient> onStartClientGlobal;

	public static event Action onStopClientGlobal;

	public static event Action<NetworkConnection> onClientConnectGlobal;

	public static event Action<NetworkConnection> onClientDisconnectGlobal;

	public static event Action onStartHostGlobal;

	public static event Action onStopHostGlobal;

	public static event Action onStartServerGlobal;

	public static event Action onStopServerGlobal;

	public static event Action<NetworkConnection> onServerConnectGlobal;

	public static event Action<NetworkConnection> onServerDisconnectGlobal;

	public static event Action<string> onServerSceneChangedGlobal;

	static NetworkManagerSystem()
	{
		cvNetTimeSmoothRate = new FloatConVar("net_time_smooth_rate", ConVarFlags.None, "1.05", "The smoothing rate for the network time.");
		wasFading = false;
		sceneWhiteList = new string[3] { "title", "crystalworld", "logbook" };
		cvClPassword = new StringConVar("cl_password", ConVarFlags.None, "", "The password to use when joining a passworded server.");
		DefaultNetworkReader = new NetworkReader();
		svTimeTransmitInterval = new FloatConVar("sv_time_transmit_interval", ConVarFlags.Cheat, (1f / 60f).ToString(), "How long it takes for the server to issue a time update to clients.");
		clientsReadyDuringLevelTransition = new Queue<NetworkConnection>();
		cvSvCustomTags = new StringConVar("sv_custom_tags", ConVarFlags.None, "", "Comma-delimited custom tags to report to the server browser.");
		loadingSceneAsyncFieldInfo = typeof(NetworkManager).GetField("s_LoadingSceneAsync", BindingFlags.Static | BindingFlags.NonPublic);
		if (loadingSceneAsyncFieldInfo == null)
		{
			Debug.LogError("NetworkManager.s_LoadingSceneAsync field could not be found! Make sure to provide a proper implementation for this version of Unity.");
		}
	}

	public virtual void Init(NetworkManagerConfiguration configurationComponent)
	{
		base.dontDestroyOnLoad = configurationComponent.DontDestroyOnLoad;
		base.runInBackground = configurationComponent.RunInBackground;
		base.logLevel = configurationComponent.LogLevel;
		base.offlineScene = configurationComponent.OfflineScene;
		base.onlineScene = configurationComponent.OnlineScene;
		base.playerPrefab = configurationComponent.PlayerPrefab;
		base.autoCreatePlayer = configurationComponent.AutoCreatePlayer;
		base.playerSpawnMethod = configurationComponent.PlayerSpawnMethod;
		base.spawnPrefabs.Clear();
		base.spawnPrefabs.AddRange(configurationComponent.SpawnPrefabs);
		base.customConfig = configurationComponent.CustomConfig;
		base.maxConnections = configurationComponent.MaxConnections;
		base.channels.Clear();
		base.channels.AddRange(configurationComponent.QosChannels);
		Run.onRunStartGlobal += ResetRunTime;
	}

	private void ResetRunTime(Run run)
	{
		if (Run.instance != null)
		{
			Run.instance.NetworkfixedTime = 0f;
			Run.instance.time = 0f;
			Run.FixedTimeStamp.Update();
		}
	}

	protected void InitializeTime()
	{
		_unpredictedServerFixedTime = 0f;
		_unpredictedServerFixedTimeSmoothed = 0f;
		unpredictedServerFixedTimeVelocity = 1f;
		_unpredictedServerFrameTime = 0f;
		_unpredictedServerFrameTimeSmoothed = 0f;
		unpredictedServerFrameTimeVelocity = 1f;
	}

	protected void UpdateTime(ref float targetValue, ref float currentValue, ref float velocity, float deltaTime)
	{
		if (!(deltaTime <= 0f))
		{
			targetValue += deltaTime;
			float num = (targetValue - currentValue) / deltaTime;
			float num2 = 1f;
			if (velocity == 0f || Mathf.Abs(num) > num2 * 3f)
			{
				currentValue = targetValue;
				velocity = num2;
			}
			else
			{
				currentValue += velocity * deltaTime;
				velocity = Mathf.MoveTowards(velocity, num, cvNetTimeSmoothRate.value * deltaTime);
			}
		}
	}

	protected static NetworkUser[] GetConnectionNetworkUsers(NetworkConnection conn)
	{
		List<PlayerController> playerControllers = conn.playerControllers;
		NetworkUser[] array = new NetworkUser[playerControllers.Count];
		for (int i = 0; i < playerControllers.Count; i++)
		{
			array[i] = playerControllers[i].gameObject.GetComponent<NetworkUser>();
		}
		return array;
	}

	protected abstract void Start();

	protected void OnDestroy()
	{
		typeof(NetworkManager).GetMethod("OnDestroy", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(this, null);
	}

	public IEnumerator FireOnStartGlobalEvent()
	{
		while (Console.instance == null)
		{
			yield return null;
		}
		NetworkManagerSystem.onStartGlobal?.Invoke();
	}

	protected void FixedUpdate()
	{
		UpdateTime(ref _unpredictedServerFixedTime, ref _unpredictedServerFixedTimeSmoothed, ref unpredictedServerFixedTimeVelocity, Time.fixedDeltaTime);
		FixedUpdateServer();
		FixedUpdateClient();
		debugServerTime = unpredictedServerFixedTime;
		debugRTT = clientRttFrame;
	}

	protected abstract void Update();

	protected abstract void EnsureDesiredHost();

	public abstract void ForceCloseAllConnections();

	public bool IsSinglePlayer()
	{
		return isSinglePlayer;
	}

	public void SetSinglePlayerNetworkState()
	{
		NetworkServer.dontListen = true;
		desiredHost = default(HostDescription);
		isSinglePlayer = true;
	}

	public void ResetDesiredHost()
	{
		actedUponDesiredHost = false;
		singleton.desiredHost = default(HostDescription);
		isSinglePlayer = false;
	}

	public abstract void CreateLocalLobby();

	public virtual IEnumerator<CanPlayOnlineState> CanPlayOnline()
	{
		yield return CanPlayOnlineState.Yes;
	}

	public override void OnStartClient(NetworkClient newClient)
	{
		base.OnStartClient(newClient);
		InitializeTime();
		RegisterPrefabs(ContentManager.bodyPrefabs);
		RegisterPrefabs(ContentManager.masterPrefabs);
		RegisterPrefabs(ContentManager.projectilePrefabs);
		RegisterPrefabs(ContentManager.networkedObjectPrefabs);
		RegisterPrefabs(ContentManager.gameModePrefabs);
		ClientScene.RegisterPrefab(LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkSession"));
		ClientScene.RegisterPrefab(LegacyResourcesAPI.Load<GameObject>("Prefabs/Stage"));
		NetworkMessageHandlerAttribute.RegisterClientMessages(newClient);
		NetworkManagerSystem.onStartClientGlobal?.Invoke(newClient);
		static void RegisterPrefabs(GameObject[] prefabs)
		{
			for (int i = 0; i < prefabs.Length; i++)
			{
				ClientScene.RegisterPrefab(prefabs[i]);
			}
		}
	}

	public override void OnStopClient()
	{
		try
		{
			NetworkManagerSystem.onStopClientGlobal?.Invoke();
		}
		catch (Exception message)
		{
			Debug.LogError(message);
		}
		foreach (NetworkClient allClient in NetworkClient.allClients)
		{
			allClient?.connection?.Disconnect();
		}
		ForceCloseAllConnections();
		if (actedUponDesiredHost)
		{
			singleton.desiredHost = HostDescription.none;
		}
		base.OnStopClient();
		OrbStorageUtility.Clear();
	}

	public override void OnClientConnect(NetworkConnection conn)
	{
		base.OnClientConnect(conn);
		clientRttFrame = 0f;
		filteredClientRttFixed = 0f;
		ClientSendAuth(conn);
		ClientSetPlayers(conn);
		NetworkManagerSystem.onClientConnectGlobal?.Invoke(conn);
	}

	public override void OnClientDisconnect(NetworkConnection conn)
	{
		base.OnClientDisconnect(conn);
		NetworkManagerSystem.onClientDisconnectGlobal?.Invoke(conn);
	}

	public void ClientAddPlayer(short playerControllerId, NetworkConnection connection = null)
	{
		foreach (PlayerController localPlayer in ClientScene.localPlayers)
		{
			if (localPlayer.playerControllerId == playerControllerId && localPlayer.IsValid && (bool)localPlayer.gameObject)
			{
				Debug.LogFormat("Player {0} already added, aborting.", playerControllerId);
				return;
			}
		}
		Debug.LogFormat("Adding local player controller {0} on connection {1}", playerControllerId, connection);
		AddPlayerMessage extraMessage = CreateClientAddPlayerMessage();
		ClientScene.AddPlayer(connection, playerControllerId, extraMessage);
	}

	protected abstract AddPlayerMessage CreateClientAddPlayerMessage();

	protected void UpdateClient()
	{
		UpdateCheckInactiveConnections();
		if (client?.connection != null)
		{
			filteredClientRttFrame = RttManager.GetConnectionFrameSmoothedRtt(client.connection);
			clientRttFrame = RttManager.GetConnectionRTT(client.connection);
		}
		bool flag = string.IsNullOrEmpty(NetworkManager.networkSceneName);
		bool flag2 = (client != null && !ClientScene.ready && !flag) || isLoadingScene;
		if (wasFading != flag2)
		{
			if (flag2)
			{
				FadeToBlackManager.fadeCount++;
			}
			else
			{
				FadeToBlackManager.fadeCount--;
			}
			wasFading = flag2;
		}
	}

	protected abstract void UpdateCheckInactiveConnections();

	protected abstract void StartClient(PlatformID serverID);

	public abstract bool IsConnectedToServer(PlatformID serverID);

	private void FixedUpdateClient()
	{
		if (NetworkClient.active && client != null && client?.connection != null && client.connection.isConnected)
		{
			NetworkConnection connection = client.connection;
			filteredClientRttFixed = RttManager.GetConnectionFixedSmoothedRtt(connection);
			clientRttFixed = RttManager.GetConnectionRTT(connection);
			if (!Util.ConnectionIsLocal(connection))
			{
				RttManager.Ping(connection, QosChannelIndex.ping.intVal);
			}
		}
	}

	public override void OnClientSceneChanged(NetworkConnection conn)
	{
		string text = NetworkManager.networkSceneName;
		List<string> list = new List<string>();
		bool flag = false;
		for (int i = 0; i < SceneManager.sceneCount; i++)
		{
			string text2 = SceneManager.GetSceneAt(i).name;
			list.Add(text2);
			if (text2 == text)
			{
				flag = true;
			}
		}
		if (flag)
		{
			base.autoCreatePlayer = false;
			base.OnClientSceneChanged(conn);
			ClientSetPlayers(conn);
			FadeToBlackManager.ForceFullBlack();
		}
	}

	private void ClientSendAuth(NetworkConnection conn)
	{
		ClientAuthData data = new ClientAuthData();
		PlatformAuth(ref data, conn);
		data.password = cvClPassword.value;
		data.version = RoR2Application.GetBuildId();
		data.modHash = NetworkModCompatibilityHelper.networkModHash;
		conn.Send(74, data);
	}

	protected abstract void PlatformAuth(ref ClientAuthData data, NetworkConnection conn);

	protected void ClientSetPlayers(NetworkConnection conn)
	{
		ReadOnlyCollection<LocalUser> readOnlyLocalUsersList = LocalUserManager.readOnlyLocalUsersList;
		for (int i = 0; i < readOnlyLocalUsersList.Count; i++)
		{
			if (i == 0 || RoR2Application.isInLocalMultiPlayer)
			{
				Debug.LogError($"For iteration {i}, out of {readOnlyLocalUsersList.Count}");
				ClientAddPlayer((short)readOnlyLocalUsersList[i].id, conn);
			}
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void ClientInit()
	{
		SceneCatalog.onMostRecentSceneDefChanged += ClientUpdateOfflineScene;
	}

	private static void ClientUpdateOfflineScene(SceneDef sceneDef)
	{
		if ((bool)singleton && sceneDef.isOfflineScene)
		{
			singleton.offlineScene = sceneDef.cachedName;
		}
	}

	protected static void EnsureNetworkManagerNotBusy()
	{
		if (!singleton || (!singleton.serverShuttingDown && !isLoadingScene))
		{
			return;
		}
		throw new ConCommandException("NetworkManager is busy and cannot receive commands.");
	}

	[ConCommand(commandName = "client_set_players", flags = ConVarFlags.None, helpText = "Adds network players for all local players. Debug only.")]
	private static void CCClientSetPlayers(ConCommandArgs args)
	{
		if ((bool)singleton)
		{
			singleton.PlatformClientSetPlayers(args);
		}
	}

	protected abstract void PlatformClientSetPlayers(ConCommandArgs args);

	[ConCommand(commandName = "ping", flags = ConVarFlags.None, helpText = "Prints the current round trip time from this client to the server and back.")]
	private static void CCPing(ConCommandArgs args)
	{
		if ((bool)singleton)
		{
			NetworkConnection networkConnection = singleton?.client?.connection;
			if (networkConnection != null)
			{
				Debug.LogFormat("rtt={0}ms smoothedFrame={1} smoothedFixed={2}", RttManager.GetConnectionRTTInMilliseconds(networkConnection), RttManager.GetConnectionFrameSmoothedRtt(networkConnection), RttManager.GetConnectionFixedSmoothedRtt(networkConnection));
			}
		}
	}

	[ConCommand(commandName = "set_scene", flags = ConVarFlags.None, helpText = "Changes to the named scene.")]
	private static void CCSetScene(ConCommandArgs args)
	{
		string argString = args.GetArgString(0);
		if (!singleton)
		{
			throw new ConCommandException("set_scene failed: NetworkManagerSystem is not available.");
		}
		SceneCatalog.GetSceneDefForCurrentScene();
		SceneDef sceneDefFromSceneName = SceneCatalog.GetSceneDefFromSceneName(argString);
		if (!sceneDefFromSceneName)
		{
			throw new ConCommandException("\"" + argString + "\" is not a valid scene.");
		}
		bool boolValue = Console.CheatsConVar.instance.boolValue;
		if (!NetworkManager.singleton)
		{
			_ = 1;
		}
		else
			_ = NetworkManager.singleton.isNetworkActive;
		if (NetworkManager.singleton.isNetworkActive)
		{
			if (sceneDefFromSceneName.isOfflineScene)
			{
				throw new ConCommandException("Cannot switch to scene \"" + argString + "\": Cannot switch to offline-only scene while in a network session.");
			}
			if (!boolValue)
			{
				throw new ConCommandException("Cannot switch to scene \"" + argString + "\": Cheats must be enabled to switch between online-only scenes.");
			}
		}
		else if (!sceneDefFromSceneName.isOfflineScene)
		{
			throw new ConCommandException("Cannot switch to scene \"" + argString + "\": Cannot switch to online-only scene while not in a network session.");
		}
		if ((bool)PauseStopController.instance && PauseStopController.instance.isPaused)
		{
			PauseStopController.instance.Pause(shouldPause: false);
		}
		if (NetworkServer.active)
		{
			Debug.LogFormat("Setting server scene to {0}", argString);
			singleton.ServerChangeScene(argString);
			return;
		}
		if (!NetworkClient.active)
		{
			Debug.LogFormat("Setting offline scene to {0}", argString);
			singleton.ServerChangeScene(argString);
			return;
		}
		throw new ConCommandException("Cannot change scene while connected to a remote server.");
	}

	[ConCommand(commandName = "scene_list", flags = ConVarFlags.None, helpText = "Prints a list of all available scene names.")]
	private static void CCSceneList(ConCommandArgs args)
	{
		if ((bool)singleton)
		{
			string[] array = new string[SceneManager.sceneCountInBuildSettings];
			for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
			{
				array[i] = $"[{i}]={SceneUtility.GetScenePathByBuildIndex(i)}";
			}
		}
	}

	[ConCommand(commandName = "dump_network_ids", flags = ConVarFlags.None, helpText = "Lists the network ids of all currently networked game objects.")]
	private static void CCDumpNetworkIDs(ConCommandArgs args)
	{
		if ((bool)singleton)
		{
			List<NetworkIdentity> list = new List<NetworkIdentity>(UnityEngine.Object.FindObjectsOfType<NetworkIdentity>());
			list.Sort((NetworkIdentity lhs, NetworkIdentity rhs) => (int)(lhs.netId.Value - rhs.netId.Value));
			for (int i = 0; i < list.Count; i++)
			{
				Debug.LogFormat("{0}={1}", list[i].netId.Value, list[i].gameObject.name);
			}
		}
	}

	[ConCommand(commandName = "disconnect", flags = ConVarFlags.None, helpText = "Disconnect from a server or shut down the current server.")]
	private static void CCDisconnect(ConCommandArgs args)
	{
		if ((bool)singleton)
		{
			singleton.PlatformDisconnect(args);
		}
	}

	protected abstract void PlatformDisconnect(ConCommandArgs args);

	protected void Disconnect()
	{
		if (!serverShuttingDown && singleton.isNetworkActive)
		{
			if (NetworkServer.active)
			{
				singleton.RequestServerShutdown();
			}
			else
			{
				singleton.StopClient();
			}
			OrbStorageUtility.Clear();
		}
	}

	[ConCommand(commandName = "connect", flags = ConVarFlags.None, helpText = "Connect to a server.")]
	private static void CCConnect(ConCommandArgs args)
	{
		if ((bool)singleton)
		{
			singleton.PlatformConnect(args);
		}
	}

	protected abstract void PlatformConnect(ConCommandArgs args);

	private static void CCConnectP2P(ConCommandArgs args)
	{
		if ((bool)singleton)
		{
			singleton.PlatformConnect(args);
		}
	}

	protected abstract void PlatformConnectP2P(ConCommandArgs args);

	[ConCommand(commandName = "host", flags = ConVarFlags.None, helpText = "Host a server. First argument is whether or not to listen for incoming connections.")]
	private static void CCHost(ConCommandArgs args)
	{
		if ((bool)singleton)
		{
			singleton.PlatformHost(args);
		}
	}

	protected abstract void PlatformHost(ConCommandArgs args);

	[ConCommand(commandName = "steam_get_p2p_session_state")]
	private static void CCSGetP2PSessionState(ConCommandArgs args)
	{
		if ((bool)singleton)
		{
			singleton.PlatformGetP2PSessionState(args);
		}
	}

	protected abstract void PlatformGetP2PSessionState(ConCommandArgs args);

	[ConCommand(commandName = "kick_steam", flags = ConVarFlags.SenderMustBeServer, helpText = "Kicks the user with the specified steam id from the server.")]
	private static void CCKickSteam(ConCommandArgs args)
	{
		if ((bool)singleton)
		{
			singleton.PlatformKick(args);
		}
	}

	protected abstract void PlatformKick(ConCommandArgs args);

	[ConCommand(commandName = "ban_steam", flags = ConVarFlags.SenderMustBeServer, helpText = "Bans the user with the specified steam id from the server.")]
	private static void CCBan(ConCommandArgs args)
	{
		if ((bool)singleton)
		{
			singleton.PlatformBan(args);
		}
	}

	protected abstract void PlatformBan(ConCommandArgs args);

	public virtual bool IsHost()
	{
		return isHost;
	}

	public override void OnStartHost()
	{
		base.OnStartHost();
		isHost = true;
		NetworkManagerSystem.onStartHostGlobal?.Invoke();
	}

	public override void OnStopHost()
	{
		NetworkManagerSystem.onStopHostGlobal?.Invoke();
		isHost = false;
		base.OnStopHost();
	}

	[NetworkMessageHandler(client = true, server = false, msgType = 67)]
	private static void HandleKick(NetworkMessage netMsg)
	{
		KickMessage kickMessage = netMsg.ReadMessage<KickMessage>();
		Debug.LogFormat("Received kick message. Reason={0}", kickMessage.kickReason);
		singleton.StopClient();
		kickMessage.TryGetDisplayTokenAndFormatParams(out var token, out var formatArgs);
		DialogBoxManager.DialogBox(new SimpleDialogBox.TokenParamsPair("DISCONNECTED"), new SimpleDialogBox.TokenParamsPair(token ?? string.Empty, formatArgs), CommonLanguageTokens.ok);
	}

	public static void HandleKick(string displayToken)
	{
		singleton.StopClient();
		DialogBoxManager.DialogBox("DISCONNECTED", displayToken, CommonLanguageTokens.ok);
	}

	[NetworkMessageHandler(msgType = 54, client = true)]
	private static void HandleUpdateTime(NetworkMessage netMsg)
	{
		float num = netMsg.reader.ReadSingle();
		singleton._unpredictedServerFixedTime = num;
		float num2 = Time.time - Time.fixedTime;
		singleton._unpredictedServerFrameTime = num + num2;
	}

	[NetworkMessageHandler(msgType = 64, client = true, server = true)]
	private static void HandleTest(NetworkMessage netMsg)
	{
		int num = netMsg.reader.ReadInt32();
		Debug.LogFormat("Received test packet. value={0}", num);
	}

	public abstract NetworkConnection GetClient(PlatformID clientId);

	public virtual void InitPlatformServer()
	{
	}

	public override void OnStartServer()
	{
		base.OnStartServer();
		NetworkMessageHandlerAttribute.RegisterServerMessages();
		InitializeTime();
		serverNetworkSessionInstance = UnityEngine.Object.Instantiate(RoR2Application.instance.networkSessionPrefab);
		serverPauseStopControllerInstance = UnityEngine.Object.Instantiate(RoR2Application.instance.pauseStopController);
		InitPlatformServer();
		NetworkManagerSystem.onStartServerGlobal?.Invoke();
	}

	public void FireStartServerGlobalEvent()
	{
		NetworkManagerSystem.onStartServerGlobal?.Invoke();
	}

	public override void OnStopServer()
	{
		base.OnStopServer();
		FireStopServerGlobalEvent();
		for (int i = 0; i < NetworkServer.connections.Count; i++)
		{
			NetworkConnection networkConnection = NetworkServer.connections[i];
			if (networkConnection != null)
			{
				OnServerDisconnect(networkConnection);
			}
		}
		UnityEngine.Object.Destroy(serverPauseStopControllerInstance);
		serverPauseStopControllerInstance = null;
		UnityEngine.Object.Destroy(serverNetworkSessionInstance);
		serverNetworkSessionInstance = null;
	}

	public void FireStopServerGlobalEvent()
	{
		NetworkManagerSystem.onStopServerGlobal?.Invoke();
	}

	public override void OnServerConnect(NetworkConnection conn)
	{
		base.OnServerConnect(conn);
	}

	public void FireServerConnectGlobalEvent(NetworkConnection conn)
	{
		NetworkManagerSystem.onServerConnectGlobal?.Invoke(conn);
	}

	public override void OnServerDisconnect(NetworkConnection conn)
	{
		base.OnServerDisconnect(conn);
	}

	public void FireServerDisconnectGlobalEvent(NetworkConnection conn)
	{
		NetworkManagerSystem.onServerDisconnectGlobal?.Invoke(conn);
	}

	public abstract void ServerHandleClientDisconnect(NetworkConnection conn);

	public abstract void ServerBanClient(NetworkConnection conn);

	public void ServerKickClient(NetworkConnection conn, BaseKickReason reason)
	{
		Debug.LogFormat("Kicking client on connection {0}: Reason {1}", conn.connectionId, reason);
		conn.SendByChannel(67, new KickMessage(reason), QosChannelIndex.defaultReliable.intVal);
		conn.FlushChannels();
		KickClient(conn, reason);
	}

	protected abstract void KickClient(NetworkConnection conn, BaseKickReason reason);

	public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId)
	{
		OnServerAddPlayer(conn, playerControllerId, null);
	}

	public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId, NetworkReader extraMessageReader)
	{
		OnServerAddPlayerInternal(conn, playerControllerId, extraMessageReader);
	}

	private void OnServerAddPlayerInternal(NetworkConnection conn, short playerControllerId, NetworkReader extraMessageReader)
	{
		if (base.playerPrefab == null)
		{
			if (LogFilter.logError)
			{
				Debug.LogError("The PlayerPrefab is empty on the NetworkManager. Please setup a PlayerPrefab object.");
			}
			return;
		}
		if (base.playerPrefab.GetComponent<NetworkIdentity>() == null)
		{
			if (LogFilter.logError)
			{
				Debug.LogError("The PlayerPrefab does not have a NetworkIdentity. Please add a NetworkIdentity to the player prefab.");
			}
			return;
		}
		if (playerControllerId < conn.playerControllers.Count && conn.playerControllers[playerControllerId].IsValid && conn.playerControllers[playerControllerId].gameObject != null)
		{
			if (LogFilter.logError)
			{
				Debug.LogErrorFormat("There is already a player at that playerControllerId for this connection, conn = {0}, playerControllerId = {1}.", conn, playerControllerId);
			}
			return;
		}
		if (NetworkUser.readOnlyInstancesList.Count >= base.maxConnections)
		{
			if (LogFilter.logError)
			{
				Debug.LogError("Cannot add any more players.)");
			}
			return;
		}
		if (extraMessageReader == null)
		{
			extraMessageReader = DefaultNetworkReader;
		}
		AddPlayerMessage message = extraMessageReader.ReadMessage<AddPlayerMessage>();
		Transform startPosition = GetStartPosition();
		GameObject gameObject = ((!(startPosition != null)) ? UnityEngine.Object.Instantiate(base.playerPrefab, Vector3.zero, Quaternion.identity) : UnityEngine.Object.Instantiate(base.playerPrefab, startPosition.position, startPosition.rotation));
		Debug.LogFormat("NetworkManagerSystem.AddPlayerInternal(conn={0}, playerControllerId={1}, extraMessageReader={2}", conn, playerControllerId, extraMessageReader);
		NetworkUser component = gameObject.GetComponent<NetworkUser>();
		Util.ConnectionIsLocal(conn);
		component.id = AddPlayerIdFromPlatform(conn, message, (byte)playerControllerId);
		Chat.SendPlayerConnectedMessage(component);
		NetworkServer.AddPlayerForConnection(conn, gameObject, playerControllerId);
	}

	protected abstract NetworkUserId AddPlayerIdFromPlatform(NetworkConnection conn, AddPlayerMessage message, byte playerControllerId);

	protected abstract void UpdateServer();

	private void FixedUpdateServer()
	{
		if (!NetworkServer.active)
		{
			return;
		}
		timeTransmitTimer -= Time.fixedDeltaTime;
		if (timeTransmitTimer <= 0f)
		{
			NetworkWriter networkWriter = new NetworkWriter();
			networkWriter.StartMessage(54);
			networkWriter.Write(unpredictedServerFixedTime);
			networkWriter.FinishMessage();
			NetworkServer.SendWriterToReady(null, networkWriter, QosChannelIndex.time.intVal);
			timeTransmitTimer += svTimeTransmitInterval.value;
		}
		foreach (NetworkConnection connection in NetworkServer.connections)
		{
			if (connection != null && !Util.ConnectionIsLocal(connection))
			{
				RttManager.Ping(connection, QosChannelIndex.ping.intVal);
			}
		}
	}

	public override void OnServerSceneChanged(string sceneName)
	{
		base.OnServerSceneChanged(sceneName);
		if ((bool)Run.instance)
		{
			Run.instance.OnServerSceneChanged(sceneName);
		}
		NetworkManagerSystem.onServerSceneChangedGlobal?.Invoke(sceneName);
		while (clientsReadyDuringLevelTransition.Count > 0)
		{
			NetworkConnection networkConnection = clientsReadyDuringLevelTransition.Dequeue();
			try
			{
				if (networkConnection.isConnected)
				{
					OnServerReady(networkConnection);
				}
			}
			catch (Exception ex)
			{
				Debug.LogErrorFormat("OnServerReady could not be called for client: {0}", ex.Message);
			}
		}
	}

	protected bool IsServerAtMaxConnections()
	{
		ReadOnlyCollection<NetworkConnection> connections = NetworkServer.connections;
		if (connections.Count >= base.maxConnections)
		{
			int num = 0;
			for (int i = 0; i < connections.Count; i++)
			{
				if (connections[i] != null)
				{
					num++;
				}
			}
			return num >= base.maxConnections;
		}
		return false;
	}

	private NetworkUser FindNetworkUserForConnectionServer(NetworkConnection connection)
	{
		ReadOnlyCollection<NetworkUser> readOnlyInstancesList = NetworkUser.readOnlyInstancesList;
		int count = readOnlyInstancesList.Count;
		for (int i = 0; i < count; i++)
		{
			NetworkUser networkUser = readOnlyInstancesList[i];
			if (networkUser.connectionToClient == connection)
			{
				return networkUser;
			}
		}
		return null;
	}

	public int GetConnectingClientCount()
	{
		int num = 0;
		ReadOnlyCollection<NetworkConnection> connections = NetworkServer.connections;
		int count = connections.Count;
		for (int i = 0; i < count; i++)
		{
			NetworkConnection networkConnection = connections[i];
			if (networkConnection != null && !FindNetworkUserForConnectionServer(networkConnection))
			{
				num++;
			}
		}
		return num;
	}

	public void RequestServerShutdown()
	{
		if (!serverShuttingDown)
		{
			serverShuttingDown = true;
			StartCoroutine(ServerShutdownCoroutine());
		}
	}

	private IEnumerator ServerShutdownCoroutine()
	{
		ReadOnlyCollection<NetworkConnection> connections = NetworkServer.connections;
		for (int num = connections.Count - 1; num >= 0; num--)
		{
			NetworkConnection networkConnection = connections[num];
			if (networkConnection != null && !Util.ConnectionIsLocal(networkConnection))
			{
				ServerKickClient(networkConnection, new SimpleLocalizedKickReason("KICK_REASON_SERVERSHUTDOWN"));
			}
		}
		float maxWait = 0.2f;
		for (float t = 0f; t < maxWait; t += Time.unscaledDeltaTime)
		{
			if (CheckConnectionsEmpty())
			{
				break;
			}
			yield return new WaitForEndOfFrame();
		}
		if (client != null)
		{
			StopHost();
		}
		else
		{
			StopServer();
		}
		serverShuttingDown = false;
		static bool CheckConnectionsEmpty()
		{
			foreach (NetworkConnection connection in NetworkServer.connections)
			{
				if (connection != null && !Util.ConnectionIsLocal(connection))
				{
					return false;
				}
			}
			return true;
		}
	}

	private static void ServerHandleReady(NetworkMessage netMsg)
	{
		if (isLoadingScene)
		{
			clientsReadyDuringLevelTransition.Enqueue(netMsg.conn);
		}
		else
		{
			singleton.OnServerReady(netMsg.conn);
		}
	}

	private void RegisterServerOverrideMessages()
	{
		NetworkServer.RegisterHandler(35, ServerHandleReady);
	}

	public override void ServerChangeScene(string newSceneName)
	{
		RegisterServerOverrideMessages();
		base.ServerChangeScene(newSceneName);
	}

	private static bool IsAddressablesKeyValid(string key, Type type)
	{
		foreach (IResourceLocator resourceLocator in Addressables.ResourceLocators)
		{
			if (resourceLocator.Locate(key, typeof(SceneInstance), out var locations) && locations.Count > 0)
			{
				return true;
			}
		}
		return false;
	}

	protected override IChangeSceneAsyncOperation ChangeSceneImplementation(string newSceneName)
	{
		IChangeSceneAsyncOperation changeSceneAsyncOperation = null;
		StringBuilder stringBuilder = HG.StringBuilderPool.RentStringBuilder();
		try
		{
			SceneDef sceneDef = SceneCatalog.FindSceneDef(newSceneName);
			if ((bool)sceneDef)
			{
				string text = sceneDef.sceneAddress?.AssetGUID;
				if (!string.IsNullOrEmpty(text))
				{
					if (IsAddressablesKeyValid(text, typeof(SceneInstance)))
					{
						changeSceneAsyncOperation = new AddressablesChangeSceneAsyncOperation(text, LoadSceneMode.Single, activateOnLoad: false);
					}
					else
					{
						stringBuilder.AppendLine("Scene address is invalid. sceneName=\"").Append(newSceneName).Append("\" sceneAddress=\"")
							.Append(text)
							.Append("\"")
							.AppendLine();
					}
				}
			}
			else if (IsAddressablesKeyValid(newSceneName, typeof(SceneInstance)))
			{
				changeSceneAsyncOperation = new AddressablesChangeSceneAsyncOperation(newSceneName, LoadSceneMode.Single, activateOnLoad: false);
			}
			if (changeSceneAsyncOperation == null)
			{
				changeSceneAsyncOperation = base.ChangeSceneImplementation(newSceneName);
				if (changeSceneAsyncOperation == null)
				{
					stringBuilder.Append("SceneManager.LoadSceneAsync(\"").Append(newSceneName).Append("\" failed.")
						.AppendLine();
				}
			}
			return changeSceneAsyncOperation;
		}
		finally
		{
			if (changeSceneAsyncOperation == null)
			{
				Debug.LogError(stringBuilder.ToString());
			}
			HG.StringBuilderPool.ReturnStringBuilder(stringBuilder);
		}
	}
}
