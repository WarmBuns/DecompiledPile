using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.MegaConstruct;

public class FallingDeath : GenericCharacterDeath
{
	public static float deathDelay;

	public static GameObject enterEffectPrefab;

	public static GameObject deathEffectPrefab;

	public static float explosionForce;

	public static string standableSurfaceChildName;

	private bool hasDied;

	public override void OnEnter()
	{
		base.OnEnter();
		EffectManager.SimpleImpactEffect(enterEffectPrefab, base.characterBody.corePosition, Vector3.up, transmit: true);
		MasterSpawnSlotController component = GetComponent<MasterSpawnSlotController>();
		if (NetworkServer.active && (bool)component)
		{
			component.KillAll();
		}
		ChildLocator modelChildLocator = GetModelChildLocator();
		if ((bool)modelChildLocator)
		{
			Transform transform = modelChildLocator.FindChild(standableSurfaceChildName);
			if ((bool)transform)
			{
				transform.gameObject.SetActive(value: false);
			}
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge > deathDelay && NetworkServer.active && !hasDied)
		{
			hasDied = true;
			EffectManager.SimpleImpactEffect(deathEffectPrefab, base.characterBody.corePosition, Vector3.up, transmit: true);
			DestroyBodyAsapServer();
		}
	}

	public override void OnExit()
	{
		Transform modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			Rigidbody component = GetComponent<Rigidbody>();
			RagdollController component2 = modelTransform.GetComponent<RagdollController>();
			if ((bool)component2 && (bool)component)
			{
				component2.BeginRagdoll(component.velocity);
			}
			ExplodeRigidbodiesOnStart component3 = modelTransform.GetComponent<ExplodeRigidbodiesOnStart>();
			if ((bool)component3)
			{
				component3.force = explosionForce;
				component3.enabled = true;
			}
		}
		Animator modelAnimator = GetModelAnimator();
		if ((bool)modelAnimator)
		{
			modelAnimator.enabled = false;
		}
		base.OnExit();
	}
}
