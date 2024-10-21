using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Fauna;

public class HabitatFruitDeathState : BaseState
{
	[SerializeField]
	public static GameObject initialExplosion;

	[SerializeField]
	public static string deathSoundString;

	public static int healPackCount;

	public static float healPackMaxVelocity;

	public static float fractionalHealing;

	public static float scale;

	public override void OnEnter()
	{
		base.OnEnter();
		Util.PlaySound(deathSoundString, base.gameObject);
		if ((bool)initialExplosion && NetworkServer.active)
		{
			EffectManager.SimpleImpactEffect(initialExplosion, base.transform.position, Vector3.up, transmit: true);
		}
		if (NetworkServer.active)
		{
			for (int i = 0; i < healPackCount; i++)
			{
				GameObject obj = Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/HealPack"), base.transform.position, Random.rotation);
				obj.GetComponent<TeamFilter>().teamIndex = TeamIndex.Player;
				obj.GetComponentInChildren<HealthPickup>().fractionalHealing = fractionalHealing;
				obj.transform.localScale = new Vector3(scale, scale, scale);
				obj.GetComponent<Rigidbody>().AddForce(Random.insideUnitSphere * healPackMaxVelocity, ForceMode.VelocityChange);
				NetworkServer.Spawn(obj);
			}
		}
		EntityState.Destroy(base.gameObject);
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Death;
	}
}
