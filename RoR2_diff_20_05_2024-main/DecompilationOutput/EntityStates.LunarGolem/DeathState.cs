using RoR2;
using UnityEngine;

namespace EntityStates.LunarGolem;

public class DeathState : GenericCharacterDeath
{
	public static GameObject deathExplosionEffect;

	public override void OnEnter()
	{
		base.OnEnter();
		if ((bool)deathExplosionEffect)
		{
			EffectManager.SimpleMuzzleFlash(deathExplosionEffect, base.gameObject, "Center", transmit: false);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
	}
}
