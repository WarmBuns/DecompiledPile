using RoR2;
using UnityEngine;

namespace EntityStates.LunarExploderMonster;

public class SpawnState : GenericCharacterSpawnState
{
	public static GameObject spawnEffectPrefab;

	public static string spawnEffectChildString;

	public override void OnEnter()
	{
		base.OnEnter();
		if ((bool)spawnEffectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(spawnEffectPrefab, base.gameObject, spawnEffectChildString, transmit: false);
		}
	}
}
