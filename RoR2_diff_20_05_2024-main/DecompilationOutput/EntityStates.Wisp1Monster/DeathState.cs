using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Wisp1Monster;

public class DeathState : GenericCharacterDeath
{
	public static GameObject initialExplosion;

	public override void OnEnter()
	{
		base.OnEnter();
		if ((bool)base.modelLocator)
		{
			if ((bool)base.modelLocator.modelBaseTransform)
			{
				EntityState.Destroy(base.modelLocator.modelBaseTransform.gameObject);
			}
			if ((bool)base.modelLocator.modelTransform)
			{
				EntityState.Destroy(base.modelLocator.modelTransform.gameObject);
			}
		}
		if (!EffectManager.ShouldUsePooledEffect(initialExplosion))
		{
			Object.Instantiate(initialExplosion, base.transform.position, base.transform.rotation);
		}
		else
		{
			EffectManager.SpawnEffect(initialExplosion, new EffectData
			{
				origin = base.transform.position,
				rotation = base.transform.rotation
			}, transmit: false);
		}
		if (NetworkServer.active)
		{
			EntityState.Destroy(base.gameObject);
		}
	}
}
