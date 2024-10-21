using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RoR2;

public class VoidMegaCrabItemBehavior : CharacterBody.ItemBehavior
{
	private const float baseSecondsPerSpawn = 60f;

	private const int baseMaxAllies = 1;

	private const int maxAlliesPerStack = 1;

	private const float minSpawnDist = 3f;

	private const float maxSpawnDist = 40f;

	private float spawnTimer;

	private Xoroshiro128Plus rng;

	private WeightedSelection<CharacterSpawnCard> spawnSelection;

	private DirectorPlacementRule placementRule;

	public static int GetMaxProjectiles(Inventory inventory)
	{
		return 1 + (inventory.GetItemCount(DLC1Content.Items.VoidMegaCrabItem) - 1);
	}

	private void Awake()
	{
		base.enabled = false;
		ulong seed = Run.instance.seed ^ (ulong)Run.instance.stageClearCount;
		rng = new Xoroshiro128Plus(seed);
		spawnSelection = new WeightedSelection<CharacterSpawnCard>();
		spawnSelection.AddChoice(Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/DLC1/VoidJailer/cscVoidJailerAlly.asset").WaitForCompletion(), 15f);
		spawnSelection.AddChoice(Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/Base/Nullifier/cscNullifierAlly.asset").WaitForCompletion(), 15f);
		spawnSelection.AddChoice(Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/DLC1/VoidMegaCrab/cscVoidMegaCrabAlly.asset").WaitForCompletion(), 1f);
		placementRule = new DirectorPlacementRule
		{
			placementMode = DirectorPlacementRule.PlacementMode.Approximate,
			minDistance = 3f,
			maxDistance = 40f,
			spawnOnTarget = base.transform
		};
	}

	private void FixedUpdate()
	{
		spawnTimer += Time.fixedDeltaTime;
		if (!body.master.IsDeployableLimited(DeployableSlot.VoidMegaCrabItem) && spawnTimer > 60f / (float)stack)
		{
			spawnTimer = 0f;
			DirectorSpawnRequest directorSpawnRequest = new DirectorSpawnRequest(spawnSelection.Evaluate(rng.nextNormalizedFloat), placementRule, rng);
			directorSpawnRequest.summonerBodyObject = base.gameObject;
			directorSpawnRequest.onSpawnedServer = OnMasterSpawned;
			directorSpawnRequest.summonerBodyObject = base.gameObject;
			DirectorCore.instance.TrySpawnObject(directorSpawnRequest);
		}
	}

	private void OnMasterSpawned(SpawnCard.SpawnResult spawnResult)
	{
		GameObject spawnedInstance = spawnResult.spawnedInstance;
		if (!spawnedInstance)
		{
			return;
		}
		CharacterMaster component = spawnedInstance.GetComponent<CharacterMaster>();
		if ((bool)component)
		{
			Deployable component2 = component.GetComponent<Deployable>();
			if ((bool)component2)
			{
				body.master.AddDeployable(component2, DeployableSlot.VoidMegaCrabItem);
			}
		}
	}
}
