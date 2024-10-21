using System.Collections.Generic;
using UnityEngine;

namespace RoR2;

public class GeodeSecretMissionController : MonoBehaviour
{
	public int numberOfGeodesNecessary = 1;

	public List<GameObject> geodeMissionObjects = new List<GameObject>();

	public GameObject rewardSpawnLocation;

	public EntityStateMachine entityStateMachine;

	public GameObject rewardPrefab;

	public bool increaseRewardPerPlayer;

	public int numberOfRewardsSpawned = 1;

	public int numberOfRewardOptions = 3;

	public BasicPickupDropTable rewardDropTable;

	[Tooltip("Use this tier to get a pickup index for the reward. The droplet's visuals will correspond to this.")]
	public ItemTier rewardDisplayTier;

	[HideInInspector]
	public int geodeInteractionsTracker;

	public OnCollisionEventController onCollisionEventController;

	public void AdvanceGeodeSecretMission()
	{
		geodeInteractionsTracker++;
		CheckIfRewardShouldBeGranted();
	}

	public void CheckIfRewardShouldBeGranted()
	{
		if (numberOfGeodesNecessary <= geodeInteractionsTracker)
		{
			onCollisionEventController.SetAvailable(setActive: true);
		}
	}
}
