using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Drone;

public class DeathState : GenericCharacterDeath
{
	public class RigidbodyCollisionListener : MonoBehaviour
	{
		public DeathState deathState;

		private void OnCollisionEnter(Collision collision)
		{
			deathState.OnImpactServer(collision.GetContact(0).point);
			deathState.Explode();
		}
	}

	[SerializeField]
	public GameObject initialExplosionEffect;

	[SerializeField]
	public GameObject deathExplosionEffect;

	[SerializeField]
	public string initialSoundString;

	[SerializeField]
	public string deathSoundString;

	[SerializeField]
	public float deathEffectRadius;

	[SerializeField]
	public float forceAmount = 20f;

	[SerializeField]
	public float deathDuration = 2f;

	[SerializeField]
	public bool destroyOnImpact;

	private RigidbodyCollisionListener rigidbodyCollisionListener;

	public override void OnEnter()
	{
		base.OnEnter();
		Util.PlaySound(initialSoundString, base.gameObject);
		if ((bool)base.rigidbodyMotor)
		{
			base.rigidbodyMotor.forcePID.enabled = false;
			base.rigidbodyMotor.rigid.useGravity = true;
			base.rigidbodyMotor.rigid.AddForce(Vector3.up * forceAmount, ForceMode.Force);
			base.rigidbodyMotor.rigid.collisionDetectionMode = CollisionDetectionMode.Continuous;
		}
		if ((bool)base.rigidbodyDirection)
		{
			base.rigidbodyDirection.enabled = false;
		}
		if ((bool)initialExplosionEffect)
		{
			EffectManager.SpawnEffect(deathExplosionEffect, new EffectData
			{
				origin = base.characterBody.corePosition,
				scale = base.characterBody.radius + deathEffectRadius
			}, transmit: false);
		}
		if (base.isAuthority && destroyOnImpact)
		{
			rigidbodyCollisionListener = base.gameObject.AddComponent<RigidbodyCollisionListener>();
			rigidbodyCollisionListener.deathState = this;
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (NetworkServer.active && base.fixedAge > deathDuration)
		{
			Explode();
		}
	}

	public void Explode()
	{
		EntityState.Destroy(base.gameObject);
	}

	public virtual void OnImpactServer(Vector3 contactPoint)
	{
		string bodyName = BodyCatalog.GetBodyName(base.characterBody.bodyIndex);
		bodyName = bodyName.Replace("Body", "");
		bodyName = "iscBroken" + bodyName;
		SpawnCard spawnCard = LegacyResourcesAPI.Load<SpawnCard>("SpawnCards/InteractableSpawnCard/" + bodyName);
		if (!(spawnCard != null))
		{
			return;
		}
		DirectorPlacementRule placementRule = new DirectorPlacementRule
		{
			placementMode = DirectorPlacementRule.PlacementMode.Direct,
			position = contactPoint
		};
		GameObject gameObject = DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(spawnCard, placementRule, new Xoroshiro128Plus(0uL)));
		if ((bool)gameObject)
		{
			PurchaseInteraction component = gameObject.GetComponent<PurchaseInteraction>();
			if ((bool)component && component.costType == CostTypeIndex.Money)
			{
				component.Networkcost = Run.instance.GetDifficultyScaledCost(component.cost);
			}
		}
	}

	public override void OnExit()
	{
		if ((bool)deathExplosionEffect)
		{
			EffectManager.SpawnEffect(deathExplosionEffect, new EffectData
			{
				origin = base.characterBody.corePosition,
				scale = base.characterBody.radius + deathEffectRadius
			}, transmit: false);
		}
		if ((bool)rigidbodyCollisionListener)
		{
			EntityState.Destroy(rigidbodyCollisionListener);
		}
		Util.PlaySound(deathSoundString, base.gameObject);
		base.OnExit();
	}
}
