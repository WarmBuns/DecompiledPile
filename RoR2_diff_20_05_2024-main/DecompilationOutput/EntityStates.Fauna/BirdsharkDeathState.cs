using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Fauna;

public class BirdsharkDeathState : BaseState
{
	[SerializeField]
	public GameObject initialExplosion;

	[SerializeField]
	public string deathSoundString;

	public override void OnEnter()
	{
		base.OnEnter();
		Util.PlaySound(deathSoundString, base.gameObject);
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
		if ((bool)initialExplosion && NetworkServer.active)
		{
			EffectManager.SimpleImpactEffect(initialExplosion, base.transform.position, Vector3.up, transmit: true);
		}
		EntityState.Destroy(base.gameObject);
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Death;
	}
}
