using System;
using System.Runtime.InteropServices;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace RoR2;

public class OnCollisionEventController : NetworkBehaviour
{
	[Serializable]
	public class OnCollisionEvent : UnityEvent
	{
	}

	[SerializeField]
	private float monsterCredits;

	[SerializeField]
	private CombatDirector director;

	[SerializeField]
	private bool shouldAccountForMultiplePlayers;

	public OnCollisionEvent onPlayerCollision;

	[SyncVar]
	public bool available = true;

	public bool Networkavailable
	{
		get
		{
			return available;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref available, 1u);
		}
	}

	public override int GetNetworkChannel()
	{
		return QosChannelIndex.defaultReliable.intVal;
	}

	public void OnTriggerStay(Collider other)
	{
		if (NetworkServer.active && available && other.TryGetComponent<CharacterBody>(out var component) && component.isPlayerControlled)
		{
			int participatingPlayerCount = Run.instance.participatingPlayerCount;
			if (shouldAccountForMultiplePlayers)
			{
				monsterCredits *= participatingPlayerCount;
			}
			if ((bool)director)
			{
				director.SetMonsterCredit(monsterCredits);
			}
			onPlayerCollision.Invoke();
		}
	}

	[Server]
	public void SetAvailable(bool setActive)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.OnCollisionEventController::SetAvailable(System.Boolean)' called on client");
		}
		else
		{
			Networkavailable = setActive;
		}
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.Write(available);
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
			writer.Write(available);
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
			available = reader.ReadBoolean();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			available = reader.ReadBoolean();
		}
	}

	public override void PreStartClient()
	{
	}
}
