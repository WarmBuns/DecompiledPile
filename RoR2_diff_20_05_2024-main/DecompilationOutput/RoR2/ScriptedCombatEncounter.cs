using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(CombatSquad))]
public class ScriptedCombatEncounter : MonoBehaviour
{
	[Serializable]
	public struct SpawnInfo
	{
		public SpawnCard spawnCard;

		public Transform explicitSpawnPosition;

		[Range(0f, 100f)]
		[Tooltip("The chance that this spawn card will be culled, removing it from the list. A value of 0 means it is guaranteed.")]
		public float cullChance;
	}

	public ulong seed;

	public bool randomizeSeed;

	public TeamIndex teamIndex;

	public SpawnInfo[] spawns;

	public bool spawnOnStart;

	public bool grantUniqueBonusScaling = true;

	private Xoroshiro128Plus rng;

	public CombatSquad combatSquad { get; private set; }

	public bool hasSpawnedServer { get; private set; }

	public event Action<ScriptedCombatEncounter> onBeginEncounter;

	private void Awake()
	{
		combatSquad = GetComponent<CombatSquad>();
		hasSpawnedServer = false;
		if (NetworkServer.active)
		{
			rng = new Xoroshiro128Plus(randomizeSeed ? Run.instance.stageRng.nextUlong : seed);
		}
	}

	private void Start()
	{
		if (NetworkServer.active && spawnOnStart)
		{
			BeginEncounter();
		}
	}

	private void Spawn(ref SpawnInfo spawnInfo)
	{
		Vector3 position = base.transform.position;
		DirectorPlacementRule directorPlacementRule = new DirectorPlacementRule
		{
			placementMode = DirectorPlacementRule.PlacementMode.NearestNode,
			minDistance = 0f,
			maxDistance = 1000f,
			position = position
		};
		if ((bool)spawnInfo.explicitSpawnPosition)
		{
			directorPlacementRule.placementMode = DirectorPlacementRule.PlacementMode.Direct;
			directorPlacementRule.spawnOnTarget = spawnInfo.explicitSpawnPosition;
		}
		DirectorSpawnRequest directorSpawnRequest = new DirectorSpawnRequest(spawnInfo.spawnCard, directorPlacementRule, rng);
		directorSpawnRequest.ignoreTeamMemberLimit = true;
		directorSpawnRequest.teamIndexOverride = teamIndex;
		directorSpawnRequest.onSpawnedServer = (Action<SpawnCard.SpawnResult>)Delegate.Combine(directorSpawnRequest.onSpawnedServer, new Action<SpawnCard.SpawnResult>(HandleSpawn));
		DirectorCore.instance.TrySpawnObject(directorSpawnRequest);
		void HandleSpawn(SpawnCard.SpawnResult spawnResult)
		{
			GameObject spawnedInstance = spawnResult.spawnedInstance;
			if ((bool)spawnedInstance)
			{
				hasSpawnedServer = true;
				CharacterMaster component = spawnedInstance.GetComponent<CharacterMaster>();
				if (grantUniqueBonusScaling)
				{
					float num = 1f;
					float num2 = 1f;
					num += Run.instance.difficultyCoefficient / 2.5f;
					num2 += Run.instance.difficultyCoefficient / 30f;
					int num3 = Mathf.Max(1, Run.instance.livingPlayerCount);
					num *= Mathf.Pow(num3, 0.5f);
					Debug.LogFormat("Scripted Combat Encounter: currentBoostHpCoefficient={0}, currentBoostDamageCoefficient={1}", num, num2);
					component.inventory.GiveItem(RoR2Content.Items.BoostHp, Mathf.RoundToInt((num - 1f) * 10f));
					component.inventory.GiveItem(RoR2Content.Items.BoostDamage, Mathf.RoundToInt((num2 - 1f) * 10f));
				}
				if (RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.eliteOnlyArtifactDef))
				{
					EliteDef[] array = new EliteDef[4]
					{
						RoR2Content.Elites.Fire,
						RoR2Content.Elites.Lightning,
						RoR2Content.Elites.Ice,
						DLC1Content.Elites.Earth
					};
					int num4 = rng.RangeInt(0, array.Length);
					EquipmentIndex equipmentIndex = array[num4]?.eliteEquipmentDef?.equipmentIndex ?? EquipmentIndex.None;
					if (equipmentIndex != EquipmentIndex.None)
					{
						component.inventory.SetEquipmentIndex(equipmentIndex);
					}
				}
				combatSquad.AddMember(component);
			}
			else
			{
				Debug.LogFormat("No spawned master from combat group!");
			}
		}
	}

	public void BeginEncounter()
	{
		if (hasSpawnedServer || !NetworkServer.active)
		{
			return;
		}
		for (int i = 0; i < spawns.Length; i++)
		{
			ref SpawnInfo reference = ref spawns[i];
			if (!(rng.nextNormalizedFloat * 100f < reference.cullChance))
			{
				Spawn(ref reference);
			}
		}
		this.onBeginEncounter?.Invoke(this);
	}
}
