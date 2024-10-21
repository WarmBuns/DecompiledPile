using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class AchievementGranter : NetworkBehaviour
{
	private static int kRpcRpcGrantAchievement;

	[ClientRpc]
	public void RpcGrantAchievement(string achievementName)
	{
		foreach (LocalUser readOnlyLocalUsers in LocalUserManager.readOnlyLocalUsersList)
		{
			AchievementManager.GetUserAchievementManager(readOnlyLocalUsers).GrantAchievement(AchievementManager.GetAchievementDef(achievementName));
		}
	}

	private void UNetVersion()
	{
	}

	protected static void InvokeRpcRpcGrantAchievement(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcGrantAchievement called on server.");
		}
		else
		{
			((AchievementGranter)obj).RpcGrantAchievement(reader.ReadString());
		}
	}

	public void CallRpcGrantAchievement(string achievementName)
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
		networkWriter.Write(achievementName);
		SendRPCInternal(networkWriter, 0, "RpcGrantAchievement");
	}

	static AchievementGranter()
	{
		kRpcRpcGrantAchievement = -180752285;
		NetworkBehaviour.RegisterRpcDelegate(typeof(AchievementGranter), kRpcRpcGrantAchievement, InvokeRpcRpcGrantAchievement);
		NetworkCRC.RegisterBehaviour("AchievementGranter", 0);
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
