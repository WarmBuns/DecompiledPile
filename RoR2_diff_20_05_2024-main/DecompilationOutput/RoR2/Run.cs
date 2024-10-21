using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using HG;
using JetBrains.Annotations;
using RoR2.ConVar;
using RoR2.ExpansionManagement;
using RoR2.Navigation;
using RoR2.Networking;
using RoR2.Stats;
using Unity;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkRuleBook))]
[RequireComponent(typeof(RunArtifactManager))]
public class Run : NetworkBehaviour
{
	[Serializable]
	public struct RunStopwatch : IEquatable<RunStopwatch>
	{
		public float offsetFromFixedTime;

		public bool isPaused;

		public bool Equals(RunStopwatch other)
		{
			if (offsetFromFixedTime.Equals(other.offsetFromFixedTime))
			{
				return isPaused == other.isPaused;
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			if (obj is RunStopwatch other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (offsetFromFixedTime.GetHashCode() * 397) ^ isPaused.GetHashCode();
		}
	}

	[Serializable]
	public struct TimeStamp : IEquatable<TimeStamp>, IComparable<TimeStamp>
	{
		public readonly float t;

		private static float tNow;

		public static readonly TimeStamp zero;

		public static readonly TimeStamp positiveInfinity;

		public static readonly TimeStamp negativeInfinity;

		public float timeUntil => t - tNow;

		public float timeSince => tNow - t;

		public float timeUntilClamped => Mathf.Max(timeUntil, 0f);

		public float timeSinceClamped => Mathf.Max(timeSince, 0f);

		public bool hasPassed => t <= tNow;

		public bool isInfinity => float.IsInfinity(t);

		public bool isPositiveInfinity => float.IsPositiveInfinity(t);

		public bool isNegativeInfinity => float.IsNegativeInfinity(t);

		public static TimeStamp now => new TimeStamp(tNow);

		public override int GetHashCode()
		{
			return t.GetHashCode();
		}

		public static void Update()
		{
			tNow = instance.time;
		}

		private TimeStamp(float t)
		{
			this.t = t;
		}

		public bool Equals(TimeStamp other)
		{
			return t.Equals(other.t);
		}

		public override bool Equals(object obj)
		{
			if (obj is TimeStamp)
			{
				return Equals((TimeStamp)obj);
			}
			return false;
		}

		public int CompareTo(TimeStamp other)
		{
			return t.CompareTo(other.t);
		}

		public static TimeStamp operator +(TimeStamp a, float b)
		{
			return new TimeStamp(a.t + b);
		}

		public static TimeStamp operator -(TimeStamp a, float b)
		{
			return new TimeStamp(a.t - b);
		}

		public static float operator -(TimeStamp a, TimeStamp b)
		{
			return a.t - b.t;
		}

		public static bool operator <(TimeStamp a, TimeStamp b)
		{
			return a.t < b.t;
		}

		public static bool operator >(TimeStamp a, TimeStamp b)
		{
			return a.t > b.t;
		}

		public static bool operator <=(TimeStamp a, TimeStamp b)
		{
			return a.t <= b.t;
		}

		public static bool operator >=(TimeStamp a, TimeStamp b)
		{
			return a.t >= b.t;
		}

		public static bool operator ==(TimeStamp a, TimeStamp b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(TimeStamp a, TimeStamp b)
		{
			return !a.Equals(b);
		}

		public static float operator -(TimeStamp a, FixedTimeStamp b)
		{
			return a.t - b.t;
		}

		public static TimeStamp Deserialize(NetworkReader reader)
		{
			return new TimeStamp(reader.ReadSingle());
		}

		public static void Serialize(NetworkWriter writer, TimeStamp timeStamp)
		{
			writer.Write(timeStamp.t);
		}

		public static void ToXml(XElement element, TimeStamp src)
		{
			element.Value = TextSerialization.ToStringInvariant(src.t);
		}

		public static bool FromXml(XElement element, ref TimeStamp dest)
		{
			if (TextSerialization.TryParseInvariant(element.Value, out float result))
			{
				dest = new TimeStamp(result);
				return true;
			}
			return false;
		}

		static TimeStamp()
		{
			zero = new TimeStamp(0f);
			positiveInfinity = new TimeStamp(float.PositiveInfinity);
			negativeInfinity = new TimeStamp(float.NegativeInfinity);
			HGXml.Register<TimeStamp>(ToXml, FromXml);
		}
	}

	[Serializable]
	public struct FixedTimeStamp : IEquatable<FixedTimeStamp>, IComparable<FixedTimeStamp>
	{
		public readonly float t;

		private static float tNow;

		public static readonly FixedTimeStamp zero;

		public static readonly FixedTimeStamp positiveInfinity;

		public static readonly FixedTimeStamp negativeInfinity;

		public float timeUntil => t - tNow;

		public float timeSince => tNow - t;

		public float timeUntilClamped => Mathf.Max(timeUntil, 0f);

		public float timeSinceClamped => Mathf.Max(timeSince, 0f);

		public bool hasPassed => t <= tNow;

		public bool isInfinity => float.IsInfinity(t);

		public bool isPositiveInfinity => float.IsPositiveInfinity(t);

		public bool isNegativeInfinity => float.IsNegativeInfinity(t);

		public static FixedTimeStamp now => new FixedTimeStamp(tNow);

		public override int GetHashCode()
		{
			return t.GetHashCode();
		}

		public static void Update()
		{
			tNow = instance.fixedTime;
		}

		private FixedTimeStamp(float t)
		{
			this.t = t;
		}

		public bool Equals(FixedTimeStamp other)
		{
			return t.Equals(other.t);
		}

		public override bool Equals(object obj)
		{
			if (obj is FixedTimeStamp)
			{
				return Equals((FixedTimeStamp)obj);
			}
			return false;
		}

		public int CompareTo(FixedTimeStamp other)
		{
			return t.CompareTo(other.t);
		}

		public static FixedTimeStamp operator +(FixedTimeStamp a, float b)
		{
			return new FixedTimeStamp(a.t + b);
		}

		public static FixedTimeStamp operator -(FixedTimeStamp a, float b)
		{
			return new FixedTimeStamp(a.t - b);
		}

		public static float operator -(FixedTimeStamp a, FixedTimeStamp b)
		{
			return a.t - b.t;
		}

		public static bool operator <(FixedTimeStamp a, FixedTimeStamp b)
		{
			return a.t < b.t;
		}

		public static bool operator >(FixedTimeStamp a, FixedTimeStamp b)
		{
			return a.t > b.t;
		}

		public static bool operator <=(FixedTimeStamp a, FixedTimeStamp b)
		{
			return a.t <= b.t;
		}

		public static bool operator >=(FixedTimeStamp a, FixedTimeStamp b)
		{
			return a.t >= b.t;
		}

		public static bool operator ==(FixedTimeStamp a, FixedTimeStamp b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(FixedTimeStamp a, FixedTimeStamp b)
		{
			return !a.Equals(b);
		}

		public static FixedTimeStamp Deserialize(NetworkReader reader)
		{
			return new FixedTimeStamp(reader.ReadSingle());
		}

		public static void Serialize(NetworkWriter writer, FixedTimeStamp timeStamp)
		{
			writer.Write(timeStamp.t);
		}

		public static void ToXml(XElement element, FixedTimeStamp src)
		{
			element.Value = TextSerialization.ToStringInvariant(src.t);
		}

		public static bool FromXml(XElement element, ref FixedTimeStamp dest)
		{
			if (TextSerialization.TryParseInvariant(element.Value, out float result))
			{
				dest = new FixedTimeStamp(result);
				return true;
			}
			return false;
		}

		static FixedTimeStamp()
		{
			zero = new FixedTimeStamp(0f);
			positiveInfinity = new FixedTimeStamp(float.PositiveInfinity);
			negativeInfinity = new FixedTimeStamp(float.NegativeInfinity);
			HGXml.Register<FixedTimeStamp>(ToXml, FromXml);
		}
	}

	private NetworkRuleBook networkRuleBookComponent;

	[HideInInspector]
	[Tooltip("This is assigned to the prefab automatically by GameModeCatalog at runtime. Do not set this value manually.")]
	public GameModeIndex gameModeIndex = GameModeIndex.Invalid;

	public string nameToken = "";

	[Tooltip("Whether or not the user can select this game mode for play in the game mode selector UI.")]
	public bool userPickable = true;

	[Tooltip("Dictates items given when rebirth is active when the user doesn't have a stored item.")]
	public PickupDropTable rebirthDropTable;

	public static int stagesPerLoop = 5;

	public static float baseGravity = -30f;

	[SyncVar]
	private NetworkGuid _uniqueId;

	[SyncVar]
	private NetworkDateTime startTimeUtc;

	[Obsolete("Use startingSceneGroup instead.")]
	[ShowFieldObsolete]
	[Tooltip("The pool of scenes to select the first scene of the run from.")]
	public SceneDef[] startingScenes = Array.Empty<SceneDef>();

	[Tooltip("The pool of scenes to select the first scene of the run from.")]
	public SceneCollection startingSceneGroup;

	public ItemMask availableItems;

	public ItemMask expansionLockedItems;

	public EquipmentMask availableEquipment;

	public EquipmentMask expansionLockedEquipment;

	public bool isRunWeekly;

	[SyncVar]
	public float fixedTime;

	public float time;

	[HideInInspector]
	public bool isRunning;

	[SyncVar]
	private RunStopwatch runStopwatch;

	[SyncVar]
	public int stageClearCount;

	public SceneDef nextStageScene;

	public SceneDef[] blacklistedScenesForFirstScene = Array.Empty<SceneDef>();

	public GameObject gameOverPrefab;

	public GameObject lobbyBackgroundPrefab;

	public GameObject uiPrefab;

	private ulong _seed;

	public Xoroshiro128Plus runRNG;

	public Xoroshiro128Plus nextStageRng;

	public Xoroshiro128Plus stageRngGenerator;

	public Xoroshiro128Plus stageRng;

	public Xoroshiro128Plus bossRewardRng;

	public Xoroshiro128Plus treasureRng;

	public Xoroshiro128Plus spawnRng;

	public Xoroshiro128Plus randomSurvivorOnRespawnRng;

	public float difficultyCoefficient = 1f;

	public float compensatedDifficultyCoefficient = 1f;

	public float oneOverCompensatedDifficultyCoefficientSquared = 1f;

	[SyncVar]
	private int selectedDifficultyInternal = 1;

	public int shopPortalCount;

	private static int ambientLevelCap = 99;

	private bool lowerPricedChestsTimerOn;

	private float lowerPricedChestsTimer;

	private float lowerPricedChestsDuration = 10f;

	private static readonly StringConVar cvRunSceneOverride = new StringConVar("run_scene_override", ConVarFlags.Cheat, "", "Overrides the first scene to enter in a run.");

	private readonly HashSet<UnlockableDef> unlockablesUnlockedByAnyUser = new HashSet<UnlockableDef>();

	private readonly HashSet<UnlockableDef> unlockablesUnlockedByAllUsers = new HashSet<UnlockableDef>();

	private readonly HashSet<UnlockableDef> unlockablesAlreadyFullyObtained = new HashSet<UnlockableDef>();

	private bool shutdown;

	private Dictionary<NetworkUserId, CharacterMaster> userMasters = new Dictionary<NetworkUserId, CharacterMaster>();

	private bool allowNewParticipants;

	public readonly List<PickupIndex> availableTier1DropList = new List<PickupIndex>();

	public readonly List<PickupIndex> availableTier2DropList = new List<PickupIndex>();

	public readonly List<PickupIndex> availableTier3DropList = new List<PickupIndex>();

	public readonly List<PickupIndex> availableEquipmentDropList = new List<PickupIndex>();

	public readonly List<PickupIndex> availableLunarEquipmentDropList = new List<PickupIndex>();

	public readonly List<PickupIndex> availableLunarItemDropList = new List<PickupIndex>();

	public readonly List<PickupIndex> availableLunarCombinedDropList = new List<PickupIndex>();

	public readonly List<PickupIndex> availableBossDropList = new List<PickupIndex>();

	public readonly List<PickupIndex> availableVoidTier1DropList = new List<PickupIndex>();

	public readonly List<PickupIndex> availableVoidTier2DropList = new List<PickupIndex>();

	public readonly List<PickupIndex> availableVoidTier3DropList = new List<PickupIndex>();

	public readonly List<PickupIndex> availableVoidBossDropList = new List<PickupIndex>();

	public WeightedSelection<List<PickupIndex>> smallChestDropTierSelector = new WeightedSelection<List<PickupIndex>>();

	public WeightedSelection<List<PickupIndex>> mediumChestDropTierSelector = new WeightedSelection<List<PickupIndex>>();

	public WeightedSelection<List<PickupIndex>> largeChestDropTierSelector = new WeightedSelection<List<PickupIndex>>();

	private readonly HashSet<string> eventFlags = new HashSet<string>();

	public static Run instance { get; private set; }

	public RuleBook ruleBook => networkRuleBookComponent.ruleBook;

	public bool isRunStopwatchPaused => runStopwatch.isPaused;

	public virtual int loopClearCount => stageClearCount / stagesPerLoop;

	public virtual bool spawnWithPod => instance.stageClearCount == 0;

	public virtual bool autoGenerateSpawnPoints => true;

	public virtual bool canFamilyEventTrigger => true;

	public IList<GameObject> uiInstances { get; protected set; } = new List<GameObject>();

	public ulong seed
	{
		get
		{
			return _seed;
		}
		set
		{
			_seed = value;
			OnSeedSet();
		}
	}

	public DifficultyIndex selectedDifficulty
	{
		get
		{
			return (DifficultyIndex)selectedDifficultyInternal;
		}
		set
		{
			NetworkselectedDifficultyInternal = (int)value;
		}
	}

	public int livingPlayerCount => PlayerCharacterMasterController.GetPlayersWithBodiesCount();

	public int participatingPlayerCount => PlayerCharacterMasterController.instances.Count;

	public float ambientLevel { get; protected set; }

	public int ambientLevelFloor { get; protected set; }

	public float teamlessDamageCoefficient => difficultyCoefficient;

	public bool isGameOverServer { get; private set; }

	public NetworkGuid Network_uniqueId
	{
		get
		{
			return _uniqueId;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref _uniqueId, 1u);
		}
	}

	public NetworkDateTime NetworkstartTimeUtc
	{
		get
		{
			return startTimeUtc;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref startTimeUtc, 2u);
		}
	}

	public float NetworkfixedTime
	{
		get
		{
			return fixedTime;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref fixedTime, 4u);
		}
	}

	public RunStopwatch NetworkrunStopwatch
	{
		get
		{
			return runStopwatch;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref runStopwatch, 8u);
		}
	}

	public int NetworkstageClearCount
	{
		get
		{
			return stageClearCount;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref stageClearCount, 16u);
		}
	}

	public int NetworkselectedDifficultyInternal
	{
		get
		{
			return selectedDifficultyInternal;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref selectedDifficultyInternal, 32u);
		}
	}

	public static event Action<Run, RuleBook> onServerRunSetRuleBookGlobal;

	public static event Action<Run, RuleBook> onRunSetRuleBookGlobal;

	public static event Action<Run> onRunAmbientLevelUp;

	public static event Action<Run, PlayerCharacterMasterController> onPlayerFirstCreatedServer;

	public static event Action<Run, GameEndingDef> onServerGameOver;

	public static event Action<Run, RunReport> onClientGameOverGlobal;

	public static event Action<Run> onRunStartGlobal;

	public static event Action<Run> onRunDestroyGlobal;

	public static event Action<Run> onAvailablePickupsModified;

	protected virtual void OnEnable()
	{
		instance = SingletonHelper.Assign(instance, this);
	}

	protected virtual void OnDisable()
	{
		instance = SingletonHelper.Unassign(instance, this);
		if (instance == null)
		{
			RewiredIntegrationManager.RefreshAfterRunFinish();
		}
	}

	protected void Awake()
	{
		networkRuleBookComponent = GetComponent<NetworkRuleBook>();
		networkRuleBookComponent.onRuleBookUpdated += OnRuleBookUpdated;
		availableItems = ItemMask.Rent();
		expansionLockedItems = ItemMask.Rent();
		availableEquipment = EquipmentMask.Rent();
		expansionLockedEquipment = EquipmentMask.Rent();
		if (NetworkServer.active)
		{
			Network_uniqueId = (NetworkGuid)Guid.NewGuid();
			NetworkstartTimeUtc = (NetworkDateTime)DateTime.UtcNow;
		}
	}

	[Server]
	public void SetRuleBook(RuleBook newRuleBook)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.Run::SetRuleBook(RoR2.RuleBook)' called on client");
		}
		else
		{
			networkRuleBookComponent.SetRuleBook(newRuleBook);
		}
	}

	private void OnRuleBookUpdated(NetworkRuleBook networkRuleBookComponent)
	{
		RuleBook ruleBook = networkRuleBookComponent.ruleBook;
		selectedDifficulty = ruleBook.FindDifficulty();
		ruleBook.GenerateItemMask(availableItems);
		ruleBook.GenerateEquipmentMask(availableEquipment);
		expansionLockedItems.Clear();
		foreach (ItemDef allItemDef in ItemCatalog.allItemDefs)
		{
			if ((bool)allItemDef && (bool)allItemDef.requiredExpansion && !IsExpansionEnabled(allItemDef.requiredExpansion))
			{
				expansionLockedItems.Add(allItemDef.itemIndex);
			}
		}
		expansionLockedEquipment.Clear();
		foreach (EquipmentIndex item in EquipmentCatalog.allEquipment)
		{
			EquipmentDef equipmentDef = EquipmentCatalog.GetEquipmentDef(item);
			if ((bool)equipmentDef && (bool)equipmentDef.requiredExpansion && !IsExpansionEnabled(equipmentDef.requiredExpansion))
			{
				expansionLockedEquipment.Add(equipmentDef.equipmentIndex);
			}
		}
		if (NetworkServer.active)
		{
			Run.onServerRunSetRuleBookGlobal?.Invoke(this, ruleBook);
		}
		Run.onRunSetRuleBookGlobal?.Invoke(this, ruleBook);
	}

	public Guid GetUniqueId()
	{
		return (Guid)_uniqueId;
	}

	public DateTime GetStartTimeUtc()
	{
		return (DateTime)startTimeUtc;
	}

	[Server]
	private void SetRunStopwatchPaused(bool isPaused)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.Run::SetRunStopwatchPaused(System.Boolean)' called on client");
		}
		else if (isPaused != runStopwatch.isPaused)
		{
			RunStopwatch networkrunStopwatch = runStopwatch;
			networkrunStopwatch.isPaused = isPaused;
			float num = GetRunStopwatch();
			if (isPaused)
			{
				networkrunStopwatch.offsetFromFixedTime = num;
			}
			else
			{
				networkrunStopwatch.offsetFromFixedTime = num - fixedTime;
			}
			NetworkrunStopwatch = networkrunStopwatch;
		}
	}

	public float GetRunStopwatch()
	{
		if (runStopwatch.isPaused)
		{
			return runStopwatch.offsetFromFixedTime;
		}
		return fixedTime + runStopwatch.offsetFromFixedTime;
	}

	[Server]
	public void SetRunStopwatch(float t)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.Run::SetRunStopwatch(System.Single)' called on client");
			return;
		}
		RunStopwatch networkrunStopwatch = runStopwatch;
		if (networkrunStopwatch.isPaused)
		{
			networkrunStopwatch.offsetFromFixedTime = t;
		}
		else
		{
			networkrunStopwatch.offsetFromFixedTime = t - fixedTime;
		}
		NetworkrunStopwatch = networkrunStopwatch;
	}

	public virtual GameObject InstantiateUi(Transform uiRoot)
	{
		GameObject gameObject = null;
		if ((bool)uiRoot && (bool)uiPrefab)
		{
			gameObject = UnityEngine.Object.Instantiate(uiPrefab, uiRoot);
			uiInstances.Add(gameObject);
		}
		return gameObject;
	}

	public void UnregisterUi(GameObject uiInstance)
	{
		uiInstances.Remove(uiInstance);
	}

	private void GenerateStageRNG()
	{
		stageRng = new Xoroshiro128Plus(stageRngGenerator.nextUlong);
		bossRewardRng = new Xoroshiro128Plus(stageRng.nextUlong);
		treasureRng = new Xoroshiro128Plus(stageRng.nextUlong);
		spawnRng = new Xoroshiro128Plus(stageRng.nextUlong);
		randomSurvivorOnRespawnRng = new Xoroshiro128Plus(stageRng.nextUlong);
	}

	protected void OnAmbientLevelUp()
	{
		Run.onRunAmbientLevelUp?.Invoke(this);
	}

	protected virtual void FixedUpdate()
	{
		NetworkfixedTime = fixedTime + Time.fixedDeltaTime;
		FixedTimeStamp.Update();
		if (NetworkServer.active)
		{
			SetRunStopwatchPaused(!ShouldUpdateRunStopwatch());
		}
		OnFixedUpdate();
	}

	public void RecalculateDifficultyCoefficent()
	{
		RecalculateDifficultyCoefficentInternal();
	}

	protected virtual void RecalculateDifficultyCoefficentInternal()
	{
		float num = GetRunStopwatch();
		DifficultyDef difficultyDef = DifficultyCatalog.GetDifficultyDef(selectedDifficulty);
		float num2 = Mathf.Floor(num * (1f / 60f));
		float num3 = (float)participatingPlayerCount * 0.3f;
		float num4 = 0.7f + num3;
		float num5 = 0.7f + num3;
		float num6 = Mathf.Pow(participatingPlayerCount, 0.2f);
		float num7 = 0.0506f * difficultyDef.scalingValue * num6;
		float num8 = 0.0506f * difficultyDef.scalingValue * num6;
		float num9 = Mathf.Pow(1.15f, stageClearCount);
		compensatedDifficultyCoefficient = (num5 + num8 * num2) * num9;
		difficultyCoefficient = (num4 + num7 * num2) * num9;
		oneOverCompensatedDifficultyCoefficientSquared = 1f / (compensatedDifficultyCoefficient * compensatedDifficultyCoefficient);
		float num10 = (num4 + num7 * (num * (1f / 60f))) * Mathf.Pow(1.15f, stageClearCount);
		ambientLevel = Mathf.Min((num10 - num4) / 0.33f + 1f, ambientLevelCap);
		int num11 = ambientLevelFloor;
		ambientLevelFloor = Mathf.FloorToInt(ambientLevel);
		if (num11 != ambientLevelFloor && num11 != 0 && ambientLevelFloor > num11)
		{
			OnAmbientLevelUp();
		}
	}

	private void InitializeTimeStamps()
	{
		NetworkfixedTime = 0f;
		time = 0f;
		FixedTimeStamp.Update();
		TimeStamp.Update();
	}

	protected virtual void OnFixedUpdate()
	{
		RecalculateDifficultyCoefficent();
	}

	public bool GetLowerPricedChestsTimer()
	{
		return lowerPricedChestsTimerOn;
	}

	protected void Update()
	{
		time = Mathf.Clamp(time + Time.deltaTime, fixedTime, fixedTime + Time.fixedDeltaTime);
		TimeStamp.Update();
		if ((double)GetRunStopwatch() - Math.Floor(GetRunStopwatch() * (1f / 60f)) * 60.0 <= 0.1 && !lowerPricedChestsTimerOn)
		{
			lowerPricedChestsTimerOn = true;
		}
		if (lowerPricedChestsTimerOn)
		{
			lowerPricedChestsTimer += Time.fixedDeltaTime;
		}
		if (lowerPricedChestsTimer >= lowerPricedChestsDuration)
		{
			lowerPricedChestsTimerOn = false;
			lowerPricedChestsTimer = 0f;
		}
	}

	protected virtual bool ShouldUpdateRunStopwatch()
	{
		SceneDef mostRecentSceneDef = SceneCatalog.mostRecentSceneDef;
		if (mostRecentSceneDef.sceneType != SceneType.Stage && mostRecentSceneDef.sceneType != SceneType.TimedIntermission)
		{
			return false;
		}
		return livingPlayerCount > 0;
	}

	[Obsolete("Use the overload that accepts an UnlockableDef instead. This method may be removed from future releases.", false)]
	[Server]
	public bool CanUnlockableBeGrantedThisRun(string unlockableName)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Boolean RoR2.Run::CanUnlockableBeGrantedThisRun(System.String)' called on client");
			return false;
		}
		return CanUnlockableBeGrantedThisRun(UnlockableCatalog.GetUnlockableDef(unlockableName));
	}

	[Server]
	public virtual bool CanUnlockableBeGrantedThisRun(UnlockableDef unlockableDef)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Boolean RoR2.Run::CanUnlockableBeGrantedThisRun(RoR2.UnlockableDef)' called on client");
			return false;
		}
		return !unlockablesAlreadyFullyObtained.Contains(unlockableDef);
	}

	[Obsolete("Use the overload that accepts an UnlockableDef instead. This method may be removed from future releases.", false)]
	[Server]
	public void GrantUnlockToAllParticipatingPlayers(string unlockableName)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.Run::GrantUnlockToAllParticipatingPlayers(System.String)' called on client");
		}
		else
		{
			GrantUnlockToAllParticipatingPlayers(UnlockableCatalog.GetUnlockableDef(unlockableName));
		}
	}

	[Server]
	public void GrantUnlockToAllParticipatingPlayers(UnlockableDef unlockableDef)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.Run::GrantUnlockToAllParticipatingPlayers(RoR2.UnlockableDef)' called on client");
		}
		else
		{
			if (!unlockableDef || unlockableDef.index == UnlockableIndex.None || unlockablesAlreadyFullyObtained.Contains(unlockableDef))
			{
				return;
			}
			unlockablesAlreadyFullyObtained.Add(unlockableDef);
			foreach (NetworkUser readOnlyInstances in NetworkUser.readOnlyInstancesList)
			{
				if (readOnlyInstances.isParticipating)
				{
					readOnlyInstances.ServerHandleUnlock(unlockableDef);
				}
			}
		}
	}

	[Obsolete("Use the overload that accepts an UnlockableDef instead. This method may be removed from future releases.", false)]
	[Server]
	public void GrantUnlockToSinglePlayer(string unlockableName, CharacterBody body)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.Run::GrantUnlockToSinglePlayer(System.String,RoR2.CharacterBody)' called on client");
		}
		else
		{
			GrantUnlockToSinglePlayer(UnlockableCatalog.GetUnlockableDef(unlockableName), body);
		}
	}

	[Server]
	public void GrantUnlockToSinglePlayer(UnlockableDef unlockableDef, CharacterBody body)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.Run::GrantUnlockToSinglePlayer(RoR2.UnlockableDef,RoR2.CharacterBody)' called on client");
		}
		else if ((bool)unlockableDef && unlockableDef.index != UnlockableIndex.None && (bool)body)
		{
			NetworkUser networkUser = Util.LookUpBodyNetworkUser(body);
			if ((bool)networkUser)
			{
				networkUser.ServerHandleUnlock(unlockableDef);
			}
		}
	}

	[Obsolete("Use the overload that accepts an UnlockableDef instead. This method may be removed from future releases.", false)]
	[Server]
	public bool IsUnlockableUnlocked(string unlockableName)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Boolean RoR2.Run::IsUnlockableUnlocked(System.String)' called on client");
			return false;
		}
		return IsUnlockableUnlocked(UnlockableCatalog.GetUnlockableDef(unlockableName));
	}

	[Server]
	public virtual bool IsUnlockableUnlocked(UnlockableDef unlockableDef)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Boolean RoR2.Run::IsUnlockableUnlocked(RoR2.UnlockableDef)' called on client");
			return false;
		}
		return unlockablesUnlockedByAnyUser.Contains(unlockableDef);
	}

	[Obsolete("Use the overload that accepts an UnlockableDef instead. This method may be removed from future releases.", false)]
	[Server]
	public bool DoesEveryoneHaveThisUnlockableUnlocked(string unlockableName)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Boolean RoR2.Run::DoesEveryoneHaveThisUnlockableUnlocked(System.String)' called on client");
			return false;
		}
		return DoesEveryoneHaveThisUnlockableUnlocked(UnlockableCatalog.GetUnlockableDef(unlockableName));
	}

	[Server]
	public virtual bool DoesEveryoneHaveThisUnlockableUnlocked(UnlockableDef unlockableDef)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Boolean RoR2.Run::DoesEveryoneHaveThisUnlockableUnlocked(RoR2.UnlockableDef)' called on client");
			return false;
		}
		return unlockablesUnlockedByAllUsers.Contains(unlockableDef);
	}

	[Obsolete("Use the overload that accepts an UnlockableDef instead. This method may be removed from future releases.", false)]
	[Server]
	public void ForceUnlockImmediate(string unlockableName)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.Run::ForceUnlockImmediate(System.String)' called on client");
		}
		else
		{
			ForceUnlockImmediate(UnlockableCatalog.GetUnlockableDef(unlockableName));
		}
	}

	[Server]
	public void ForceUnlockImmediate(UnlockableDef unlockableDef)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.Run::ForceUnlockImmediate(RoR2.UnlockableDef)' called on client");
		}
		else
		{
			unlockablesUnlockedByAnyUser.Add(unlockableDef);
		}
	}

	public void PickNextStageSceneFromCurrentSceneDestinations()
	{
		WeightedSelection<SceneDef> weightedSelection = new WeightedSelection<SceneDef>();
		SceneCatalog.mostRecentSceneDef.AddDestinationsToWeightedSelection(weightedSelection, CanPickStage);
		PickNextStageScene(weightedSelection);
	}

	public bool CanPickStage(SceneDef sceneDef)
	{
		if ((bool)sceneDef.requiredExpansion)
		{
			return IsExpansionEnabled(sceneDef.requiredExpansion);
		}
		return true;
	}

	public void PickNextStageScene(WeightedSelection<SceneDef> choices)
	{
		if (choices.Count != 0)
		{
			if (ruleBook.stageOrder == StageOrder.Normal)
			{
				nextStageScene = choices.Evaluate(nextStageRng.nextNormalizedFloat);
				return;
			}
			SceneDef[] array = SceneCatalog.allStageSceneDefs.Where(IsValidNextStage).ToArray();
			nextStageScene = nextStageRng.NextElementUniform(array);
		}
		bool IsValidNextStage(SceneDef sceneDef)
		{
			if (nextStageScene != null && nextStageScene.baseSceneName == sceneDef.baseSceneName)
			{
				return false;
			}
			if (!sceneDef.hasAnyDestinations)
			{
				return false;
			}
			if (stageClearCount == 0 && blacklistedScenesForFirstScene.Contains(sceneDef))
			{
				return false;
			}
			return sceneDef.validForRandomSelection;
		}
	}

	public virtual ulong GenerateSeedForNewRun()
	{
		return RoR2Application.rng.nextUlong;
	}

	[Server]
	public void RefreshUnlockAvailability()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.Run::RefreshUnlockAvailability()' called on client");
		}
		else
		{
			BuildUnlockAvailability();
		}
	}

	protected virtual void BuildUnlockAvailability()
	{
		unlockablesUnlockedByAnyUser.Clear();
		unlockablesUnlockedByAllUsers.Clear();
		unlockablesAlreadyFullyObtained.Clear();
		int num = 0;
		Dictionary<UnlockableDef, int> dictionary = new Dictionary<UnlockableDef, int>();
		foreach (NetworkUser readOnlyInstances in NetworkUser.readOnlyInstancesList)
		{
			if (!readOnlyInstances.isParticipating)
			{
				continue;
			}
			num++;
			foreach (UnlockableDef unlockable in readOnlyInstances.unlockables)
			{
				unlockablesUnlockedByAnyUser.Add(unlockable);
				if (!dictionary.ContainsKey(unlockable))
				{
					dictionary.Add(unlockable, 0);
				}
				UnlockableDef key = unlockable;
				int value = dictionary[key] + 1;
				dictionary[key] = value;
			}
		}
		if (num <= 0)
		{
			return;
		}
		foreach (KeyValuePair<UnlockableDef, int> item in dictionary)
		{
			if (item.Value == num)
			{
				unlockablesUnlockedByAllUsers.Add(item.Key);
				unlockablesAlreadyFullyObtained.Add(item.Key);
			}
		}
	}

	protected virtual void Start()
	{
		InitializeTimeStamps();
		OnRuleBookUpdated(networkRuleBookComponent);
		if (NetworkServer.active)
		{
			runRNG = new Xoroshiro128Plus(seed);
			nextStageRng = new Xoroshiro128Plus(runRNG.nextUlong);
			stageRngGenerator = new Xoroshiro128Plus(runRNG.nextUlong);
			GenerateStageRNG();
		}
		allowNewParticipants = true;
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
		ReadOnlyCollection<NetworkUser> readOnlyInstancesList = NetworkUser.readOnlyInstancesList;
		for (int i = 0; i < readOnlyInstancesList.Count; i++)
		{
			OnUserAdded(readOnlyInstancesList[i]);
		}
		allowNewParticipants = false;
		if (NetworkServer.active)
		{
			WeightedSelection<SceneDef> weightedSelection = new WeightedSelection<SceneDef>();
			string @string = cvRunSceneOverride.GetString();
			if (@string != "")
			{
				weightedSelection.AddChoice(SceneCatalog.GetSceneDefFromSceneName(@string), 1f);
			}
			else if ((bool)startingSceneGroup)
			{
				startingSceneGroup.AddToWeightedSelection(weightedSelection, CanPickStage);
			}
			else
			{
				for (int j = 0; j < startingScenes.Length; j++)
				{
					if (CanPickStage(startingScenes[j]))
					{
						weightedSelection.AddChoice(startingScenes[j], 1f);
					}
				}
			}
			PickNextStageScene(weightedSelection);
			if (!nextStageScene)
			{
				Debug.LogError("Cannot set next scene. nextStageScene is null!");
			}
			NetworkManager.singleton.ServerChangeScene(nextStageScene.cachedName);
		}
		BuildUnlockAvailability();
		BuildDropTable();
		isRunning = true;
		Run.onRunStartGlobal?.Invoke(this);
		if (NetworkServer.active && RunArtifactManager.instance.IsArtifactEnabled(DLC2Content.Artifacts.Rebirth))
		{
			ServerGiveRebirthItems();
		}
		if (!NetworkClient.active || !RunArtifactManager.instance.IsArtifactEnabled(DLC2Content.Artifacts.Rebirth))
		{
			return;
		}
		foreach (NetworkUser readOnlyInstances in NetworkUser.readOnlyInstancesList)
		{
			if (readOnlyInstances.isLocalPlayer)
			{
				readOnlyInstances.localUser.userProfile.RebirthItem = null;
				break;
			}
		}
	}

	protected virtual void OnDestroy()
	{
		Run.onRunDestroyGlobal?.Invoke(this);
		if ((bool)GameOverController.instance)
		{
			UnityEngine.Object.Destroy(GameOverController.instance.gameObject);
		}
		ReadOnlyCollection<CharacterBody> readOnlyInstancesList = CharacterBody.readOnlyInstancesList;
		for (int num = readOnlyInstancesList.Count - 1; num >= 0; num--)
		{
			if ((bool)readOnlyInstancesList[num])
			{
				UnityEngine.Object.Destroy(readOnlyInstancesList[num].gameObject);
			}
		}
		ReadOnlyCollection<CharacterMaster> readOnlyInstancesList2 = CharacterMaster.readOnlyInstancesList;
		for (int num2 = readOnlyInstancesList2.Count - 1; num2 >= 0; num2--)
		{
			if ((bool)readOnlyInstancesList2[num2])
			{
				UnityEngine.Object.Destroy(readOnlyInstancesList2[num2].gameObject);
			}
		}
		if ((bool)Stage.instance)
		{
			UnityEngine.Object.Destroy(Stage.instance.gameObject);
		}
		Chat.Clear();
		if (!shutdown && PlatformSystems.networkManager.isNetworkActive)
		{
			HandlePostRunDestination();
		}
		ItemMask.Return(availableItems);
		ItemMask.Return(expansionLockedItems);
		EquipmentMask.Return(availableEquipment);
		EquipmentMask.Return(expansionLockedEquipment);
		PlatformSystems.saveSystem.SaveLoggedInUserProfiles();
	}

	[Server]
	private void ServerGiveRebirthItems()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.Run::ServerGiveRebirthItems()' called on client");
		}
		else
		{
			if (!NetworkServer.active)
			{
				return;
			}
			ItemIndex itemIndex = ItemIndex.None;
			foreach (PlayerCharacterMasterController instance in PlayerCharacterMasterController.instances)
			{
				Inventory inventory = instance.master.inventory;
				NetworkUser networkUser = instance.networkUser;
				if (networkUser.rebirthItem != ItemIndex.None)
				{
					if ((bool)inventory)
					{
						itemIndex = networkUser.rebirthItem;
						networkUser.NetworkrebirthItem = ItemIndex.None;
					}
				}
				else if (rebirthDropTable != null)
				{
					Xoroshiro128Plus rng = new Xoroshiro128Plus(Run.instance.treasureRng.nextUlong);
					itemIndex = PickupCatalog.GetPickupDef(rebirthDropTable.GenerateDrop(rng)).itemIndex;
				}
				if ((bool)inventory)
				{
					inventory.GiveItem(itemIndex);
				}
			}
		}
	}

	protected virtual void HandlePostRunDestination()
	{
		if (NetworkServer.active)
		{
			NetworkManager.singleton.ServerChangeScene("lobby");
		}
	}

	protected void OnApplicationQuit()
	{
		shutdown = true;
	}

	[Server]
	public CharacterMaster GetUserMaster(NetworkUserId networkUserId)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'RoR2.CharacterMaster RoR2.Run::GetUserMaster(RoR2.NetworkUserId)' called on client");
			return null;
		}
		userMasters.TryGetValue(networkUserId, out var value);
		return value;
	}

	[Server]
	public void OnServerSceneChanged(string sceneName)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.Run::OnServerSceneChanged(System.String)' called on client");
			return;
		}
		BeginStage();
		isGameOverServer = false;
	}

	[Server]
	private void BeginStage()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.Run::BeginStage()' called on client");
		}
		else
		{
			NetworkServer.Spawn(UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/Stage")));
		}
	}

	[Server]
	private void EndStage()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.Run::EndStage()' called on client");
		}
		else if ((bool)Stage.instance)
		{
			UnityEngine.Object.Destroy(Stage.instance);
		}
	}

	public void OnUserAdded(NetworkUser user)
	{
		if (NetworkServer.active)
		{
			SetupUserCharacterMaster(user);
		}
	}

	public void OnUserRemoved(NetworkUser user)
	{
	}

	public virtual bool ShouldAllowNewParticipant(NetworkUser user)
	{
		if (!allowNewParticipants)
		{
			return (bool)ruleBook.GetRuleChoice(RuleCatalog.FindRuleDef("Misc.AllowDropIn"))?.extraData;
		}
		return true;
	}

	[Server]
	private void SetupUserCharacterMaster(NetworkUser user)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.Run::SetupUserCharacterMaster(RoR2.NetworkUser)' called on client");
		}
		else if (!user)
		{
			Debug.LogError("SetupUserCharacterMaster user is null");
		}
		else
		{
			if ((bool)user.masterObject)
			{
				return;
			}
			CharacterMaster characterMaster = GetUserMaster(user.id);
			bool flag = !characterMaster && ShouldAllowNewParticipant(user);
			if (flag)
			{
				characterMaster = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/CharacterMasters/CommandoMaster"), Vector3.zero, Quaternion.identity).GetComponent<CharacterMaster>();
				if ((bool)characterMaster)
				{
					userMasters[user.id] = characterMaster;
					if (ruleBook != null)
					{
						characterMaster.GiveMoney(ruleBook.startingMoney);
					}
					else
					{
						Debug.LogError("Can't give new master money because there's no rulebook");
					}
					DifficultyDef difficultyDef = DifficultyCatalog.GetDifficultyDef(selectedDifficulty);
					if ((bool)characterMaster.inventory)
					{
						if (selectedDifficulty == DifficultyIndex.Easy)
						{
							characterMaster.inventory.GiveItem(RoR2Content.Items.DrizzlePlayerHelper);
						}
						else if (difficultyDef.countsAsHardMode)
						{
							characterMaster.inventory.GiveItem(RoR2Content.Items.MonsoonPlayerHelper);
						}
					}
					else
					{
						Debug.LogError("Instantiated master has no inventory");
					}
					NetworkServer.Spawn(characterMaster.gameObject);
				}
				else
				{
					Debug.LogError("Instantiated master object has no CharacterMaster");
				}
			}
			if ((bool)characterMaster)
			{
				PlayerCharacterMasterController component = characterMaster.GetComponent<PlayerCharacterMasterController>();
				if ((bool)component)
				{
					component.LinkToNetworkUserServer(user);
				}
				else
				{
					Debug.LogError("CharacterMaster has no PlayerCharacterMasterController");
				}
				if (flag)
				{
					Run.onPlayerFirstCreatedServer?.Invoke(this, component);
				}
			}
		}
	}

	[Server]
	public virtual void HandlePlayerFirstEntryAnimation(CharacterBody body, Vector3 spawnPosition, Quaternion spawnRotation)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.Run::HandlePlayerFirstEntryAnimation(RoR2.CharacterBody,UnityEngine.Vector3,UnityEngine.Quaternion)' called on client");
		}
		else if ((bool)body)
		{
			if ((bool)body.preferredPodPrefab)
			{
				GameObject gameObject = UnityEngine.Object.Instantiate(body.preferredPodPrefab, body.transform.position, spawnRotation);
				VehicleSeat component = gameObject.GetComponent<VehicleSeat>();
				if ((bool)component)
				{
					component.AssignPassenger(body.gameObject);
				}
				else
				{
					Debug.LogError("Body " + body.name + " has preferred pod " + body.preferredPodPrefab.name + ", but that object has no VehicleSeat.");
				}
				NetworkServer.Spawn(gameObject);
			}
			else
			{
				body.SetBodyStateToPreferredInitialState();
			}
		}
		else
		{
			Debug.LogError("Can't handle player first entry animation for null body.");
		}
	}

	public virtual void OnServerBossAdded(BossGroup bossGroup, CharacterMaster characterMaster)
	{
	}

	public virtual void OnServerBossDefeated(BossGroup bossGroup)
	{
	}

	public virtual void OnServerCharacterBodySpawned(CharacterBody characterBody)
	{
	}

	public virtual void OnServerTeleporterPlaced(SceneDirector sceneDirector, GameObject teleporter)
	{
	}

	public virtual void OnPlayerSpawnPointsPlaced(SceneDirector sceneDirector)
	{
	}

	public virtual GameObject GetTeleportEffectPrefab(GameObject objectToTeleport)
	{
		return LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/TeleportOutBoom");
	}

	public int GetDifficultyScaledCost(int baseCost, float difficultyCoefficient)
	{
		return (int)((float)baseCost * Mathf.Pow(difficultyCoefficient, 1.25f));
	}

	public int GetDifficultyScaledCost(int baseCost)
	{
		return GetDifficultyScaledCost(baseCost, instance.difficultyCoefficient);
	}

	public void BuildDropTable()
	{
		availableTier1DropList.Clear();
		availableTier2DropList.Clear();
		availableTier3DropList.Clear();
		availableLunarItemDropList.Clear();
		availableEquipmentDropList.Clear();
		availableBossDropList.Clear();
		availableLunarEquipmentDropList.Clear();
		availableVoidTier1DropList.Clear();
		availableVoidTier2DropList.Clear();
		availableVoidTier3DropList.Clear();
		availableVoidBossDropList.Clear();
		ItemIndex itemIndex = ItemIndex.Count;
		for (ItemIndex itemCount = (ItemIndex)ItemCatalog.itemCount; itemIndex < itemCount; itemIndex++)
		{
			if (availableItems.Contains(itemIndex))
			{
				ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
				List<PickupIndex> list = null;
				switch (itemDef.tier)
				{
				case ItemTier.Tier1:
					list = availableTier1DropList;
					break;
				case ItemTier.Tier2:
					list = availableTier2DropList;
					break;
				case ItemTier.Tier3:
					list = availableTier3DropList;
					break;
				case ItemTier.Lunar:
					list = availableLunarItemDropList;
					break;
				case ItemTier.Boss:
					list = availableBossDropList;
					break;
				case ItemTier.VoidTier1:
					list = availableVoidTier1DropList;
					break;
				case ItemTier.VoidTier2:
					list = availableVoidTier2DropList;
					break;
				case ItemTier.VoidTier3:
					list = availableVoidTier3DropList;
					break;
				case ItemTier.VoidBoss:
					list = availableVoidBossDropList;
					break;
				}
				if (list != null && itemDef.DoesNotContainTag(ItemTag.WorldUnique))
				{
					list.Add(PickupCatalog.FindPickupIndex(itemIndex));
				}
			}
		}
		EquipmentIndex equipmentIndex = (EquipmentIndex)0;
		for (EquipmentIndex equipmentCount = (EquipmentIndex)EquipmentCatalog.equipmentCount; equipmentIndex < equipmentCount; equipmentIndex++)
		{
			if (!availableEquipment.Contains(equipmentIndex))
			{
				continue;
			}
			EquipmentDef equipmentDef = EquipmentCatalog.GetEquipmentDef(equipmentIndex);
			if (equipmentDef.canDrop)
			{
				if (!equipmentDef.isLunar)
				{
					availableEquipmentDropList.Add(PickupCatalog.FindPickupIndex(equipmentIndex));
				}
				else
				{
					availableLunarEquipmentDropList.Add(PickupCatalog.FindPickupIndex(equipmentIndex));
				}
			}
		}
		smallChestDropTierSelector.Clear();
		smallChestDropTierSelector.AddChoice(availableTier1DropList, 0.8f);
		smallChestDropTierSelector.AddChoice(availableTier2DropList, 0.2f);
		smallChestDropTierSelector.AddChoice(availableTier3DropList, 0.01f);
		mediumChestDropTierSelector.Clear();
		mediumChestDropTierSelector.AddChoice(availableTier2DropList, 0.8f);
		mediumChestDropTierSelector.AddChoice(availableTier3DropList, 0.2f);
		largeChestDropTierSelector.Clear();
		RefreshLunarCombinedDropList();
	}

	public bool IsItemAvailable(ItemIndex itemIndex)
	{
		return availableItems.Contains(itemIndex);
	}

	public bool IsEquipmentAvailable(EquipmentIndex equipmentIndex)
	{
		return availableEquipment.Contains(equipmentIndex);
	}

	public bool IsItemExpansionLocked(ItemIndex itemIndex)
	{
		return expansionLockedItems.Contains(itemIndex);
	}

	public bool IsEquipmentExpansionLocked(EquipmentIndex equipmentIndex)
	{
		return expansionLockedEquipment.Contains(equipmentIndex);
	}

	public bool IsPickupAvailable(PickupIndex pickupIndex)
	{
		PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
		if (pickupDef.itemIndex != ItemIndex.None)
		{
			return IsItemAvailable(pickupDef.itemIndex);
		}
		if (pickupDef.equipmentIndex != EquipmentIndex.None)
		{
			return IsEquipmentAvailable(pickupDef.equipmentIndex);
		}
		return true;
	}

	[Server]
	public void DisablePickupDrop(PickupIndex pickupIndex)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.Run::DisablePickupDrop(RoR2.PickupIndex)' called on client");
			return;
		}
		PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
		if (pickupDef.itemIndex != ItemIndex.None)
		{
			DisableItemDrop(pickupDef.itemIndex);
		}
		if (pickupDef.equipmentIndex != EquipmentIndex.None)
		{
			DisableEquipmentDrop(pickupDef.equipmentIndex);
		}
	}

	[Server]
	public void DisableItemDrop(ItemIndex itemIndex)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.Run::DisableItemDrop(RoR2.ItemIndex)' called on client");
			return;
		}
		ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
		List<PickupIndex> list = null;
		bool flag = false;
		switch (itemDef.tier)
		{
		case ItemTier.Tier1:
			list = availableTier1DropList;
			break;
		case ItemTier.Tier2:
			list = availableTier2DropList;
			break;
		case ItemTier.Tier3:
			list = availableTier3DropList;
			break;
		case ItemTier.Lunar:
			list = availableLunarItemDropList;
			break;
		case ItemTier.Boss:
			list = availableBossDropList;
			break;
		case ItemTier.VoidTier1:
			list = availableVoidTier1DropList;
			break;
		case ItemTier.VoidTier2:
			list = availableVoidTier2DropList;
			break;
		case ItemTier.VoidTier3:
			list = availableVoidTier3DropList;
			break;
		case ItemTier.VoidBoss:
			list = availableVoidBossDropList;
			break;
		}
		PickupIndex pickupIndex = PickupCatalog.FindPickupIndex(itemIndex);
		if (list != null && pickupIndex != PickupIndex.none)
		{
			list.Remove(pickupIndex);
			if (flag)
			{
				RefreshLunarCombinedDropList();
			}
			Run.onAvailablePickupsModified?.Invoke(this);
		}
	}

	[Server]
	public void DisableEquipmentDrop(EquipmentIndex equipmentIndex)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.Run::DisableEquipmentDrop(RoR2.EquipmentIndex)' called on client");
			return;
		}
		PickupIndex pickupIndex = PickupCatalog.FindPickupIndex(equipmentIndex);
		if (!(pickupIndex != PickupIndex.none))
		{
			return;
		}
		List<PickupIndex> list = null;
		bool flag = false;
		if (PickupCatalog.GetPickupDef(pickupIndex).isLunar)
		{
			flag = true;
			list = availableLunarEquipmentDropList;
		}
		else
		{
			list = availableEquipmentDropList;
		}
		if (list.Contains(pickupIndex))
		{
			list.Remove(pickupIndex);
			if (flag)
			{
				RefreshLunarCombinedDropList();
			}
			Run.onAvailablePickupsModified?.Invoke(this);
		}
	}

	[Server]
	public void EnablePickupDrop(PickupIndex pickupIndex)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.Run::EnablePickupDrop(RoR2.PickupIndex)' called on client");
			return;
		}
		PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
		if (pickupDef.itemIndex != ItemIndex.None)
		{
			EnableItemDrop(pickupDef.itemIndex);
		}
		if (pickupDef.equipmentIndex != EquipmentIndex.None)
		{
			EnableEquipmentDrop(pickupDef.equipmentIndex);
		}
	}

	[Server]
	public void EnableItemDrop(ItemIndex itemIndex)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.Run::EnableItemDrop(RoR2.ItemIndex)' called on client");
			return;
		}
		ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
		List<PickupIndex> list = null;
		bool flag = false;
		switch (itemDef.tier)
		{
		case ItemTier.Tier1:
			list = availableTier1DropList;
			break;
		case ItemTier.Tier2:
			list = availableTier2DropList;
			break;
		case ItemTier.Tier3:
			list = availableTier3DropList;
			break;
		case ItemTier.Lunar:
			list = availableLunarItemDropList;
			break;
		case ItemTier.Boss:
			list = availableBossDropList;
			break;
		case ItemTier.VoidTier1:
			list = availableVoidTier1DropList;
			break;
		case ItemTier.VoidTier2:
			list = availableVoidTier2DropList;
			break;
		case ItemTier.VoidTier3:
			list = availableVoidTier3DropList;
			break;
		case ItemTier.VoidBoss:
			list = availableVoidBossDropList;
			break;
		}
		PickupIndex pickupIndex = PickupCatalog.FindPickupIndex(itemIndex);
		if (list != null && pickupIndex != PickupIndex.none && !list.Contains(pickupIndex))
		{
			list.Add(pickupIndex);
			if (flag)
			{
				RefreshLunarCombinedDropList();
			}
			Run.onAvailablePickupsModified?.Invoke(this);
		}
	}

	[Server]
	public void EnableEquipmentDrop(EquipmentIndex equipmentIndex)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.Run::EnableEquipmentDrop(RoR2.EquipmentIndex)' called on client");
			return;
		}
		PickupIndex pickupIndex = PickupCatalog.FindPickupIndex(equipmentIndex);
		if (!(pickupIndex != PickupIndex.none))
		{
			return;
		}
		List<PickupIndex> list = null;
		bool flag = false;
		list = ((!PickupCatalog.GetPickupDef(pickupIndex).isLunar) ? availableEquipmentDropList : availableLunarEquipmentDropList);
		if (!list.Contains(pickupIndex))
		{
			list.Add(pickupIndex);
			if (flag)
			{
				RefreshLunarCombinedDropList();
			}
			Run.onAvailablePickupsModified?.Invoke(this);
		}
	}

	[Server]
	private void RefreshLunarCombinedDropList()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.Run::RefreshLunarCombinedDropList()' called on client");
			return;
		}
		availableLunarCombinedDropList.Clear();
		availableLunarCombinedDropList.AddRange(availableLunarEquipmentDropList);
		availableLunarCombinedDropList.AddRange(availableLunarItemDropList);
	}

	[ConCommand(commandName = "run_end", flags = ConVarFlags.SenderMustBeServer, helpText = "Ends the current run.")]
	private static void CCRunEnd(ConCommandArgs args)
	{
		if ((bool)instance)
		{
			UnityEngine.Object.Destroy(instance.gameObject);
		}
	}

	[ConCommand(commandName = "run_print_unlockables", flags = ConVarFlags.SenderMustBeServer, helpText = "Prints all unlockables available in this run.")]
	private static void CCRunPrintUnlockables(ConCommandArgs args)
	{
		if (!instance)
		{
			throw new ConCommandException("No run is currently in progress.");
		}
		List<string> list = new List<string>();
		foreach (UnlockableDef item in instance.unlockablesUnlockedByAnyUser)
		{
			list.Add(item.cachedName);
		}
	}

	[ConCommand(commandName = "run_print_seed", flags = ConVarFlags.None, helpText = "Prints the seed of the current run.")]
	private static void CCRunPrintSeed(ConCommandArgs args)
	{
		if (!instance)
		{
			throw new ConCommandException("No run is currently in progress.");
		}
		Debug.LogFormat("Current run seed: {0}", instance.seed);
	}

	[ConCommand(commandName = "run_set_stages_cleared", flags = (ConVarFlags.ExecuteOnServer | ConVarFlags.Cheat), helpText = "Sets the current number of stages cleared in the run.")]
	private static void CCRunSetStagesCleared(ConCommandArgs args)
	{
		if (!instance)
		{
			throw new ConCommandException("No run is currently in progress.");
		}
		instance.NetworkstageClearCount = args.GetArgInt(0);
	}

	public virtual void AdvanceStage(SceneDef nextScene)
	{
		if ((bool)Stage.instance)
		{
			Stage.instance.CompleteServer();
			SceneType sceneType = SceneCatalog.GetSceneDefForCurrentScene().sceneType;
			if (sceneType == SceneType.Stage || sceneType == SceneType.UntimedStage)
			{
				NetworkstageClearCount = stageClearCount + 1;
			}
		}
		GenerateStageRNG();
		RecalculateDifficultyCoefficent();
		NetworkManager.singleton.ServerChangeScene(nextScene.cachedName);
	}

	public void BeginGameOver([NotNull] GameEndingDef gameEndingDef)
	{
		if (isGameOverServer)
		{
			return;
		}
		if ((bool)Stage.instance && gameEndingDef.isWin)
		{
			Stage.instance.CompleteServer();
		}
		isGameOverServer = true;
		if (gameEndingDef.lunarCoinReward != 0)
		{
			for (int i = 0; i < NetworkUser.readOnlyInstancesList.Count; i++)
			{
				NetworkUser networkUser = NetworkUser.readOnlyInstancesList[i];
				if ((bool)networkUser && networkUser.isParticipating)
				{
					networkUser.AwardLunarCoins(gameEndingDef.lunarCoinReward);
				}
			}
		}
		StatManager.ForceUpdate();
		GameObject obj = UnityEngine.Object.Instantiate(gameOverPrefab);
		GameOverController component = obj.GetComponent<GameOverController>();
		component.SetRunReport(RunReport.Generate(this, gameEndingDef));
		Run.onServerGameOver?.Invoke(this, gameEndingDef);
		NetworkServer.Spawn(obj);
		isRunning = false;
		component.CallRpcClientGameOver();
	}

	public virtual void OnClientGameOver(RunReport runReport)
	{
		isRunning = false;
		Run.onClientGameOverGlobal?.Invoke(this, runReport);
	}

	public virtual void OverrideRuleChoices(RuleChoiceMask mustInclude, RuleChoiceMask mustExclude, ulong runSeed)
	{
		ForceChoice(mustInclude, mustExclude, "Misc.KeepMoneyBetweenStages.Off");
	}

	protected void ForceChoice(RuleChoiceMask mustInclude, RuleChoiceMask mustExclude, RuleChoiceDef choiceDef)
	{
		foreach (RuleChoiceDef choice in choiceDef.ruleDef.choices)
		{
			mustInclude[choice.globalIndex] = false;
			mustExclude[choice.globalIndex] = true;
		}
		mustInclude[choiceDef.globalIndex] = true;
		mustExclude[choiceDef.globalIndex] = false;
	}

	protected void ForceChoice(RuleChoiceMask mustInclude, RuleChoiceMask mustExclude, string choiceDefGlobalName)
	{
		ForceChoice(mustInclude, mustExclude, RuleCatalog.FindChoiceDef(choiceDefGlobalName));
	}

	public virtual Vector3 FindSafeTeleportPosition(CharacterBody characterBody, Transform targetDestination)
	{
		return FindSafeTeleportPosition(characterBody, targetDestination, float.NegativeInfinity, float.NegativeInfinity);
	}

	public virtual Vector3 FindSafeTeleportPosition(CharacterBody characterBody, Transform targetDestination, float idealMinDistance, float idealMaxDistance)
	{
		Vector3 vector = (targetDestination ? targetDestination.position : characterBody.transform.position);
		SpawnCard spawnCard = ScriptableObject.CreateInstance<SpawnCard>();
		spawnCard.hullSize = characterBody.hullClassification;
		spawnCard.nodeGraphType = MapNodeGroup.GraphType.Ground;
		spawnCard.prefab = LegacyResourcesAPI.Load<GameObject>("SpawnCards/HelperPrefab");
		Vector3 result = vector;
		GameObject gameObject = null;
		if (idealMaxDistance > 0f && idealMinDistance < idealMaxDistance)
		{
			gameObject = DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(spawnCard, new DirectorPlacementRule
			{
				placementMode = DirectorPlacementRule.PlacementMode.Approximate,
				minDistance = idealMinDistance,
				maxDistance = idealMaxDistance,
				position = vector
			}, RoR2Application.rng));
		}
		if (!gameObject)
		{
			gameObject = DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(spawnCard, new DirectorPlacementRule
			{
				placementMode = DirectorPlacementRule.PlacementMode.NearestNode,
				position = vector
			}, RoR2Application.rng));
			if ((bool)gameObject)
			{
				result = gameObject.transform.position;
			}
		}
		if ((bool)gameObject)
		{
			UnityEngine.Object.Destroy(gameObject);
		}
		UnityEngine.Object.Destroy(spawnCard);
		return result;
	}

	[Server]
	public void SetEventFlag(string name)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.Run::SetEventFlag(System.String)' called on client");
		}
		else
		{
			eventFlags.Add(name);
		}
	}

	[Server]
	public bool GetEventFlag(string name)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Boolean RoR2.Run::GetEventFlag(System.String)' called on client");
			return false;
		}
		return eventFlags.Contains(name);
	}

	[Server]
	public void ResetEventFlag(string name)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.Run::ResetEventFlag(System.String)' called on client");
		}
		else
		{
			eventFlags.Remove(name);
		}
	}

	public virtual bool ShouldAllowNonChampionBossSpawn()
	{
		return stageClearCount > 0;
	}

	public bool IsExpansionEnabled([NotNull] ExpansionDef expansionDef)
	{
		if (expansionDef.enabledChoice != null)
		{
			return ruleBook.IsChoiceActive(expansionDef.enabledChoice);
		}
		return false;
	}

	protected virtual void OnSeedSet()
	{
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			GeneratedNetworkCode._WriteNetworkGuid_None(writer, _uniqueId);
			GeneratedNetworkCode._WriteNetworkDateTime_None(writer, startTimeUtc);
			writer.Write(fixedTime);
			GeneratedNetworkCode._WriteRunStopwatch_Run(writer, runStopwatch);
			writer.WritePackedUInt32((uint)stageClearCount);
			writer.WritePackedUInt32((uint)selectedDifficultyInternal);
			return true;
		}
		bool flag = false;
		if ((base.syncVarDirtyBits & (true ? 1u : 0u)) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			GeneratedNetworkCode._WriteNetworkGuid_None(writer, _uniqueId);
		}
		if ((base.syncVarDirtyBits & 2u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			GeneratedNetworkCode._WriteNetworkDateTime_None(writer, startTimeUtc);
		}
		if ((base.syncVarDirtyBits & 4u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(fixedTime);
		}
		if ((base.syncVarDirtyBits & 8u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			GeneratedNetworkCode._WriteRunStopwatch_Run(writer, runStopwatch);
		}
		if ((base.syncVarDirtyBits & 0x10u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.WritePackedUInt32((uint)stageClearCount);
		}
		if ((base.syncVarDirtyBits & 0x20u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.WritePackedUInt32((uint)selectedDifficultyInternal);
		}
		if (!flag)
		{
			writer.WritePackedUInt32(base.syncVarDirtyBits);
		}
		return flag;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
		if (initialState)
		{
			_uniqueId = GeneratedNetworkCode._ReadNetworkGuid_None(reader);
			startTimeUtc = GeneratedNetworkCode._ReadNetworkDateTime_None(reader);
			fixedTime = reader.ReadSingle();
			runStopwatch = GeneratedNetworkCode._ReadRunStopwatch_Run(reader);
			stageClearCount = (int)reader.ReadPackedUInt32();
			selectedDifficultyInternal = (int)reader.ReadPackedUInt32();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			_uniqueId = GeneratedNetworkCode._ReadNetworkGuid_None(reader);
		}
		if (((uint)num & 2u) != 0)
		{
			startTimeUtc = GeneratedNetworkCode._ReadNetworkDateTime_None(reader);
		}
		if (((uint)num & 4u) != 0)
		{
			fixedTime = reader.ReadSingle();
		}
		if (((uint)num & 8u) != 0)
		{
			runStopwatch = GeneratedNetworkCode._ReadRunStopwatch_Run(reader);
		}
		if (((uint)num & 0x10u) != 0)
		{
			stageClearCount = (int)reader.ReadPackedUInt32();
		}
		if (((uint)num & 0x20u) != 0)
		{
			selectedDifficultyInternal = (int)reader.ReadPackedUInt32();
		}
	}

	public override void PreStartClient()
	{
	}
}
