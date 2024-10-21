using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.JellyfishMonster;

public class DeathState : GenericCharacterDeath
{
	public static GameObject enterEffectPrefab;

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
		if ((bool)enterEffectPrefab)
		{
			if (!EffectManager.ShouldUsePooledEffect(enterEffectPrefab))
			{
				EffectManager.SimpleEffect(enterEffectPrefab, base.transform.position, base.transform.rotation, transmit: false);
			}
			else
			{
				EffectManager.GetAndActivatePooledEffect(enterEffectPrefab, base.transform.position, base.transform.rotation);
			}
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Death;
	}
}
