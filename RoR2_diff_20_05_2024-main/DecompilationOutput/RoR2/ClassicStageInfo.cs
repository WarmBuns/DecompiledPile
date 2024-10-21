using System;
using System.Collections;
using HG;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(SceneInfo))]
public class ClassicStageInfo : MonoBehaviour
{
	[Serializable]
	public struct BonusInteractibleCreditObject
	{
		public GameObject objectThatGrantsPointsIfEnabled;

		public int points;
	}

	[Serializable]
	public struct MonsterFamily
	{
		[SerializeField]
		public DirectorCardCategorySelection monsterFamilyCategories;

		public string familySelectionChatString;

		public float selectionWeight;

		public int minimumStageCompletion;

		public int maximumStageCompletion;
	}

	private DirectorCardCategorySelection modifiableMonsterCategories;

	[SerializeField]
	[Tooltip("We'll select a single DCCS from this pool when we enter the stage to determine which monsters can spawn.")]
	private DccsPool monsterDccsPool;

	[SerializeField]
	[Tooltip("We'll select a single DCCS from this pool when we enter the stage to determine which interactables can spawn.")]
	private DccsPool interactableDccsPool;

	private ulong seedServer;

	private Xoroshiro128Plus rng;

	public int sceneDirectorInteractibleCredits = 200;

	public int sceneDirectorMonsterCredits = 20;

	public BonusInteractibleCreditObject[] bonusInteractibleCreditObjects;

	public static float monsterFamilyChance = 0.02f;

	[HideInInspector]
	[SerializeField]
	private DirectorCard[] monsterCards;

	[HideInInspector]
	[SerializeField]
	public DirectorCard[] interactableCards;

	[CanBeNull]
	[ShowFieldObsolete]
	[Tooltip("Deprecated.  Use MonsterDccsPool instead.")]
	public DirectorCardCategorySelection interactableCategories;

	[CanBeNull]
	[SerializeField]
	[ShowFieldObsolete]
	[Tooltip("Deprecated.  Use MonsterDccsPool instead.")]
	private DirectorCardCategorySelection monsterCategories;

	[ShowFieldObsolete]
	[Tooltip("Deprecated.  Use MonsterDccsPool instead.")]
	public MonsterFamily[] possibleMonsterFamilies;

	public WeightedSelection<DirectorCard> monsterSelection { get; private set; }

	public static ClassicStageInfo instance { get; private set; }

	internal DccsPool GetMonsterDccsPool => monsterDccsPool;

	internal DccsPool GetInteractableDccsPool => interactableDccsPool;

	private static void HandleSingleMonsterTypeArtifact(DirectorCardCategorySelection monsterCategories, Xoroshiro128Plus rng)
	{
		ScriptableObject.CreateInstance<DirectorCardCategorySelection>().CopyFrom(monsterCategories);
		float baseCredits = 40f * Run.instance.difficultyCoefficient;
		int maximumNumberToSpawnBeforeSkipping = 5;
		int minimumToSpawn = 1;
		if (SceneCatalog.mostRecentSceneDef == SceneCatalog.FindSceneDef("arena"))
		{
			baseCredits = 50f * Run.instance.difficultyCoefficient;
			maximumNumberToSpawnBeforeSkipping = 6;
			minimumToSpawn = 2;
		}
		WeightedSelection<DirectorCard> candidatesSelection = new WeightedSelection<DirectorCard>();
		AddCardsWhichPassCondition(CardIsAffordable);
		if (candidatesSelection.Count == 0)
		{
			AddCardsWhichPassCondition(ReturnTrue);
		}
		if (candidatesSelection.Count == 0)
		{
			Debug.LogWarning("Could not collapse director card selection down to one, no cards passed the filters!");
			return;
		}
		DirectorCard directorCard = candidatesSelection.Evaluate(rng.nextNormalizedFloat);
		monsterCategories.Clear();
		int categoryIndex = monsterCategories.AddCategory("Basic Monsters", 1f);
		monsterCategories.AddCard(categoryIndex, directorCard);
		BodyIndex bodyIndex = directorCard.spawnCard.prefab.GetComponent<CharacterMaster>().bodyPrefab.GetComponent<CharacterBody>().bodyIndex;
		if ((bool)Stage.instance)
		{
			SetStageSingleMonsterType(Stage.instance);
		}
		else
		{
			Stage.onServerStageBegin += SetStageSingleMonsterType;
		}
		void AddCardsWhichPassCondition(Predicate<DirectorCard> predicate)
		{
			for (int i = 0; i < monsterCategories.categories.Length; i++)
			{
				ref DirectorCardCategorySelection.Category reference = ref monsterCategories.categories[i];
				DirectorCard[] cards = reference.cards;
				float selectionWeight = reference.selectionWeight;
				for (int j = 0; j < cards.Length; j++)
				{
					DirectorCard directorCard2 = cards[j];
					if (predicate(directorCard2))
					{
						candidatesSelection.AddChoice(cards[j], selectionWeight * (float)directorCard2.selectionWeight);
					}
				}
			}
		}
		bool CardIsAffordable(DirectorCard card)
		{
			return Util.DirectorCardIsReasonableChoice(baseCredits, maximumNumberToSpawnBeforeSkipping, minimumToSpawn, card, CombatDirector.CalcHighestEliteCostMultiplier(card.spawnCard.eliteRules));
		}
		static bool ReturnTrue(DirectorCard card)
		{
			return true;
		}
		void SetStageSingleMonsterType(Stage stage)
		{
			Stage.instance.singleMonsterTypeBodyIndex = bodyIndex;
			Stage.onServerStageBegin -= SetStageSingleMonsterType;
		}
	}

	private static void HandleMixEnemyArtifact(DirectorCardCategorySelection monsterCategories, Xoroshiro128Plus rng)
	{
		monsterCategories.CopyFrom(RoR2Content.mixEnemyMonsterCards);
		if (monsterCategories.categories.Length == 0)
		{
			Debug.LogError("MixEnemy monster cards are size 0!");
		}
		TrimCategory("Basic Monsters", 3);
		TrimCategory("Minibosses", 3);
		TrimCategory("Champions", 3);
		void TrimCategory(string categoryName, int requiredCount)
		{
			DirectorCardCategorySelection.Category[] categories = monsterCategories.categories;
			for (int i = 0; i < categories.Length; i++)
			{
				if (string.CompareOrdinal(categoryName, categories[i].name) == 0)
				{
					Debug.LogFormat("Trimming {0} from {1} to {2}", categoryName, categories[i].cards.Length, requiredCount);
					TrimSelection(ref categories[i].cards, requiredCount);
				}
			}
		}
		void TrimSelection(ref DirectorCard[] cards, int requiredCount)
		{
			if (cards.Length > requiredCount)
			{
				DirectorCard[] array = ArrayUtils.Clone(cards);
				Util.ShuffleArray(array, rng);
				int num = array.Length - 1;
				while (num >= 0 && array.Length > requiredCount)
				{
					if (!array[num].IsAvailable())
					{
						ArrayUtils.ArrayRemoveAtAndResize(ref array, num);
					}
					num--;
				}
				if (array.Length > requiredCount)
				{
					Array.Resize(ref array, requiredCount);
				}
				cards = array;
				DirectorCard[] array2 = cards;
				foreach (DirectorCard directorCard in array2)
				{
					Debug.LogFormat("Selected {0}", directorCard.spawnCard.name);
				}
			}
		}
	}

	private static bool DirectorCardDoesNotForbidElite(DirectorCard directorCard)
	{
		CharacterSpawnCard characterSpawnCard = directorCard.spawnCard as CharacterSpawnCard;
		if (!characterSpawnCard)
		{
			return true;
		}
		return !characterSpawnCard.noElites;
	}

	private void Awake()
	{
		if (NetworkServer.active)
		{
			seedServer = Run.instance.stageRng.nextUlong;
			rng = new Xoroshiro128Plus(seedServer);
		}
	}

	private void Start()
	{
		RebuildCards();
		RunArtifactManager.onArtifactEnabledGlobal += OnArtifactEnabled;
		RunArtifactManager.onArtifactDisabledGlobal += OnArtifactDisabled;
	}

	private void OnDestroy()
	{
		RunArtifactManager.onArtifactEnabledGlobal -= OnArtifactEnabled;
		RunArtifactManager.onArtifactDisabledGlobal -= OnArtifactDisabled;
		if ((bool)modifiableMonsterCategories)
		{
			UnityEngine.Object.Destroy(modifiableMonsterCategories);
		}
	}

	public IEnumerator BroadcastFamilySelection(string familySelectionChatString)
	{
		yield return new WaitForSeconds(1f);
		Chat.SendBroadcastChat(new Chat.SimpleChatMessage
		{
			baseToken = familySelectionChatString
		});
	}

	private void OnEnable()
	{
		instance = this;
	}

	private void OnDisable()
	{
		instance = null;
	}

	private static float CalculateTotalWeight(DirectorCard[] cards)
	{
		float num = 0f;
		foreach (DirectorCard directorCard in cards)
		{
			num += (float)directorCard.selectionWeight;
		}
		return num;
	}

	internal void RebuildCards(DirectorCardCategorySelection forcedMonsterCategory = null, DirectorCardCategorySelection forcedInteractableCategory = null)
	{
		Xoroshiro128Plus xoroshiro128Plus = new Xoroshiro128Plus(seedServer);
		Xoroshiro128Plus xoroshiro128Plus2 = new Xoroshiro128Plus(xoroshiro128Plus.nextUlong);
		Xoroshiro128Plus xoroshiro128Plus3 = new Xoroshiro128Plus(xoroshiro128Plus.nextUlong);
		Xoroshiro128Plus xoroshiro128Plus4 = new Xoroshiro128Plus(xoroshiro128Plus.nextUlong);
		Xoroshiro128Plus xoroshiro128Plus5 = new Xoroshiro128Plus(xoroshiro128Plus.nextUlong);
		if (forcedInteractableCategory != null)
		{
			forcedInteractableCategory.OnSelected(this);
			interactableCategories = forcedInteractableCategory;
		}
		else if ((bool)interactableDccsPool)
		{
			DirectorCardCategorySelection directorCardCategorySelection = interactableDccsPool.GenerateWeightedSelection().Evaluate(xoroshiro128Plus5.nextNormalizedFloat);
			if (directorCardCategorySelection != null)
			{
				directorCardCategorySelection.OnSelected(this);
				interactableCategories = directorCardCategorySelection;
			}
		}
		DirectorCardCategorySelection directorCardCategorySelection2 = null;
		if ((bool)monsterDccsPool)
		{
			DirectorCardCategorySelection directorCardCategorySelection3 = monsterDccsPool.GenerateWeightedSelection().Evaluate(xoroshiro128Plus5.nextNormalizedFloat);
			if (directorCardCategorySelection3 != null)
			{
				directorCardCategorySelection3.OnSelected(this);
				directorCardCategorySelection2 = UnityEngine.Object.Instantiate(directorCardCategorySelection3);
			}
		}
		else if ((bool)monsterCategories)
		{
			directorCardCategorySelection2 = UnityEngine.Object.Instantiate(monsterCategories);
		}
		if (!directorCardCategorySelection2)
		{
			return;
		}
		UnityEngine.Object.Destroy(modifiableMonsterCategories);
		bool flag = RunArtifactManager.instance?.IsArtifactEnabled(RoR2Content.Artifacts.singleMonsterTypeArtifactDef) ?? false;
		bool num = RunArtifactManager.instance?.IsArtifactEnabled(RoR2Content.Artifacts.mixEnemyArtifactDef) ?? false;
		bool flag2 = RunArtifactManager.instance?.IsArtifactEnabled(RoR2Content.Artifacts.eliteOnlyArtifactDef) ?? false;
		modifiableMonsterCategories = directorCardCategorySelection2;
		if (num)
		{
			HandleMixEnemyArtifact(modifiableMonsterCategories, xoroshiro128Plus3);
		}
		else if (forcedMonsterCategory != null)
		{
			forcedMonsterCategory.OnSelected(this);
			modifiableMonsterCategories.CopyFrom(forcedMonsterCategory);
		}
		else if (!monsterDccsPool && xoroshiro128Plus4.nextNormalizedFloat <= monsterFamilyChance && possibleMonsterFamilies != null)
		{
			Run run = Run.instance;
			if ((object)run == null || run.canFamilyEventTrigger)
			{
				WeightedSelection<MonsterFamily> weightedSelection = new WeightedSelection<MonsterFamily>();
				for (int i = 0; i < possibleMonsterFamilies.Length; i++)
				{
					MonsterFamily value = possibleMonsterFamilies[i];
					if (Run.instance != null && value.minimumStageCompletion <= Run.instance.stageClearCount && value.maximumStageCompletion > Run.instance.stageClearCount)
					{
						weightedSelection.AddChoice(value, value.selectionWeight);
					}
				}
				if (weightedSelection.Count > 0)
				{
					MonsterFamily monsterFamily = weightedSelection.Evaluate(xoroshiro128Plus.nextNormalizedFloat);
					modifiableMonsterCategories.CopyFrom(monsterFamily.monsterFamilyCategories);
					StartCoroutine("BroadcastFamilySelection", monsterFamily.familySelectionChatString);
				}
			}
		}
		if (flag2)
		{
			modifiableMonsterCategories.RemoveCardsThatFailFilter(DirectorCardDoesNotForbidElite);
		}
		if (flag)
		{
			HandleSingleMonsterTypeArtifact(modifiableMonsterCategories, xoroshiro128Plus2);
		}
		monsterSelection = modifiableMonsterCategories.GenerateDirectorCardWeightedSelection();
	}

	private void OnArtifactDisabled([NotNull] RunArtifactManager runArtifactManager, [NotNull] ArtifactDef artifactDef)
	{
		if ((object)artifactDef == RoR2Content.Artifacts.mixEnemyArtifactDef || (object)artifactDef == RoR2Content.Artifacts.singleMonsterTypeArtifactDef)
		{
			RebuildCards();
		}
	}

	private void OnArtifactEnabled([NotNull] RunArtifactManager runArtifactManager, [NotNull] ArtifactDef artifactDef)
	{
		if ((object)artifactDef == RoR2Content.Artifacts.mixEnemyArtifactDef || (object)artifactDef == RoR2Content.Artifacts.singleMonsterTypeArtifactDef)
		{
			RebuildCards();
		}
	}
}
