using RoR2;
using UnityEngine;

namespace EntityStates.RoboBallBoss;

public class SpawnState : GenericCharacterSpawnState
{
	public static GameObject spawnEffectPrefab;

	public static float spawnEffectRadius;

	public override void OnEnter()
	{
		base.OnEnter();
		if ((bool)spawnEffectPrefab)
		{
			EffectManager.SpawnEffect(spawnEffectPrefab, new EffectData
			{
				origin = base.characterBody.corePosition,
				scale = spawnEffectRadius
			}, transmit: false);
		}
	}
}
