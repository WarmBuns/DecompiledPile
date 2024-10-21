using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class MeridianEventLightningTrigger : NetworkBehaviour
{
	public GameObject lightningStorm;

	public CombatDirector enemySpawnerScript;

	public bool lightningToggle;

	public bool enemySpawnToggle;

	private float monsterCredit;

	[SerializeField]
	private float levelstartMonsterCredit;

	[SerializeField]
	private float expRewardCoefficient;

	[SerializeField]
	private float eliteBias;

	[SerializeField]
	private float spawnDistanceMultiplier;

	private void Start()
	{
		if (NetworkServer.active)
		{
			monsterCredit = (int)(levelstartMonsterCredit * Run.instance.difficultyCoefficient);
		}
	}

	public void OnTriggerExit(Collider other)
	{
		if (other.TryGetComponent<CharacterBody>(out var component) && component.isPlayerControlled)
		{
			LightningStormController.SetStormActive(lightningToggle);
			enemySpawnerScript.enabled = enemySpawnToggle;
			PopulateSceneWithMonsters();
			Object.Destroy(base.gameObject);
		}
	}

	private void PopulateSceneWithMonsters()
	{
		float num = enemySpawnerScript.expRewardCoefficient;
		float num2 = enemySpawnerScript.eliteBias;
		float num3 = enemySpawnerScript.spawnDistanceMultiplier;
		enemySpawnerScript.monsterCredit += monsterCredit;
		enemySpawnerScript.expRewardCoefficient = expRewardCoefficient;
		enemySpawnerScript.eliteBias = eliteBias;
		enemySpawnerScript.spawnDistanceMultiplier = spawnDistanceMultiplier;
		monsterCredit = 0f;
		enemySpawnerScript.SpendAllCreditsOnMapSpawns(TeleporterInteraction.instance ? TeleporterInteraction.instance.transform : null);
		enemySpawnerScript.expRewardCoefficient = num;
		enemySpawnerScript.eliteBias = num2;
		enemySpawnerScript.spawnDistanceMultiplier = num3;
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		bool result = default(bool);
		return result;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
	}

	public override void PreStartClient()
	{
	}
}
