using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.BrotherMonster;

public class InstantDeathState : GenericCharacterDeath
{
	public static GameObject deathEffectPrefab;

	protected override bool shouldAutoDestroy => false;

	public override void OnEnter()
	{
		base.OnEnter();
		DestroyModel();
		if (NetworkServer.active)
		{
			DestroyBodyAsapServer();
		}
	}

	protected override void CreateDeathEffects()
	{
		base.CreateDeathEffects();
		EffectManager.SimpleMuzzleFlash(deathEffectPrefab, base.gameObject, "MuzzleCenter", transmit: false);
	}
}
