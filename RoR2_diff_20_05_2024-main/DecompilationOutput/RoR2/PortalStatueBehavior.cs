using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class PortalStatueBehavior : NetworkBehaviour
{
	public enum PortalType
	{
		Shop,
		Goldshores,
		Count
	}

	public PortalType portalType;

	private static int kRpcRpcSetPingable;

	public void GrantPortalEntry()
	{
		switch (portalType)
		{
		case PortalType.Shop:
			if ((bool)TeleporterInteraction.instance)
			{
				TeleporterInteraction.instance.shouldAttemptToSpawnShopPortal = true;
			}
			EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ShrineUseEffect"), new EffectData
			{
				origin = base.transform.position,
				rotation = Quaternion.identity,
				scale = 1f,
				color = ColorCatalog.GetColor(ColorCatalog.ColorIndex.LunarItem)
			}, transmit: true);
			break;
		case PortalType.Goldshores:
			if ((bool)TeleporterInteraction.instance)
			{
				TeleporterInteraction.instance.shouldAttemptToSpawnGoldshoresPortal = true;
			}
			EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ShrineUseEffect"), new EffectData
			{
				origin = base.transform.position,
				rotation = Quaternion.identity,
				scale = 1f,
				color = ColorCatalog.GetColor(ColorCatalog.ColorIndex.Money)
			}, transmit: true);
			break;
		}
		PortalStatueBehavior[] array = Object.FindObjectsOfType<PortalStatueBehavior>();
		foreach (PortalStatueBehavior portalStatueBehavior in array)
		{
			if (portalStatueBehavior.portalType == portalType)
			{
				PurchaseInteraction component = portalStatueBehavior.GetComponent<PurchaseInteraction>();
				if ((bool)component)
				{
					component.Networkavailable = false;
					CallRpcSetPingable(portalStatueBehavior.gameObject, value: false);
				}
			}
		}
	}

	[ClientRpc]
	private void RpcSetPingable(GameObject statueGO, bool value)
	{
		NetworkIdentity component = statueGO.GetComponent<NetworkIdentity>();
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
			((PortalStatueBehavior)obj).RpcSetPingable(reader.ReadGameObject(), reader.ReadBoolean());
		}
	}

	public void CallRpcSetPingable(GameObject statueGO, bool value)
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
		networkWriter.Write(statueGO);
		networkWriter.Write(value);
		SendRPCInternal(networkWriter, 0, "RpcSetPingable");
	}

	static PortalStatueBehavior()
	{
		kRpcRpcSetPingable = 1586108438;
		NetworkBehaviour.RegisterRpcDelegate(typeof(PortalStatueBehavior), kRpcRpcSetPingable, InvokeRpcRpcSetPingable);
		NetworkCRC.RegisterBehaviour("PortalStatueBehavior", 0);
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
