using RoR2;
using UnityEngine;

namespace EntityStates.Gup;

internal class SpawnState : GenericCharacterSpawnState
{
	public static GameObject spawnEffectPrefab;

	public static string spawnEffectMuzzle;

	public override void OnEnter()
	{
		base.OnEnter();
		if ((bool)spawnEffectPrefab)
		{
			EffectManager.SpawnEffect(spawnEffectPrefab, new EffectData
			{
				origin = FindModelChild(spawnEffectMuzzle).position,
				scale = base.characterBody.radius
			}, transmit: true);
		}
	}
}
