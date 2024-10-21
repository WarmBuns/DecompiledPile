using System.Collections;
using System.Runtime.InteropServices;
using RoR2.Networking;
using RoR2.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;

namespace RoR2;

public class PauseStopController : NetworkBehaviour
{
	private class PauseMessage : MessageBase
	{
		public bool shouldPause;

		public string playerName;

		public override void Serialize(NetworkWriter writer)
		{
			writer.Write(shouldPause);
			writer.Write(playerName);
		}

		public override void Deserialize(NetworkReader reader)
		{
			shouldPause = reader.ReadBoolean();
			playerName = reader.ReadString();
		}
	}

	[SyncVar]
	private bool _allowMultiplayerPause;

	[SyncVar]
	public bool isPaused;

	private float _oldTimeScale;

	private LocalUser _localUser;

	private Coroutine pauseMsgCoroutine;

	private static int kRpcRpcPauseStop;

	public static PauseStopController instance { get; private set; }

	public bool allowMultiplayerPause
	{
		get
		{
			return _allowMultiplayerPause;
		}
		set
		{
			Network_allowMultiplayerPause = value;
		}
	}

	public bool canControlPause
	{
		get
		{
			if (_localUser != null && (bool)_localUser.currentNetworkUser && _localUser.currentNetworkUser.isParticipating)
			{
				if (!_allowMultiplayerPause)
				{
					return RoR2Application.isInSinglePlayer;
				}
				return true;
			}
			return false;
		}
	}

	public bool Network_allowMultiplayerPause
	{
		get
		{
			return _allowMultiplayerPause;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref _allowMultiplayerPause, 1u);
		}
	}

	public bool NetworkisPaused
	{
		get
		{
			return isPaused;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref isPaused, 2u);
		}
	}

	private void Awake()
	{
		_localUser = ((MPEventSystem)EventSystem.current)?.localUser;
		_oldTimeScale = Time.timeScale;
	}

	private void Start()
	{
		Object.DontDestroyOnLoad(base.gameObject);
		if (NetworkServer.active)
		{
			NetworkServer.Spawn(base.gameObject);
		}
	}

	public override void OnStartClient()
	{
		if (isPaused)
		{
			ClientPauseStop(shouldPause: true);
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

	private void OnDestroy()
	{
		ClientPauseStop(shouldPause: false);
	}

	public void Pause(bool shouldPause)
	{
		PauseStopInternal(shouldPause);
	}

	public void ForceClientUnpause()
	{
		ClientPauseStop(shouldPause: false);
	}

	private void PauseStopInternal(bool shouldPause)
	{
		if (!NetworkManager.singleton.isNetworkActive)
		{
			ClientPauseStop(shouldPause: false);
		}
		else if (canControlPause)
		{
			PauseMessage msg = new PauseMessage
			{
				shouldPause = shouldPause,
				playerName = _localUser.currentNetworkUser.GetNetworkPlayerName().GetResolvedName()
			};
			QosChannelIndex defaultReliable = QosChannelIndex.defaultReliable;
			ClientScene.readyConnection.SendByChannel(79, msg, defaultReliable.intVal);
		}
	}

	[NetworkMessageHandler(msgType = 79, client = true, server = true)]
	private static void HandlePauseStop(NetworkMessage netMsg)
	{
		PauseMessage pauseMessage = netMsg.ReadMessage<PauseMessage>();
		instance.PauseStop(pauseMessage);
	}

	private void PauseStop(PauseMessage pauseMessage)
	{
		if (isPaused == pauseMessage.shouldPause)
		{
			return;
		}
		NetworkisPaused = pauseMessage.shouldPause;
		CallRpcPauseStop(pauseMessage.shouldPause);
		if (!NetworkServer.dontListen)
		{
			if (isPaused)
			{
				Chat.SendPlayerPausedMessage(pauseMessage.playerName);
			}
			else
			{
				Chat.SendPlayerResumedMessage(pauseMessage.playerName);
			}
		}
	}

	[ClientRpc]
	private void RpcPauseStop(bool shouldPause)
	{
		ClientPauseStop(shouldPause);
	}

	private void ClientPauseStop(bool shouldPause)
	{
		NetworkisPaused = shouldPause;
		if (shouldPause)
		{
			_oldTimeScale = Time.timeScale;
			Time.timeScale = 0f;
			if (PauseManager.onPauseStartGlobal != null)
			{
				PauseManager.onPauseStartGlobal();
				if (!HUD.IsScoreboardActive(_localUser))
				{
					PauseManager.OpenMinimalPauseScreen();
				}
			}
			pauseMsgCoroutine = StartCoroutine(SendPauseMsg());
		}
		else
		{
			Time.timeScale = _oldTimeScale;
			if (PauseManager.onPauseEndGlobal != null)
			{
				PauseManager.onPauseEndGlobal();
				PauseManager.CloseMinimalPauseScreen();
			}
			if (pauseMsgCoroutine != null)
			{
				StopCoroutine(pauseMsgCoroutine);
			}
		}
	}

	private IEnumerator SendPauseMsg()
	{
		while (true)
		{
			yield return new WaitForSecondsRealtime(2f);
			PauseMessage msg = new PauseMessage
			{
				shouldPause = isPaused,
				playerName = _localUser.currentNetworkUser.GetNetworkPlayerName().GetResolvedName()
			};
			QosChannelIndex defaultReliable = QosChannelIndex.defaultReliable;
			ClientScene.readyConnection.SendByChannel(79, msg, defaultReliable.intVal);
		}
	}

	private void ServerPauseStop(bool shouldPause)
	{
		if (shouldPause)
		{
			_oldTimeScale = Time.timeScale;
			Time.timeScale = 0f;
		}
		else
		{
			Time.timeScale = _oldTimeScale;
		}
	}

	private void UNetVersion()
	{
	}

	protected static void InvokeRpcRpcPauseStop(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcPauseStop called on server.");
		}
		else
		{
			((PauseStopController)obj).RpcPauseStop(reader.ReadBoolean());
		}
	}

	public void CallRpcPauseStop(bool shouldPause)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcPauseStop called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcPauseStop);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.Write(shouldPause);
		SendRPCInternal(networkWriter, 0, "RpcPauseStop");
	}

	static PauseStopController()
	{
		kRpcRpcPauseStop = -1375384466;
		NetworkBehaviour.RegisterRpcDelegate(typeof(PauseStopController), kRpcRpcPauseStop, InvokeRpcRpcPauseStop);
		NetworkCRC.RegisterBehaviour("PauseStopController", 0);
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.Write(_allowMultiplayerPause);
			writer.Write(isPaused);
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
			writer.Write(_allowMultiplayerPause);
		}
		if ((base.syncVarDirtyBits & 2u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(isPaused);
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
			_allowMultiplayerPause = reader.ReadBoolean();
			isPaused = reader.ReadBoolean();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			_allowMultiplayerPause = reader.ReadBoolean();
		}
		if (((uint)num & 2u) != 0)
		{
			isPaused = reader.ReadBoolean();
		}
	}

	public override void PreStartClient()
	{
	}
}
