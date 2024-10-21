using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class RadiotowerTerminal : NetworkBehaviour
{
	[SyncVar(hook = "SetHasBeenPurchased")]
	private bool hasBeenPurchased;

	private UnlockableDef unlockableDef;

	public string unlockSoundString;

	public GameObject unlockEffect;

	private static int kRpcRpcSetPingable;

	public bool NetworkhasBeenPurchased
	{
		get
		{
			return hasBeenPurchased;
		}
		[param: In]
		set
		{
			if (NetworkServer.localClientActive && !base.syncVarHookGuard)
			{
				base.syncVarHookGuard = true;
				SetHasBeenPurchased(value);
				base.syncVarHookGuard = false;
			}
			SetSyncVar(value, ref hasBeenPurchased, 1u);
		}
	}

	private void SetHasBeenPurchased(bool newHasBeenPurchased)
	{
		if (hasBeenPurchased != newHasBeenPurchased)
		{
			NetworkhasBeenPurchased = newHasBeenPurchased;
		}
	}

	public void Start()
	{
		if (NetworkServer.active)
		{
			FindStageLogUnlockable();
		}
		_ = NetworkClient.active;
	}

	private void FindStageLogUnlockable()
	{
		SceneDef mostRecentSceneDef = SceneCatalog.mostRecentSceneDef;
		if ((bool)mostRecentSceneDef)
		{
			unlockableDef = SceneCatalog.GetUnlockableLogFromBaseSceneName(mostRecentSceneDef.baseSceneName);
		}
	}

	[Server]
	public void GrantUnlock(Interactor interactor)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.RadiotowerTerminal::GrantUnlock(RoR2.Interactor)' called on client");
			return;
		}
		EffectManager.SpawnEffect(unlockEffect, new EffectData
		{
			origin = base.transform.position
		}, transmit: true);
		SetHasBeenPurchased(newHasBeenPurchased: true);
		if ((bool)Run.instance)
		{
			Util.PlaySound(unlockSoundString, interactor.gameObject);
			Run.instance.GrantUnlockToAllParticipatingPlayers(unlockableDef);
			string pickupToken = "???";
			if ((bool)unlockableDef)
			{
				pickupToken = unlockableDef.nameToken;
			}
			Chat.SendBroadcastChat(new Chat.PlayerPickupChatMessage
			{
				subjectAsCharacterBody = interactor.GetComponent<CharacterBody>(),
				baseToken = "PLAYER_PICKUP",
				pickupToken = pickupToken,
				pickupColor = ColorCatalog.GetColor(ColorCatalog.ColorIndex.Unlockable),
				pickupQuantity = 1u
			});
			CallRpcSetPingable(value: false);
		}
	}

	[ClientRpc]
	private void RpcSetPingable(bool value)
	{
		NetworkIdentity component = GetComponent<NetworkIdentity>();
		if ((bool)component)
		{
			component.isPingable = value;
		}
	}

	private void UNetVersion()
	{
	}

	protected static void InvokeRpcRpcSetPingable(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcSetPingable called on server.");
		}
		else
		{
			((RadiotowerTerminal)obj).RpcSetPingable(reader.ReadBoolean());
		}
	}

	public void CallRpcSetPingable(bool value)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcSetPingable called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcSetPingable);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.Write(value);
		SendRPCInternal(networkWriter, 0, "RpcSetPingable");
	}

	static RadiotowerTerminal()
	{
		kRpcRpcSetPingable = 310061166;
		NetworkBehaviour.RegisterRpcDelegate(typeof(RadiotowerTerminal), kRpcRpcSetPingable, InvokeRpcRpcSetPingable);
		NetworkCRC.RegisterBehaviour("RadiotowerTerminal", 0);
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.Write(hasBeenPurchased);
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
			writer.Write(hasBeenPurchased);
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
			hasBeenPurchased = reader.ReadBoolean();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			SetHasBeenPurchased(reader.ReadBoolean());
		}
	}

	public override void PreStartClient()
	{
	}
}
