using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class AmmoPickup : MonoBehaviour
{
	[Tooltip("The base object to destroy when this pickup is consumed.")]
	public GameObject baseObject;

	[Tooltip("The team filter object which determines who can pick up this pack.")]
	public TeamFilter teamFilter;

	public GameObject pickupEffect;

	private bool alive = true;

	private void OnTriggerStay(Collider other)
	{
		if (NetworkServer.active && alive && TeamComponent.GetObjectTeam(other.gameObject) == teamFilter.teamIndex)
		{
			SkillLocator component = other.GetComponent<SkillLocator>();
			if ((bool)component)
			{
				alive = false;
				component.ApplyAmmoPack();
				EffectManager.SimpleEffect(pickupEffect, base.transform.position, Quaternion.identity, transmit: true);
				Object.Destroy(baseObject);
			}
		}
	}
}
