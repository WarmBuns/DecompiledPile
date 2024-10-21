using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Drone;

public class MegaDroneDeathState : GenericCharacterDeath
{
	public static string initialSoundString;

	public static GameObject initialEffect;

	public static float initialEffectScale;

	public static float velocityMagnitude;

	public static float explosionForce;

	public override void OnEnter()
	{
		if (NetworkServer.active)
		{
			EntityState.Destroy(base.gameObject);
		}
	}

	public override void OnExit()
	{
		base.OnExit();
		Util.PlaySound(initialSoundString, base.gameObject);
		Transform modelTransform = GetModelTransform();
		if ((bool)modelTransform && NetworkServer.active)
		{
			ChildLocator component = modelTransform.GetComponent<ChildLocator>();
			if ((bool)component)
			{
				component.FindChild("LeftJet").gameObject.SetActive(value: false);
				component.FindChild("RightJet").gameObject.SetActive(value: false);
				if ((bool)initialEffect)
				{
					EffectManager.SpawnEffect(initialEffect, new EffectData
					{
						origin = base.transform.position,
						scale = initialEffectScale
					}, transmit: true);
				}
			}
		}
		Rigidbody component2 = GetComponent<Rigidbody>();
		RagdollController component3 = modelTransform.GetComponent<RagdollController>();
		if ((bool)component3 && (bool)component2)
		{
			component3.BeginRagdoll(component2.velocity * velocityMagnitude);
		}
		ExplodeRigidbodiesOnStart component4 = modelTransform.GetComponent<ExplodeRigidbodiesOnStart>();
		if ((bool)component4)
		{
			component4.force = explosionForce;
			component4.enabled = true;
		}
	}
}
