using RoR2;
using UnityEngine;

namespace EntityStates.VoidJailer;

public class SpawnState : GenericCharacterSpawnState
{
	public static GameObject spawnFXPrefab;

	public static string spawnFXTransformName;

	public override void OnEnter()
	{
		base.OnEnter();
		PlaySpawnFX();
	}

	private void PlaySpawnFX()
	{
		if (spawnFXPrefab != null && !string.IsNullOrEmpty(spawnFXTransformName))
		{
			EffectManager.SimpleMuzzleFlash(spawnFXPrefab, base.gameObject, spawnFXTransformName, transmit: false);
		}
	}
}
