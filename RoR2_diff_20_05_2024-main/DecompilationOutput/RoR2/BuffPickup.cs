using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class BuffPickup : MonoBehaviour
{
	[Tooltip("The base object to destroy when this pickup is consumed.")]
	public GameObject baseObject;

	[Tooltip("The team filter object which determines who can pick up this pack.")]
	public TeamFilter teamFilter;

	public GameObject pickupEffect;

	public BuffDef buffDef;

	public float buffDuration;

	private bool alive = true;

	private void OnTriggerStay(Collider other)
	{
		if (NetworkServer.active && alive && TeamComponent.GetObjectTeam(other.gameObject) == teamFilter.teamIndex)
		{
			CharacterBody component = other.GetComponent<CharacterBody>();
			if ((bool)component)
			{
				component.AddTimedBuff(buffDef.buffIndex, buffDuration);
				Object.Instantiate(pickupEffect, other.transform.position, Quaternion.identity);
				Object.Destroy(baseObject);
			}
		}
	}
}
