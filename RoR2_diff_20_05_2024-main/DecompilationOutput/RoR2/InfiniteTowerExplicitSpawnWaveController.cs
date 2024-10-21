using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class InfiniteTowerExplicitSpawnWaveController : InfiniteTowerWaveController
{
	[Serializable]
	private struct SpawnInfo
	{
		[SerializeField]
		public int count;

		[SerializeField]
		public CharacterSpawnCard spawnCard;

		[SerializeField]
		public EliteDef eliteDef;

		[SerializeField]
		public bool preventOverhead;

		[SerializeField]
		public DirectorCore.MonsterSpawnDistance spawnDistance;
	}

	[SerializeField]
	[Tooltip("The information for all of the characters to spawn.")]
	private SpawnInfo[] spawnList;

	[Server]
	public override void Initialize(int waveIndex, Inventory enemyInventory, GameObject spawnTargetObject)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.InfiniteTowerExplicitSpawnWaveController::Initialize(System.Int32,RoR2.Inventory,UnityEngine.GameObject)' called on client");
			return;
		}
		base.Initialize(waveIndex, enemyInventory, spawnTargetObject);
		SpawnInfo[] array = spawnList;
		for (int i = 0; i < array.Length; i++)
		{
			SpawnInfo spawnInfo = array[i];
			for (int j = 0; j < spawnInfo.count; j++)
			{
				combatDirector.Spawn(spawnInfo.spawnCard, spawnInfo.eliteDef, spawnTarget.transform, preventOverhead: spawnInfo.preventOverhead, spawnDistance: spawnInfo.spawnDistance);
			}
		}
	}

	protected override void OnAllEnemiesDefeatedServer()
	{
		base.OnAllEnemiesDefeatedServer();
		InfiniteTowerRun infiniteTowerRun = Run.instance as InfiniteTowerRun;
		if ((bool)infiniteTowerRun && !infiniteTowerRun.IsStageTransitionWave())
		{
			infiniteTowerRun.MoveSafeWard();
		}
	}

	protected override void OnTimerExpire()
	{
	}

	private void UNetVersion()
	{
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
