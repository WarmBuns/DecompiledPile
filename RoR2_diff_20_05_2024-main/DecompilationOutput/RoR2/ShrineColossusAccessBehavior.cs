using System.Collections.Generic;
using System.Linq;
using RoR2.Navigation;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(PurchaseInteraction))]
public sealed class ShrineColossusAccessBehavior : ShrineBehavior
{
	[SerializeField]
	private PurchaseInteraction purchaseInteraction;

	private GameObject playerRespawnEffectPrefab;

	private CharacterBody body;

	[SerializeField]
	private Transform symbolTransform;

	private static int kRpcRpcUpdateInteractionClients;

	[Server]
	public void OnInteraction(Interactor interactor)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.ShrineColossusAccessBehavior::OnInteraction(RoR2.Interactor)' called on client");
			return;
		}
		body = interactor.GetComponent<CharacterBody>();
		if ((bool)body && (bool)body.master.playerCharacterMasterController)
		{
			foreach (PlayerCharacterMasterController instance in PlayerCharacterMasterController.instances)
			{
				CharacterBody characterBody = instance.master.GetBody();
				if ((bool)characterBody && characterBody != body)
				{
					int newCount = purchaseInteraction.cost / 10;
					instance.master.GetBody().SetBuffCount(DLC2Content.Buffs.SoulCost.buffIndex, newCount);
				}
			}
		}
		ReviveAlliedPlayers();
		CallRpcSetPingable(value: false);
		CallRpcUpdateInteractionClients();
	}

	[ClientRpc]
	private void RpcUpdateInteractionClients()
	{
		Util.PlaySound("Play_obj_portalShaping_activate", base.gameObject);
		symbolTransform.gameObject.SetActive(value: false);
	}

	[Server]
	private void ReviveAlliedPlayers()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.ShrineColossusAccessBehavior::ReviveAlliedPlayers()' called on client");
			return;
		}
		foreach (PlayerCharacterMasterController instance in PlayerCharacterMasterController.instances)
		{
			CharacterMaster master = instance.master;
			if (instance.isConnected && master.IsDeadAndOutOfLivesServer())
			{
				NodeGraph nodeGraph = SceneInfo.instance.GetNodeGraph(MapNodeGroup.GraphType.Ground);
				List<NodeGraph.NodeIndex> source = nodeGraph.FindNodesInRange(base.gameObject.transform.position, 10f, 30f, HullMask.Human);
				Vector3 position = default(Vector3);
				nodeGraph.GetNodePosition(source.First(), out position);
				master.Respawn(position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), wasRevivedMidStage: true);
				CharacterBody characterBody = master.GetBody();
				if ((bool)characterBody)
				{
					characterBody.AddTimedBuff(RoR2Content.Buffs.Immune, 3f);
					EntityStateMachine[] components = characterBody.GetComponents<EntityStateMachine>();
					foreach (EntityStateMachine obj in components)
					{
						obj.initialStateType = obj.mainStateType;
					}
				}
			}
			else if (instance.isConnected && !master.IsDeadAndOutOfLivesServer() && master.GetBody().healthComponent.alive)
			{
				master.GetBody().AddTimedBuff(RoR2Content.Buffs.Immune, 3f);
				master.GetBody().AddBuff(DLC2Content.Buffs.ExtraLifeBuff);
			}
		}
	}

	private void UNetVersion()
	{
	}

	protected static void InvokeRpcRpcUpdateInteractionClients(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcUpdateInteractionClients called on server.");
		}
		else
		{
			((ShrineColossusAccessBehavior)obj).RpcUpdateInteractionClients();
		}
	}

	public void CallRpcUpdateInteractionClients()
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcUpdateInteractionClients called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcUpdateInteractionClients);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		SendRPCInternal(networkWriter, 0, "RpcUpdateInteractionClients");
	}

	static ShrineColossusAccessBehavior()
	{
		kRpcRpcUpdateInteractionClients = -248324325;
		NetworkBehaviour.RegisterRpcDelegate(typeof(ShrineColossusAccessBehavior), kRpcRpcUpdateInteractionClients, InvokeRpcRpcUpdateInteractionClients);
		NetworkCRC.RegisterBehaviour("ShrineColossusAccessBehavior", 0);
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		bool flag = base.OnSerialize(writer, forceAll);
		bool flag2 = default(bool);
		return flag2 || flag;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
		base.OnDeserialize(reader, initialState);
	}

	public override void PreStartClient()
	{
		base.PreStartClient();
	}
}
