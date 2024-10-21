using Unity;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Achievements;

[RequireComponent(typeof(NetworkUser))]
public class ServerAchievementTracker : NetworkBehaviour
{
	private BaseServerAchievement[] achievementTrackers;

	private SerializableBitArray maskBitArrayConverter;

	private byte[] maskBuffer;

	private static int kCmdCmdSetAchievementTrackerRequests;

	private static int kRpcRpcGrantAchievement;

	private static int kRpcRpcTryToCompleteActivity;

	public NetworkUser networkUser { get; private set; }

	private void Awake()
	{
		networkUser = GetComponent<NetworkUser>();
		maskBitArrayConverter = new SerializableBitArray(AchievementManager.serverAchievementCount);
		if (NetworkServer.active)
		{
			achievementTrackers = new BaseServerAchievement[AchievementManager.serverAchievementCount];
		}
		if (NetworkClient.active)
		{
			maskBuffer = new byte[maskBitArrayConverter.byteCount];
		}
	}

	private void Start()
	{
		if (networkUser.localUser != null)
		{
			AchievementManager.GetUserAchievementManager(networkUser.localUser)?.TransmitAchievementRequestsToServer();
		}
	}

	private void OnDestroy()
	{
		if (achievementTrackers != null)
		{
			int serverAchievementCount = AchievementManager.serverAchievementCount;
			for (int i = 0; i < serverAchievementCount; i++)
			{
				SetAchievementTracked(new ServerAchievementIndex
				{
					intValue = i
				}, shouldTrack: false);
			}
		}
	}

	[Client]
	public void SendAchievementTrackerRequestsMaskToServer(bool[] serverAchievementsToTrackMask)
	{
		if (!NetworkClient.active)
		{
			Debug.LogWarning("[Client] function 'System.Void RoR2.Achievements.ServerAchievementTracker::SendAchievementTrackerRequestsMaskToServer(System.Boolean[])' called on server");
			return;
		}
		int serverAchievementCount = AchievementManager.serverAchievementCount;
		for (int i = 0; i < serverAchievementCount; i++)
		{
			maskBitArrayConverter[i] = serverAchievementsToTrackMask[i];
		}
		maskBitArrayConverter.GetBytes(maskBuffer);
		CallCmdSetAchievementTrackerRequests(maskBuffer);
	}

	[Command]
	private void CmdSetAchievementTrackerRequests(byte[] packedServerAchievementsToTrackMask)
	{
		int serverAchievementCount = AchievementManager.serverAchievementCount;
		if (packedServerAchievementsToTrackMask.Length << 3 >= serverAchievementCount)
		{
			for (int i = 0; i < serverAchievementCount; i++)
			{
				int num = i >> 3;
				int num2 = i & 7;
				SetAchievementTracked(new ServerAchievementIndex
				{
					intValue = i
				}, ((packedServerAchievementsToTrackMask[num] >> num2) & 1) != 0);
			}
		}
	}

	private void SetAchievementTracked(ServerAchievementIndex serverAchievementIndex, bool shouldTrack)
	{
		BaseServerAchievement baseServerAchievement = achievementTrackers[serverAchievementIndex.intValue];
		if (shouldTrack != (baseServerAchievement != null))
		{
			if (shouldTrack)
			{
				BaseServerAchievement baseServerAchievement2 = BaseServerAchievement.Instantiate(serverAchievementIndex);
				baseServerAchievement2.serverAchievementTracker = this;
				achievementTrackers[serverAchievementIndex.intValue] = baseServerAchievement2;
				baseServerAchievement2.OnInstall();
			}
			else
			{
				baseServerAchievement.OnUninstall();
				achievementTrackers[serverAchievementIndex.intValue] = null;
			}
		}
	}

	[ClientRpc]
	public void RpcGrantAchievement(ServerAchievementIndex serverAchievementIndex)
	{
		LocalUser localUser = networkUser.localUser;
		if (localUser != null)
		{
			AchievementManager.GetUserAchievementManager(localUser)?.HandleServerAchievementCompleted(serverAchievementIndex);
		}
	}

	[ClientRpc]
	public void RpcTryToCompleteActivity(ServerAchievementIndex serverAchievementIndex)
	{
		LocalUser localUser = networkUser.localUser;
		if (localUser != null)
		{
			AchievementManager.GetUserAchievementManager(localUser)?.HandleServerTryToCompleteActivity(serverAchievementIndex);
		}
	}

	private void UNetVersion()
	{
	}

	protected static void InvokeCmdCmdSetAchievementTrackerRequests(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdSetAchievementTrackerRequests called on client.");
		}
		else
		{
			((ServerAchievementTracker)obj).CmdSetAchievementTrackerRequests(reader.ReadBytesAndSize());
		}
	}

	public void CallCmdSetAchievementTrackerRequests(byte[] packedServerAchievementsToTrackMask)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("Command function CmdSetAchievementTrackerRequests called on server.");
			return;
		}
		if (base.isServer)
		{
			CmdSetAchievementTrackerRequests(packedServerAchievementsToTrackMask);
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)5);
		networkWriter.WritePackedUInt32((uint)kCmdCmdSetAchievementTrackerRequests);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.WriteBytesFull(packedServerAchievementsToTrackMask);
		SendCommandInternal(networkWriter, 0, "CmdSetAchievementTrackerRequests");
	}

	protected static void InvokeRpcRpcGrantAchievement(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcGrantAchievement called on server.");
		}
		else
		{
			((ServerAchievementTracker)obj).RpcGrantAchievement(GeneratedNetworkCode._ReadServerAchievementIndex_None(reader));
		}
	}

	protected static void InvokeRpcRpcTryToCompleteActivity(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcTryToCompleteActivity called on server.");
		}
		else
		{
			((ServerAchievementTracker)obj).RpcTryToCompleteActivity(GeneratedNetworkCode._ReadServerAchievementIndex_None(reader));
		}
	}

	public void CallRpcGrantAchievement(ServerAchievementIndex serverAchievementIndex)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcGrantAchievement called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcGrantAchievement);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		GeneratedNetworkCode._WriteServerAchievementIndex_None(networkWriter, serverAchievementIndex);
		SendRPCInternal(networkWriter, 0, "RpcGrantAchievement");
	}

	public void CallRpcTryToCompleteActivity(ServerAchievementIndex serverAchievementIndex)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcTryToCompleteActivity called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcTryToCompleteActivity);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		GeneratedNetworkCode._WriteServerAchievementIndex_None(networkWriter, serverAchievementIndex);
		SendRPCInternal(networkWriter, 0, "RpcTryToCompleteActivity");
	}

	static ServerAchievementTracker()
	{
		kCmdCmdSetAchievementTrackerRequests = 387052099;
		NetworkBehaviour.RegisterCommandDelegate(typeof(ServerAchievementTracker), kCmdCmdSetAchievementTrackerRequests, InvokeCmdCmdSetAchievementTrackerRequests);
		kRpcRpcGrantAchievement = -1713740939;
		NetworkBehaviour.RegisterRpcDelegate(typeof(ServerAchievementTracker), kRpcRpcGrantAchievement, InvokeRpcRpcGrantAchievement);
		kRpcRpcTryToCompleteActivity = -1526039716;
		NetworkBehaviour.RegisterRpcDelegate(typeof(ServerAchievementTracker), kRpcRpcTryToCompleteActivity, InvokeRpcRpcTryToCompleteActivity);
		NetworkCRC.RegisterBehaviour("ServerAchievementTracker", 0);
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		bool result = default(bool);
		return result;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
	}

	public override void PreStartClient()
	{
	}
}
