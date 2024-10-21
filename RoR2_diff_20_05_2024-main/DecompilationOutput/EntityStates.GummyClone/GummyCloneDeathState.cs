using RoR2;
using UnityEngine;

namespace EntityStates.GummyClone;

public class GummyCloneDeathState : BaseState
{
	[SerializeField]
	public string soundString;

	[SerializeField]
	public GameObject effectPrefab;

	public override void OnEnter()
	{
		base.OnEnter();
		Util.PlaySound(soundString, base.gameObject);
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
		if (base.isAuthority && (bool)effectPrefab)
		{
			EffectManager.SimpleImpactEffect(effectPrefab, base.transform.position, Vector3.up, transmit: true);
		}
		EntityState.Destroy(base.gameObject);
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Death;
	}
}
