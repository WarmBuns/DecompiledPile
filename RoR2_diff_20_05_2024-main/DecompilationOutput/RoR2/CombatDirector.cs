using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RoR2.ConVar;
using RoR2.Navigation;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.Serialization;

namespace RoR2;

public class CombatDirector : MonoBehaviour
{
	[Serializable]
	public class OnSpawnedServer : UnityEvent<GameObject>
	{
	}

	public class EliteTierDef
	{
		public float costMultiplier;

		public EliteDef[] eliteTypes;

		public Func<SpawnCard.EliteRules, bool> isAvailable = (SpawnCard.EliteRules rules) => true;

		public bool canSelectWithoutAvailableEliteDef;

		private List<EliteDef> availableDefs = new List<EliteDef>();

		public bool CanSelect(SpawnCard.EliteRules rules)
		{
			if (isAvailable(rules))
			{
				if (!canSelectWithoutAvailableEliteDef)
				{
					return HasAnyAvailableEliteDefs();
				}
				return true;
			}
			return false;
		}

		public bool HasAnyAvailableEliteDefs()
		{
			EliteDef[] array = eliteTypes;
			foreach (EliteDef eliteDef in array)
			{
				if ((bool)eliteDef && eliteDef.IsAvailable())
				{
					return true;
				}
			}
			return false;
		}

		public EliteDef GetRandomAvailableEliteDef(Xoroshiro128Plus rng)
		{
			availableDefs.Clear();
			EliteDef[] array = eliteTypes;
			foreach (EliteDef eliteDef in array)
			{
				if ((bool)eliteDef && eliteDef.IsAvailable())
				{
					availableDefs.Add(eliteDef);
				}
			}
			if (availableDefs.Count > 0)
			{
				return rng.NextElementUniform(availableDefs);
			}
			return null;
		}
	}

	private class DirectorMoneyWave
	{
		public float interval;

		public float timer;

		public float multiplier;

		private float accumulatedAward;

		public float Update(float deltaTime, float difficultyCoefficient)
		{
			timer += deltaTime;
			if (timer > interval)
			{
				float num = 0.5f + (float)Run.instance.participatingPlayerCount * 0.5f;
				timer -= interval;
				float num2 = 1f;
				float num3 = 0.4f;
				accumulatedAward += interval * multiplier * (num2 + num3 * difficultyCoefficient) * num;
			}
			float num4 = Mathf.FloorToInt(accumulatedAward);
			accumulatedAward -= num4;
			return num4;
		}
	}

	[Header("Core Director Values")]
	public string customName;

	public float monsterCredit;

	[Tooltip("Monster credit that's been refunded from culling non-elite enemies. Can only be used to buy non-elite enemies.")]
	public float refundedMonsterCredit;

	public float expRewardCoefficient = 0.2f;

	public float goldRewardCoefficient = 1f;

	public float minSeriesSpawnInterval = 0.1f;

	public float maxSeriesSpawnInterval = 1f;

	public float minRerollSpawnInterval = 2.3333333f;

	public float maxRerollSpawnInterval = 4.3333335f;

	public RangeFloat[] moneyWaveIntervals;

	public TeamIndex teamIndex = TeamIndex.Monster;

	[Header("Optional Behaviors")]
	[Tooltip("How much to multiply money wave yield by.")]
	public float creditMultiplier = 1f;

	[Tooltip("The coefficient to multiply spawn distances. Used for combat shrines, to keep spawns nearby.")]
	public float spawnDistanceMultiplier = 1f;

	[Tooltip("The maximum distance at which enemies will spawn.")]
	public float maxSpawnDistance = float.PositiveInfinity;

	[Tooltip("Ensure that the minimum spawn distance is at least this many units away from the maxSpawnDistance")]
	public float minSpawnRange;

	public bool shouldSpawnOneWave;

	public bool targetPlayers = true;

	public bool skipSpawnIfTooCheap = true;

	[Tooltip("If skipSpawnIfTooCheap is true, we'll behave as though it's not set after this many consecutive skips")]
	public int maxConsecutiveCheapSkips = int.MaxValue;

	public bool resetMonsterCardIfFailed = true;

	public int maximumNumberToSpawnBeforeSkipping = 6;

	public float eliteBias = 1f;

	public OnSpawnedServer onSpawnedServer;

	[FormerlySerializedAs("_combatSquad")]
	public CombatSquad combatSquad;

	[Tooltip("A special effect for when a monster appears will be instantiated at its position. Used for combat shrine.")]
	public GameObject spawnEffectPrefab;

	public bool ignoreTeamSizeLimit;

	[SerializeField]
	private DirectorCardCategorySelection _monsterCards;

	public bool fallBackToStageMonsterCards = true;

	public static readonly List<CombatDirector> instancesList = new List<CombatDirector>();

	private bool hasStartedWave;

	private Xoroshiro128Plus rng;

	private DirectorCard currentMonsterCard;

	private EliteTierDef currentActiveEliteTier;

	private EliteDef currentActiveEliteDef;

	private int currentMonsterCardCost;

	private WeightedSelection<DirectorCard> monsterCardsSelection;

	private int consecutiveCheapSkips;

	public GameObject currentSpawnTarget;

	private float playerRetargetTimer;

	private static readonly float baseEliteCostMultiplier = 6f;

	private static EliteTierDef[] eliteTiers;

	private int spawnCountInCurrentWave;

	public static readonly BoolConVar cvDirectorCombatDisable = new BoolConVar("director_combat_disable", ConVarFlags.SenderMustBeServer | ConVarFlags.Cheat, "0", "Disables all combat directors.");

	private static readonly BoolConVar cvDirectorCombatEnableInternalLogs = new BoolConVar("director_combat_enable_internal_logs", ConVarFlags.None, "0", "Enables all combat directors to print internal logging.");

	private DirectorMoneyWave[] moneyWaves;

	private bool isHalcyonShrineSpawn;

	private int shrineHalcyoniteDifficultyLevel;

	public float monsterSpawnTimer { get; set; }

	public DirectorCard lastAttemptedMonsterCard { get; set; }

	public float totalCreditsSpent { get; private set; }

	private WeightedSelection<DirectorCard> finalMonsterCardsSelection
	{
		get
		{
			WeightedSelection<DirectorCard> monsterSelection = monsterCardsSelection;
			if (monsterSelection == null)
			{
				ClassicStageInfo instance = ClassicStageInfo.instance;
				if ((object)instance == null)
				{
					return null;
				}
				monsterSelection = instance.monsterSelection;
			}
			return monsterSelection;
		}
	}

	private DirectorCardCategorySelection monsterCards
	{
		get
		{
			return _monsterCards;
		}
		set
		{
			if (_monsterCards != value)
			{
				_monsterCards = value;
				monsterCardsSelection = _monsterCards?.GenerateDirectorCardWeightedSelection();
			}
		}
	}

	public static float lowestEliteCostMultiplier => eliteTiers[1].costMultiplier;

	private int mostExpensiveMonsterCostInDeck
	{
		get
		{
			int num = 0;
			for (int i = 0; i < finalMonsterCardsSelection.Count; i++)
			{
				DirectorCard value = finalMonsterCardsSelection.GetChoice(i).value;
				int num2 = value.cost;
				if (!(value.spawnCard as CharacterSpawnCard).noElites)
				{
					num2 = (int)((float)num2 * CalcHighestEliteCostMultiplier(value.spawnCard.eliteRules));
				}
				num = Mathf.Max(num, num2);
			}
			return num;
		}
	}

	private void Awake()
	{
		if (NetworkServer.active)
		{
			rng = new Xoroshiro128Plus(Run.instance.stageRng.nextUint);
			moneyWaves = new DirectorMoneyWave[moneyWaveIntervals.Length];
			for (int i = 0; i < moneyWaveIntervals.Length; i++)
			{
				moneyWaves[i] = new DirectorMoneyWave
				{
					interval = rng.RangeFloat(moneyWaveIntervals[i].min, moneyWaveIntervals[i].max),
					multiplier = creditMultiplier
				};
			}
			monsterCardsSelection = monsterCards?.GenerateDirectorCardWeightedSelection();
		}
	}

	private void OnEnable()
	{
		instancesList.Add(this);
	}

	private void OnDisable()
	{
		instancesList.Remove(this);
		if (NetworkServer.active && instancesList.Count > 0)
		{
			float num = 0.4f;
			CombatDirector combatDirector = rng.NextElementUniform(instancesList);
			monsterCredit *= num;
			combatDirector.monsterCredit += monsterCredit;
			Debug.LogFormat("Transfered {0} monster credits from {1} to {2}", monsterCredit, base.gameObject, combatDirector.gameObject);
			monsterCredit = 0f;
		}
	}

	private void GenerateAmbush(Vector3 victimPosition)
	{
		NodeGraph groundNodes = SceneInfo.instance.groundNodes;
		NodeGraph.NodeIndex nodeIndex = groundNodes.FindClosestNode(victimPosition, HullClassification.Human);
		NodeGraphSpider nodeGraphSpider = new NodeGraphSpider(groundNodes, HullMask.Human);
		nodeGraphSpider.AddNodeForNextStep(nodeIndex);
		List<NodeGraphSpider.StepInfo> list = new List<NodeGraphSpider.StepInfo>();
		int num = 0;
		List<NodeGraphSpider.StepInfo> collectedSteps = nodeGraphSpider.collectedSteps;
		while (nodeGraphSpider.PerformStep() && num < 8)
		{
			num++;
			for (int i = 0; i < collectedSteps.Count; i++)
			{
				if (IsAcceptableAmbushSpiderStep(groundNodes, nodeIndex, collectedSteps[i]))
				{
					list.Add(collectedSteps[i]);
				}
			}
			collectedSteps.Clear();
		}
		for (int j = 0; j < list.Count; j++)
		{
			groundNodes.GetNodePosition(list[j].node, out var position);
			LegacyResourcesAPI.Load<SpawnCard>("SpawnCards/scLemurian").DoSpawn(position, Quaternion.identity, null);
		}
	}

	private static bool IsAcceptableAmbushSpiderStep(NodeGraph nodeGraph, NodeGraph.NodeIndex startNode, NodeGraphSpider.StepInfo stepInfo)
	{
		int num = 0;
		while (stepInfo.previousStep != null)
		{
			if (nodeGraph.TestNodeLineOfSight(startNode, stepInfo.node))
			{
				return false;
			}
			stepInfo = stepInfo.previousStep;
			num++;
			if (num > 2)
			{
				return true;
			}
		}
		return false;
	}

	public void OverrideCurrentMonsterCard(DirectorCard overrideMonsterCard)
	{
		PrepareNewMonsterWave(overrideMonsterCard);
	}

	public void SetNextSpawnAsBoss()
	{
		WeightedSelection<DirectorCard> weightedSelection = new WeightedSelection<DirectorCard>();
		int i = 0;
		for (int count = finalMonsterCardsSelection.Count; i < count; i++)
		{
			WeightedSelection<DirectorCard>.ChoiceInfo choice = finalMonsterCardsSelection.GetChoice(i);
			SpawnCard spawnCard = choice.value.spawnCard;
			bool isChampion = spawnCard.prefab.GetComponent<CharacterMaster>().bodyPrefab.GetComponent<CharacterBody>().isChampion;
			bool flag = (spawnCard as CharacterSpawnCard)?.forbiddenAsBoss ?? false;
			if (isChampion && !flag && choice.value.IsAvailable())
			{
				weightedSelection.AddChoice(choice);
			}
		}
		if (weightedSelection.Count > 0)
		{
			DirectorCard monsterCard = weightedSelection.Evaluate(rng.nextNormalizedFloat);
			PrepareNewMonsterWave(monsterCard);
		}
		monsterSpawnTimer = -600f;
	}

	private void PickPlayerAsSpawnTarget()
	{
		ReadOnlyCollection<PlayerCharacterMasterController> instances = PlayerCharacterMasterController.instances;
		List<PlayerCharacterMasterController> list = new List<PlayerCharacterMasterController>();
		foreach (PlayerCharacterMasterController item in instances)
		{
			if (item.master.hasBody)
			{
				list.Add(item);
			}
		}
		if (list.Count > 0)
		{
			currentSpawnTarget = rng.NextElementUniform(list).master.GetBodyObject();
		}
	}

	public void SpendAllCreditsOnMapSpawns(Transform mapSpawnTarget)
	{
		int num = 0;
		int num2 = 10;
		while (monsterCredit > 0f)
		{
			PrepareNewMonsterWave(finalMonsterCardsSelection.Evaluate(rng.nextNormalizedFloat));
			bool flag = false;
			if ((!mapSpawnTarget) ? AttemptSpawnOnTarget(null, SceneInfo.instance.approximateMapBoundMesh ? DirectorPlacementRule.PlacementMode.RandomNormalized : DirectorPlacementRule.PlacementMode.Random) : AttemptSpawnOnTarget(mapSpawnTarget))
			{
				num = 0;
				continue;
			}
			num++;
			if (num < num2)
			{
				continue;
			}
			break;
		}
	}

	public void ToggleAllCombatDirectorsOnThisObject(bool newValue)
	{
		CombatDirector[] components = GetComponents<CombatDirector>();
		for (int i = 0; i < components.Length; i++)
		{
			components[i].enabled = newValue;
		}
	}

	private void Simulate(float deltaTime)
	{
		if (targetPlayers)
		{
			playerRetargetTimer -= deltaTime;
			if (playerRetargetTimer <= 0f)
			{
				playerRetargetTimer = rng.RangeFloat(1f, 10f);
				PickPlayerAsSpawnTarget();
			}
		}
		monsterSpawnTimer -= deltaTime;
		if (!(monsterSpawnTimer <= 0f))
		{
			return;
		}
		if (AttemptSpawnOnTarget(currentSpawnTarget ? currentSpawnTarget.transform : null))
		{
			if (shouldSpawnOneWave)
			{
				hasStartedWave = true;
			}
			monsterSpawnTimer += rng.RangeFloat(minSeriesSpawnInterval, maxSeriesSpawnInterval);
			return;
		}
		monsterSpawnTimer += rng.RangeFloat(minRerollSpawnInterval, maxRerollSpawnInterval);
		if (resetMonsterCardIfFailed)
		{
			currentMonsterCard = null;
		}
		if (shouldSpawnOneWave && hasStartedWave)
		{
			base.enabled = false;
		}
	}

	public static bool IsEliteOnlyArtifactActive()
	{
		return RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.eliteOnlyArtifactDef);
	}

	private static bool NotEliteOnlyArtifactActive()
	{
		return !IsEliteOnlyArtifactActive();
	}

	[SystemInitializer(new Type[] { typeof(EliteCatalog) })]
	private static void Init()
	{
		eliteTiers = new EliteTierDef[6]
		{
			new EliteTierDef
			{
				costMultiplier = 1f,
				eliteTypes = new EliteDef[1],
				isAvailable = (SpawnCard.EliteRules rules) => NotEliteOnlyArtifactActive(),
				canSelectWithoutAvailableEliteDef = true
			},
			new EliteTierDef
			{
				costMultiplier = baseEliteCostMultiplier,
				eliteTypes = new EliteDef[4]
				{
					RoR2Content.Elites.Lightning,
					RoR2Content.Elites.Ice,
					RoR2Content.Elites.Fire,
					DLC1Content.Elites.Earth
				},
				isAvailable = (SpawnCard.EliteRules rules) => NotEliteOnlyArtifactActive() && rules == SpawnCard.EliteRules.Default,
				canSelectWithoutAvailableEliteDef = false
			},
			new EliteTierDef
			{
				costMultiplier = Mathf.LerpUnclamped(1f, baseEliteCostMultiplier, 0.5f),
				eliteTypes = new EliteDef[4]
				{
					RoR2Content.Elites.LightningHonor,
					RoR2Content.Elites.IceHonor,
					RoR2Content.Elites.FireHonor,
					DLC1Content.Elites.EarthHonor
				},
				isAvailable = (SpawnCard.EliteRules rules) => IsEliteOnlyArtifactActive(),
				canSelectWithoutAvailableEliteDef = false
			},
			new EliteTierDef
			{
				costMultiplier = Mathf.LerpUnclamped(1f, baseEliteCostMultiplier, 0.5f),
				eliteTypes = new EliteDef[5]
				{
					RoR2Content.Elites.Lightning,
					RoR2Content.Elites.Ice,
					RoR2Content.Elites.Fire,
					DLC1Content.Elites.Earth,
					DLC2Content.Elites.Aurelionite
				},
				isAvailable = (SpawnCard.EliteRules rules) => NotEliteOnlyArtifactActive() && rules == SpawnCard.EliteRules.Default && Run.instance.stageClearCount >= 2,
				canSelectWithoutAvailableEliteDef = false
			},
			new EliteTierDef
			{
				costMultiplier = baseEliteCostMultiplier * 6f,
				eliteTypes = new EliteDef[3]
				{
					RoR2Content.Elites.Poison,
					RoR2Content.Elites.Haunted,
					DLC2Content.Elites.Bead
				},
				isAvailable = (SpawnCard.EliteRules rules) => Run.instance.loopClearCount > 0 && rules == SpawnCard.EliteRules.Default,
				canSelectWithoutAvailableEliteDef = false
			},
			new EliteTierDef
			{
				costMultiplier = baseEliteCostMultiplier,
				eliteTypes = new EliteDef[1] { RoR2Content.Elites.Lunar },
				isAvailable = (SpawnCard.EliteRules rules) => rules == SpawnCard.EliteRules.Lunar,
				canSelectWithoutAvailableEliteDef = false
			}
		};
	}

	public static float CalcHighestEliteCostMultiplier(SpawnCard.EliteRules eliteRules)
	{
		float num = 1f;
		for (int i = 1; i < eliteTiers.Length; i++)
		{
			if (eliteTiers[i].CanSelect(eliteRules))
			{
				num = Mathf.Max(num, eliteTiers[i].costMultiplier);
			}
		}
		return num;
	}

	private void ResetEliteType()
	{
		currentActiveEliteTier = eliteTiers[0];
		for (int i = 0; i < eliteTiers.Length; i++)
		{
			if (eliteTiers[i].CanSelect(currentMonsterCard.spawnCard.eliteRules))
			{
				currentActiveEliteTier = eliteTiers[i];
				break;
			}
		}
		currentActiveEliteDef = currentActiveEliteTier.GetRandomAvailableEliteDef(rng);
	}

	private void PrepareNewMonsterWave(DirectorCard monsterCard)
	{
		if (cvDirectorCombatEnableInternalLogs.value)
		{
			Debug.LogFormat("Preparing monster wave {0}", monsterCard.spawnCard);
		}
		currentMonsterCard = monsterCard;
		ResetEliteType();
		if (!(currentMonsterCard.spawnCard as CharacterSpawnCard).noElites)
		{
			for (int i = 1; i < eliteTiers.Length; i++)
			{
				EliteTierDef eliteTierDef = eliteTiers[i];
				if (!eliteTierDef.CanSelect(currentMonsterCard.spawnCard.eliteRules))
				{
					if (cvDirectorCombatEnableInternalLogs.value)
					{
						Debug.LogFormat("Elite tier index {0} is unavailable", i);
					}
					continue;
				}
				float num = (float)currentMonsterCard.cost * eliteTierDef.costMultiplier * eliteBias;
				if (num <= monsterCredit)
				{
					currentActiveEliteTier = eliteTierDef;
					if (cvDirectorCombatEnableInternalLogs.value)
					{
						Debug.LogFormat("Found valid elite tier index {0}", i);
					}
				}
				else if (cvDirectorCombatEnableInternalLogs.value)
				{
					Debug.LogFormat("Elite tier index {0} is too expensive ({1}/{2})", i, num, monsterCredit);
				}
			}
		}
		else if (cvDirectorCombatEnableInternalLogs.value)
		{
			Debug.LogFormat("Card {0} cannot be elite. Skipping elite procedure.", currentMonsterCard.spawnCard);
		}
		currentActiveEliteDef = currentActiveEliteTier.GetRandomAvailableEliteDef(rng);
		if (cvDirectorCombatEnableInternalLogs.value)
		{
			Debug.LogFormat("Assigned elite index {0}", currentActiveEliteDef);
		}
		lastAttemptedMonsterCard = currentMonsterCard;
		spawnCountInCurrentWave = 0;
	}

	private bool AttemptSpawnOnTarget(Transform spawnTarget, DirectorPlacementRule.PlacementMode placementMode = DirectorPlacementRule.PlacementMode.Approximate)
	{
		if (currentMonsterCard == null)
		{
			_ = cvDirectorCombatEnableInternalLogs.value;
			if (finalMonsterCardsSelection == null)
			{
				return false;
			}
			PrepareNewMonsterWave(finalMonsterCardsSelection.Evaluate(rng.nextNormalizedFloat));
		}
		if (spawnCountInCurrentWave >= maximumNumberToSpawnBeforeSkipping)
		{
			spawnCountInCurrentWave = 0;
			if (cvDirectorCombatEnableInternalLogs.value)
			{
				Debug.LogFormat("Spawn count has hit the max ({0}/{1}). Aborting spawn.", spawnCountInCurrentWave, maximumNumberToSpawnBeforeSkipping);
			}
			return false;
		}
		int num = currentMonsterCard.cost;
		int cost = currentMonsterCard.cost;
		float num2 = 1f;
		EliteDef eliteDef = currentActiveEliteDef;
		cost = (int)((float)num * currentActiveEliteTier.costMultiplier);
		if ((float)cost <= monsterCredit)
		{
			num = cost;
			num2 = currentActiveEliteTier.costMultiplier;
		}
		else
		{
			ResetEliteType();
			eliteDef = currentActiveEliteDef;
		}
		if (!currentMonsterCard.IsAvailable())
		{
			if (cvDirectorCombatEnableInternalLogs.value)
			{
				Debug.LogFormat("Spawn card {0} is invalid, aborting spawn.", currentMonsterCard.spawnCard);
			}
			return false;
		}
		if (monsterCredit < (float)num)
		{
			if (cvDirectorCombatEnableInternalLogs.value)
			{
				Debug.LogFormat("Spawn card {0} is too expensive, aborting spawn.", currentMonsterCard.spawnCard);
			}
			return false;
		}
		if (skipSpawnIfTooCheap && consecutiveCheapSkips < maxConsecutiveCheapSkips && (float)(cost * maximumNumberToSpawnBeforeSkipping) < monsterCredit)
		{
			if (cvDirectorCombatEnableInternalLogs.value)
			{
				Debug.LogFormat("Card {0} seems too cheap ({1}/{2}). Comparing against most expensive possible ({3})", currentMonsterCard.spawnCard, num * maximumNumberToSpawnBeforeSkipping, monsterCredit, mostExpensiveMonsterCostInDeck);
			}
			if (mostExpensiveMonsterCostInDeck > num)
			{
				consecutiveCheapSkips++;
				if (cvDirectorCombatEnableInternalLogs.value)
				{
					Debug.LogFormat("Spawn card {0} is too cheap, aborting spawn.", currentMonsterCard.spawnCard);
				}
				return false;
			}
		}
		SpawnCard spawnCard = currentMonsterCard.spawnCard;
		if (Spawn(spawnCard, eliteDef, spawnTarget, valueMultiplier: num2, placementMode: placementMode, preventOverhead: currentMonsterCard.preventOverhead, spawnDistance: currentMonsterCard.spawnDistance))
		{
			monsterCredit -= num;
			totalCreditsSpent += num;
			spawnCountInCurrentWave++;
			consecutiveCheapSkips = 0;
			return true;
		}
		return false;
	}

	public bool Spawn(SpawnCard spawnCard, EliteDef eliteDef, Transform spawnTarget, DirectorCore.MonsterSpawnDistance spawnDistance, bool preventOverhead, float valueMultiplier = 1f, DirectorPlacementRule.PlacementMode placementMode = DirectorPlacementRule.PlacementMode.Approximate)
	{
		DirectorPlacementRule directorPlacementRule = new DirectorPlacementRule
		{
			placementMode = placementMode,
			spawnOnTarget = spawnTarget,
			preventOverhead = preventOverhead
		};
		DirectorCore.GetMonsterSpawnDistance(spawnDistance, out directorPlacementRule.minDistance, out directorPlacementRule.maxDistance);
		directorPlacementRule.maxDistance = Mathf.Min(maxSpawnDistance, directorPlacementRule.maxDistance * spawnDistanceMultiplier);
		directorPlacementRule.minDistance = Mathf.Max(0f, Mathf.Min(directorPlacementRule.maxDistance - minSpawnRange, directorPlacementRule.minDistance * spawnDistanceMultiplier));
		DirectorSpawnRequest directorSpawnRequest = new DirectorSpawnRequest(spawnCard, directorPlacementRule, rng);
		directorSpawnRequest.ignoreTeamMemberLimit = ignoreTeamSizeLimit;
		directorSpawnRequest.teamIndexOverride = teamIndex;
		directorSpawnRequest.onSpawnedServer = OnCardSpawned;
		if (!DirectorCore.instance.TrySpawnObject(directorSpawnRequest))
		{
			Debug.LogFormat("Spawn card {0} failed to spawn. Aborting cost procedures.", spawnCard);
			return false;
		}
		return true;
		void OnCardSpawned(SpawnCard.SpawnResult result)
		{
			if (result.success)
			{
				float num = 1f;
				float num2 = 1f;
				CharacterMaster component = result.spawnedInstance.GetComponent<CharacterMaster>();
				GameObject bodyObject = component.GetBodyObject();
				CharacterBody component2 = bodyObject.GetComponent<CharacterBody>();
				if ((bool)component2)
				{
					component2.cost = valueMultiplier * (float)spawnCard.directorCreditCost;
				}
				if ((bool)combatSquad)
				{
					combatSquad.AddMember(component);
				}
				num = eliteDef?.healthBoostCoefficient ?? 1f;
				num2 = eliteDef?.damageBoostCoefficient ?? 1f;
				if (isHalcyonShrineSpawn)
				{
					if (shrineHalcyoniteDifficultyLevel > 20)
					{
						shrineHalcyoniteDifficultyLevel = 20 + shrineHalcyoniteDifficultyLevel / 100;
					}
					num += (float)shrineHalcyoniteDifficultyLevel * 0.6f;
					num2 += (float)shrineHalcyoniteDifficultyLevel * 0.25f;
					eliteDef = DLC2Content.Elites.Aurelionite;
				}
				EquipmentIndex equipmentIndex = eliteDef?.eliteEquipmentDef?.equipmentIndex ?? EquipmentIndex.None;
				if (equipmentIndex != EquipmentIndex.None)
				{
					component.inventory.SetEquipmentIndex(equipmentIndex);
				}
				if ((bool)combatSquad && combatSquad.grantBonusHealthInMultiplayer)
				{
					int livingPlayerCount = Run.instance.livingPlayerCount;
					num *= Mathf.Pow(livingPlayerCount, 1f);
				}
				component.inventory.GiveItem(RoR2Content.Items.BoostHp, Mathf.RoundToInt((num - 1f) * 10f));
				component.inventory.GiveItem(RoR2Content.Items.BoostDamage, Mathf.RoundToInt((num2 - 1f) * 10f));
				DeathRewards component3 = bodyObject.GetComponent<DeathRewards>();
				if ((bool)component3)
				{
					float num3 = (float)spawnCard.directorCreditCost * valueMultiplier * expRewardCoefficient;
					component3.spawnValue = (int)Mathf.Max(1f, num3);
					if (num3 > Mathf.Epsilon)
					{
						component3.expReward = (uint)Mathf.Max(1f, num3 * Run.instance.compensatedDifficultyCoefficient);
						component3.goldReward = (uint)Mathf.Max(1f, num3 * goldRewardCoefficient * 2f * Run.instance.compensatedDifficultyCoefficient);
					}
					else
					{
						component3.expReward = 0u;
						component3.goldReward = 0u;
					}
				}
				if ((bool)spawnEffectPrefab && NetworkServer.active)
				{
					Vector3 origin = result.position;
					CharacterBody characterBody = component2;
					if ((bool)characterBody)
					{
						origin = characterBody.corePosition;
					}
					EffectManager.SpawnEffect(spawnEffectPrefab, new EffectData
					{
						origin = origin
					}, transmit: true);
				}
				onSpawnedServer?.Invoke(result.spawnedInstance);
			}
		}
	}

	private void FixedUpdate()
	{
		if (!cvDirectorCombatDisable.value && NetworkServer.active && (bool)Run.instance)
		{
			float compensatedDifficultyCoefficient = Run.instance.compensatedDifficultyCoefficient;
			for (int i = 0; i < moneyWaves.Length; i++)
			{
				float num = moneyWaves[i].Update(Time.fixedDeltaTime, compensatedDifficultyCoefficient);
				monsterCredit += num;
			}
			Simulate(Time.fixedDeltaTime);
		}
	}

	public void CombatShrineActivation(Interactor interactor, float monsterCredit, DirectorCard chosenDirectorCard)
	{
		base.enabled = true;
		this.monsterCredit += monsterCredit;
		OverrideCurrentMonsterCard(chosenDirectorCard);
		monsterSpawnTimer = 0f;
		CharacterMaster component = chosenDirectorCard.spawnCard.prefab.GetComponent<CharacterMaster>();
		if ((bool)component)
		{
			CharacterBody component2 = component.bodyPrefab.GetComponent<CharacterBody>();
			if ((bool)component2)
			{
				Chat.SubjectFormatChatMessage subjectFormatChatMessage = new Chat.SubjectFormatChatMessage();
				subjectFormatChatMessage.subjectAsCharacterBody = interactor.GetComponent<CharacterBody>();
				subjectFormatChatMessage.baseToken = "SHRINE_COMBAT_USE_MESSAGE";
				subjectFormatChatMessage.paramTokens = new string[1] { component2.baseNameToken };
				Chat.SendBroadcastChat(subjectFormatChatMessage);
			}
		}
	}

	public DirectorCard SelectMonsterCardForCombatShrine(float monsterCredit)
	{
		WeightedSelection<DirectorCard> weightedSelection = Util.CreateReasonableDirectorCardSpawnList(monsterCredit, maximumNumberToSpawnBeforeSkipping, 1);
		if (weightedSelection.Count == 0)
		{
			return null;
		}
		return weightedSelection.Evaluate(rng.nextNormalizedFloat);
	}

	public void HalcyoniteShrineActivation(float monsterCredit, DirectorCard chosenDirectorCard, int difficultyLevel, Transform shrineTransform)
	{
		base.enabled = true;
		this.monsterCredit += monsterCredit;
		isHalcyonShrineSpawn = true;
		shrineHalcyoniteDifficultyLevel = difficultyLevel;
		OverrideCurrentMonsterCard(chosenDirectorCard);
		monsterSpawnTimer = 0f;
		Util.PlaySound("Play_halcyonite_spawn", shrineTransform.gameObject);
	}

	public void SetMonsterCredit(float newMonsterCredit)
	{
		monsterCredit = newMonsterCredit;
	}
}
