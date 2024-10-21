using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class MoneyPickup : MonoBehaviour
{
	[Tooltip("The base object to destroy when this pickup is consumed.")]
	public GameObject baseObject;

	[Tooltip("The team filter object which determines who can pick up this pack.")]
	public TeamFilter teamFilter;

	public GameObject pickupEffectPrefab;

	public int baseGoldReward;

	public bool shouldScale;

	private bool alive = true;

	private int goldReward;

	private void Start()
	{
		if (NetworkServer.active)
		{
			goldReward = (shouldScale ? Run.instance.GetDifficultyScaledCost(baseGoldReward) : baseGoldReward);
		}
	}

	private void OnTriggerStay(Collider other)
	{
		if (!NetworkServer.active || !alive)
		{
			return;
		}
		TeamIndex objectTeam = TeamComponent.GetObjectTeam(other.gameObject);
		if (objectTeam == teamFilter.teamIndex)
		{
			alive = false;
			Vector3 position = base.transform.position;
			TeamManager.instance.GiveTeamMoney(objectTeam, (uint)goldReward);
			if ((bool)pickupEffectPrefab)
			{
				EffectManager.SimpleEffect(pickupEffectPrefab, position, Quaternion.identity, transmit: true);
			}
			Object.Destroy(baseObject);
		}
	}
}
