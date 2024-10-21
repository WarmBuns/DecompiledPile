using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class HealthPickup : MonoBehaviour
{
	[Tooltip("The base object to destroy when this pickup is consumed.")]
	public GameObject baseObject;

	[Tooltip("The team filter object which determines who can pick up this pack.")]
	public TeamFilter teamFilter;

	public GameObject pickupEffect;

	public float flatHealing;

	public float fractionalHealing;

	private bool alive = true;

	private void OnTriggerStay(Collider other)
	{
		if (!NetworkServer.active || !alive || TeamComponent.GetObjectTeam(other.gameObject) != teamFilter.teamIndex)
		{
			return;
		}
		CharacterBody component = other.GetComponent<CharacterBody>();
		if ((bool)component)
		{
			HealthComponent healthComponent = component.healthComponent;
			if ((bool)healthComponent)
			{
				component.healthComponent.Heal(flatHealing + healthComponent.fullHealth * fractionalHealing, default(ProcChainMask));
				EffectManager.SpawnEffect(pickupEffect, new EffectData
				{
					origin = base.transform.position
				}, transmit: true);
			}
			Object.Destroy(baseObject);
		}
	}
}
