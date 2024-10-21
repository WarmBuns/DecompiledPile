using System;
using System.Collections.Generic;
using EntityStates;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[DisallowMultipleComponent]
public class NetworkStateMachine : NetworkBehaviour
{
	[SerializeField]
	[Tooltip("The sibling state machine components to network.")]
	private EntityStateMachine[] stateMachines = Array.Empty<EntityStateMachine>();

	private NetworkIdentity networkIdentity;

	private void Awake()
	{
		networkIdentity = GetComponent<NetworkIdentity>();
		for (int i = 0; i < stateMachines.Length; i++)
		{
			EntityStateMachine obj = stateMachines[i];
			obj.networkIndex = i;
			obj.networker = this;
			obj.networkIdentity = networkIdentity;
		}
	}

	public override bool OnSerialize(NetworkWriter writer, bool initialState)
	{
		if (initialState)
		{
			for (int i = 0; i < stateMachines.Length; i++)
			{
				EntityStateMachine entityStateMachine = stateMachines[i];
				writer.Write(EntityStateCatalog.GetStateIndex(entityStateMachine.state.GetType()));
				entityStateMachine.state.OnSerialize(writer);
			}
			return true;
		}
		return false;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
		if (!initialState)
		{
			return;
		}
		for (int i = 0; i < stateMachines.Length; i++)
		{
			EntityStateMachine entityStateMachine = stateMachines[i];
			EntityStateIndex entityStateIndex = reader.ReadEntityStateIndex();
			if (base.hasAuthority)
			{
				continue;
			}
			EntityState entityState = EntityStateCatalog.InstantiateState(entityStateIndex);
			if (entityState != null)
			{
				entityState.outer = entityStateMachine;
				entityState.OnDeserialize(reader);
				if (!stateMachines[i])
				{
					Debug.LogErrorFormat("State machine [{0}] on object {1} is not set! incoming state = {2}", i, base.gameObject, entityState.GetType());
				}
				entityStateMachine.SetNextState(entityState);
			}
		}
	}

	[NetworkMessageHandler(msgType = 48, client = true, server = true)]
	public static void HandleSetEntityState(NetworkMessage netMsg)
	{
		NetworkIdentity networkIdentity = netMsg.reader.ReadNetworkIdentity();
		byte b = netMsg.reader.ReadByte();
		EntityStateIndex entityStateIndex = netMsg.reader.ReadEntityStateIndex();
		if (networkIdentity == null)
		{
			return;
		}
		NetworkStateMachine component = networkIdentity.gameObject.GetComponent<NetworkStateMachine>();
		if (component == null || b >= component.stateMachines.Length)
		{
			return;
		}
		EntityStateMachine entityStateMachine = component.stateMachines[b];
		if (entityStateMachine == null)
		{
			return;
		}
		if (networkIdentity.isServer)
		{
			HashSet<NetworkInstanceId> clientOwnedObjects = netMsg.conn.clientOwnedObjects;
			if (clientOwnedObjects == null || !clientOwnedObjects.Contains(networkIdentity.netId))
			{
				return;
			}
		}
		else if (networkIdentity.hasAuthority)
		{
			return;
		}
		EntityState entityState = EntityStateCatalog.InstantiateState(entityStateIndex);
		if (entityState != null)
		{
			entityState.outer = entityStateMachine;
			entityState.OnDeserialize(netMsg.reader);
			entityStateMachine.SetState(entityState);
		}
	}

	public void SendSetEntityState(int stateMachineIndex)
	{
		if (NetworkServer.active || base.hasAuthority)
		{
			NetworkWriter networkWriter = new NetworkWriter();
			EntityStateMachine entityStateMachine = stateMachines[stateMachineIndex];
			networkWriter.StartMessage(48);
			networkWriter.Write(networkIdentity);
			networkWriter.Write((byte)stateMachineIndex);
			networkWriter.Write(EntityStateCatalog.GetStateIndex(entityStateMachine.state.GetType()));
			entityStateMachine.state.OnSerialize(networkWriter);
			networkWriter.FinishMessage();
			if (NetworkServer.active)
			{
				NetworkServer.SendWriterToReady(base.gameObject, networkWriter, GetNetworkChannel());
			}
			else if (ClientScene.readyConnection != null)
			{
				ClientScene.readyConnection.SendWriter(networkWriter, GetNetworkChannel());
			}
		}
	}

	private void OnValidate()
	{
		for (int i = 0; i < stateMachines.Length; i++)
		{
			if (!stateMachines[i])
			{
				Debug.LogErrorFormat("{0} has a blank entry for NetworkStateMachine!", base.gameObject);
			}
		}
	}

	private void UNetVersion()
	{
	}

	public override void PreStartClient()
	{
	}
}
