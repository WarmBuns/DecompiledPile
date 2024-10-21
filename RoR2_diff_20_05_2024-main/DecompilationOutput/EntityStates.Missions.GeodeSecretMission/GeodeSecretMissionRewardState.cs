using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Missions.GeodeSecretMission;

public class GeodeSecretMissionRewardState : GeodeSecretMissionEntityStates
{
	private Xoroshiro128Plus rng;

	public override void OnEnter()
	{
		base.OnEnter();
		if (NetworkServer.active)
		{
			rng = new Xoroshiro128Plus(Run.instance.treasureRng.nextUlong);
			DropRewards();
		}
	}

	public void DropRewards()
	{
		int participatingPlayerCount = Run.instance.participatingPlayerCount;
		if (participatingPlayerCount > 0 && (bool)base.gameObject && (bool)geodeSecretMissionController.rewardDropTable)
		{
			int num = geodeSecretMissionController.numberOfRewardsSpawned;
			if (geodeSecretMissionController.increaseRewardPerPlayer)
			{
				num *= participatingPlayerCount;
			}
			float angle = 360f / (float)num;
			Vector3 vector = Quaternion.AngleAxis(Random.Range(0, 360), Vector3.up) * (Vector3.up * 40f + Vector3.forward * 5f);
			Quaternion quaternion = Quaternion.AngleAxis(angle, Vector3.up);
			Vector3 position = geodeSecretMissionController.rewardSpawnLocation.transform.position;
			int num2 = 0;
			while (num2 < num)
			{
				GenericPickupController.CreatePickupInfo pickupInfo = default(GenericPickupController.CreatePickupInfo);
				pickupInfo.pickupIndex = PickupCatalog.FindPickupIndex(geodeSecretMissionController.rewardDisplayTier);
				pickupInfo.pickerOptions = PickupPickerController.GenerateOptionsFromDropTablePlusForcedStorm(geodeSecretMissionController.numberOfRewardOptions, geodeSecretMissionController.rewardDropTable, geodeSecretMissionController.rewardDropTable, rng);
				pickupInfo.rotation = Quaternion.identity;
				pickupInfo.prefabOverride = geodeSecretMissionController.rewardPrefab;
				PickupDropletController.CreatePickupDroplet(pickupInfo, position, vector);
				num2++;
				vector = quaternion * vector;
			}
		}
	}
}
