using RoR2;
using UnityEngine;

namespace EntityStates.Bell;

public class DeathState : GenericCharacterDeath
{
	public static GameObject initialEffect;

	public static float initialEffectScale;

	public static float velocityMagnitude;

	public static float explosionForce;

	protected override bool shouldAutoDestroy => false;

	public override void OnEnter()
	{
		base.OnEnter();
		Transform modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			if ((bool)modelTransform.GetComponent<ChildLocator>() && (bool)initialEffect)
			{
				EffectManager.SpawnEffect(initialEffect, new EffectData
				{
					origin = base.transform.position,
					scale = initialEffectScale
				}, transmit: false);
			}
			RagdollController component = modelTransform.GetComponent<RagdollController>();
			Rigidbody component2 = GetComponent<Rigidbody>();
			if ((bool)component && (bool)component2)
			{
				component.BeginRagdoll(component2.velocity * velocityMagnitude);
			}
			ExplodeRigidbodiesOnStart component3 = modelTransform.GetComponent<ExplodeRigidbodiesOnStart>();
			if ((bool)component3)
			{
				component3.force = explosionForce;
				component3.enabled = true;
			}
		}
		if ((bool)base.modelLocator)
		{
			base.modelLocator.autoUpdateModelTransform = false;
		}
		DestroyBodyAsapServer();
	}
}
