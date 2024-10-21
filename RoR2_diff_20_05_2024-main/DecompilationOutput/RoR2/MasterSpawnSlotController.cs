using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class MasterSpawnSlotController : NetworkBehaviour
{
	public interface ISlot
	{
		bool IsOpen();

		void Spawn(GameObject summonerBodyObject, Xoroshiro128Plus rng, Action<ISlot, SpawnCard.SpawnResult> callback = null);

		void Kill();
	}

	public List<ISlot> slots = new List<ISlot>();

	[SyncVar]
	private int _openSlotCount;

	public int openSlotCount
	{
		get
		{
			if (NetworkServer.active)
			{
				return CalcOpenSlotCount();
			}
			return _openSlotCount;
		}
	}

	public int Network_openSlotCount
	{
		get
		{
			return _openSlotCount;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref _openSlotCount, 1u);
		}
	}

	private int CalcOpenSlotCount()
	{
		int num = 0;
		foreach (ISlot slot in slots)
		{
			if (slot.IsOpen())
			{
				num++;
			}
		}
		return num;
	}

	private void OnEnable()
	{
		GetComponents(slots);
	}

	[Server]
	public void SpawnAllOpen(GameObject summonerBodyObject, Xoroshiro128Plus rng, Action<ISlot, SpawnCard.SpawnResult> callback = null)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.MasterSpawnSlotController::SpawnAllOpen(UnityEngine.GameObject,Xoroshiro128Plus,System.Action`2<RoR2.MasterSpawnSlotController/ISlot,RoR2.SpawnCard/SpawnResult>)' called on client");
			return;
		}
		foreach (ISlot slot in slots)
		{
			if (slot.IsOpen())
			{
				slot.Spawn(summonerBodyObject, rng, callback);
			}
		}
	}

	[Server]
	public void SpawnRandomOpen(int spawnCount, Xoroshiro128Plus rng, GameObject summonerBodyObject, Action<ISlot, SpawnCard.SpawnResult> callback = null)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.MasterSpawnSlotController::SpawnRandomOpen(System.Int32,Xoroshiro128Plus,UnityEngine.GameObject,System.Action`2<RoR2.MasterSpawnSlotController/ISlot,RoR2.SpawnCard/SpawnResult>)' called on client");
			return;
		}
		List<ISlot> list = new List<ISlot>();
		foreach (ISlot slot in slots)
		{
			if (slot.IsOpen())
			{
				list.Add(slot);
			}
		}
		Util.ShuffleList(list, rng);
		for (int i = 0; i < spawnCount && i < list.Count; i++)
		{
			list[i].Spawn(summonerBodyObject, rng, callback);
		}
	}

	[Server]
	public void KillAll()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.MasterSpawnSlotController::KillAll()' called on client");
			return;
		}
		foreach (ISlot slot in slots)
		{
			slot.Kill();
		}
	}

	private void Update()
	{
		if (NetworkServer.active)
		{
			Network_openSlotCount = CalcOpenSlotCount();
		}
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.WritePackedUInt32((uint)_openSlotCount);
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
			writer.WritePackedUInt32((uint)_openSlotCount);
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
			_openSlotCount = (int)reader.ReadPackedUInt32();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			_openSlotCount = (int)reader.ReadPackedUInt32();
		}
	}

	public override void PreStartClient()
	{
	}
}
