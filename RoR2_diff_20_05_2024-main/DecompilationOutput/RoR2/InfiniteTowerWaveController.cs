using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using RoR2.HudOverlay;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[DisallowMultipleComponent]
public class InfiniteTowerWaveController : NetworkBehaviour
{
	[Serializable]
	public struct OverlayEntry
	{
		[SerializeField]
		[Tooltip("The overlay prefab to instantiate")]
		public GameObject prefab;

		[SerializeField]
		[Tooltip("The overaly prefab to instantiate")]
		public string hudChildName;
	}

	private const float minimumDistributedCreditsFraction = 0.1f;

	[SerializeField]
	[Tooltip("Should the enemies in this wave count as bosses?")]
	private bool isBossWave;

	[SerializeField]
	[Tooltip("The combat director for this wave")]
	protected CombatDirector combatDirector;

	[SerializeField]
	[Tooltip("The combat squad for this wave")]
	protected CombatSquad combatSquad;

	[SerializeField]
	[Tooltip("The base total number of credits to give to the CombatDirector for this wave.  Wave 1 gets this many credits.")]
	protected float baseCredits;

	[SerializeField]
	[Tooltip("The number of additional credits to give to the CombatDirector for each wave you've completed.  This doesn't affect Wave 1.")]
	protected float linearCreditsPerWave;

	[SerializeField]
	[Tooltip("The period (in seconds) over which we give the CombatDirector its credits.")]
	protected float wavePeriodSeconds;

	[SerializeField]
	[Tooltip("The number of seconds (after the last enemy is spawned) before the radius begins constricting")]
	protected float secondsBeforeSuddenDeath = 60f;

	[SerializeField]
	[Tooltip("If there's ever a period of this many seconds with no enemies, end the wave")]
	protected float secondsBeforeFailsafe = 60f;

	[Range(0f, 1f)]
	[SerializeField]
	[Tooltip("The zone radius percentage is reduced by this amount each second")]
	protected float suddenDeathRadiusConstrictingPerSecond = 0.05f;

	[SerializeField]
	[Tooltip("Broadcast this message at the beginning of sudden death")]
	protected string suddenDeathChatToken = "INFINITETOWER_SUDDEN_DEATH";

	[Range(0f, 1f)]
	[SerializeField]
	[Tooltip("The normalized fraction of the total credits to give to the CombatDirector immediately")]
	protected float immediateCreditsFraction;

	[SerializeField]
	[Tooltip("The maximum number of members in the combat squad before we stop giving the CombatDirector credits.  If the squad size is reduced below this number, we resume giving the director credits.")]
	protected int maxSquadSize;

	[SerializeField]
	[Tooltip("The time (in seconds) after completing the wave before the next wave begins")]
	public int secondsAfterWave;

	[SerializeField]
	[Tooltip("The time (in seconds) after completing the wave before the next wave begins")]
	protected float squadDefeatGracePeriod = 1f;

	[SerializeField]
	[Tooltip("The prefab to instantiate on the UI.")]
	protected GameObject uiPrefab;

	[SerializeField]
	[Tooltip("The overlays to add for this wave")]
	protected OverlayEntry[] overlayEntries;

	[SerializeField]
	[Tooltip("If true, convert all player gold to experience at the end of the wave")]
	protected bool convertGoldOnWaveFinish;

	[SerializeField]
	[Tooltip("The multiplier to use when converting gold to experience (only used if convertGoldOnWaveFinish is true).")]
	protected float goldToExpConversionRatio;

	[SerializeField]
	[Tooltip("The drop table to use for the end of wave rewards")]
	protected PickupDropTable rewardDropTable;

	[SerializeField]
	[Tooltip("Use this tier to get a pickup index for the reward.  The droplet's visuals will correspond to this.")]
	protected ItemTier rewardDisplayTier;

	[SerializeField]
	[Tooltip("The number of options to display when the player interacts with the reward pickup.")]
	protected int rewardOptionCount;

	[SerializeField]
	[Tooltip("The prefab to use for the reward pickup.")]
	protected GameObject rewardPickupPrefab;

	[SerializeField]
	[Tooltip("Where to spawn the reward droplets relative to the spawn target (the center of the safe ward).")]
	protected Vector3 rewardOffset;

	[SerializeField]
	[Tooltip("Broadcast this message at the beginning of the wave.")]
	protected string beginChatToken = "INFINITETOWER_WAVE_BEGIN";

	[SerializeField]
	[Tooltip("Play this sound at the beginning of the wave.")]
	protected string beginSoundString;

	[SerializeField]
	[Tooltip("Play this sound when all enemies are defeated.")]
	protected string onAllEnemiesDefeatedSoundString;

	[SyncVar]
	private float _totalWaveCredits;

	[SyncVar]
	private float _totalCreditsSpent;

	[SyncVar]
	private float _timerStart;

	[SyncVar]
	private float _failsafeTimer;

	[SyncVar]
	protected float _suddenDeathTimerStart;

	[SyncVar]
	private bool _isFinished;

	[SyncVar]
	private bool _isTimerActive;

	[SyncVar]
	private float _zoneRadiusPercentage = 1f;

	[SyncVar]
	protected int waveIndex;

	[SyncVar]
	private int squadCount;

	[SyncVar]
	private bool haveAllEnemiesBeenDefeated;

	protected float creditsPerSecond;

	protected GameObject uiInstance;

	protected bool hasEnabledEnemyIndicators;

	protected float squadDefeatTimer;

	private Inventory enemyInventory;

	private bool hasTimerExpired;

	protected GameObject spawnTarget;

	private Xoroshiro128Plus rng;

	private List<OverlayController> overlayControllerList = new List<OverlayController>();

	private bool hasNotifiedSuddenDeath;

	private bool hasPlayedEnemiesDefeatedSound;

	private int clearMemoryEveryXRounds = 5;

	public GameObject defaultEnemyIndicatorPrefab { get; set; }

	public bool isFinished
	{
		get
		{
			return _isFinished;
		}
		private set
		{
			Network_isFinished = value;
		}
	}

	public bool isTimerActive
	{
		get
		{
			return _isTimerActive;
		}
		private set
		{
			Network_isTimerActive = value;
		}
	}

	public float totalWaveCredits
	{
		get
		{
			return _totalWaveCredits;
		}
		protected set
		{
			Network_totalWaveCredits = value;
		}
	}

	public float totalCreditsSpent
	{
		get
		{
			return _totalCreditsSpent;
		}
		protected set
		{
			Network_totalCreditsSpent = value;
		}
	}

	public float zoneRadiusPercentage
	{
		get
		{
			return _zoneRadiusPercentage;
		}
		protected set
		{
			Network_zoneRadiusPercentage = value;
		}
	}

	public float secondsRemaining
	{
		get
		{
			if ((bool)Run.instance && _timerStart > 0f)
			{
				return Mathf.Max(0f, (float)secondsAfterWave - (Run.instance.GetRunStopwatch() - _timerStart));
			}
			return secondsAfterWave;
		}
	}

	public bool isInSuddenDeath
	{
		get
		{
			if ((bool)Run.instance && _suddenDeathTimerStart > 0f)
			{
				if (!haveAllEnemiesBeenDefeated && HasFullProgress())
				{
					return secondsBeforeSuddenDeath - (Run.instance.GetRunStopwatch() - _suddenDeathTimerStart) < 0f;
				}
				return false;
			}
			return false;
		}
	}

	public float Network_totalWaveCredits
	{
		get
		{
			return _totalWaveCredits;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref _totalWaveCredits, 1u);
		}
	}

	public float Network_totalCreditsSpent
	{
		get
		{
			return _totalCreditsSpent;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref _totalCreditsSpent, 2u);
		}
	}

	public float Network_timerStart
	{
		get
		{
			return _timerStart;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref _timerStart, 4u);
		}
	}

	public float Network_failsafeTimer
	{
		get
		{
			return _failsafeTimer;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref _failsafeTimer, 8u);
		}
	}

	public float Network_suddenDeathTimerStart
	{
		get
		{
			return _suddenDeathTimerStart;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref _suddenDeathTimerStart, 16u);
		}
	}

	public bool Network_isFinished
	{
		get
		{
			return _isFinished;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref _isFinished, 32u);
		}
	}

	public bool Network_isTimerActive
	{
		get
		{
			return _isTimerActive;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref _isTimerActive, 64u);
		}
	}

	public float Network_zoneRadiusPercentage
	{
		get
		{
			return _zoneRadiusPercentage;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref _zoneRadiusPercentage, 128u);
		}
	}

	public int NetworkwaveIndex
	{
		get
		{
			return waveIndex;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref waveIndex, 256u);
		}
	}

	public int NetworksquadCount
	{
		get
		{
			return squadCount;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref squadCount, 512u);
		}
	}

	public bool NetworkhaveAllEnemiesBeenDefeated
	{
		get
		{
			return haveAllEnemiesBeenDefeated;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref haveAllEnemiesBeenDefeated, 1024u);
		}
	}

	public event Action<InfiniteTowerWaveController> onAllEnemiesDefeatedServer;

	[Server]
	public virtual void Initialize(int waveIndex, Inventory enemyInventory, GameObject spawnTarget)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.InfiniteTowerWaveController::Initialize(System.Int32,RoR2.Inventory,UnityEngine.GameObject)' called on client");
			return;
		}
		totalWaveCredits = (baseCredits + linearCreditsPerWave * (float)(waveIndex - 1)) * Run.instance.difficultyCoefficient;
		creditsPerSecond = Mathf.Max(0.1f, 1f - immediateCreditsFraction) * totalWaveCredits / wavePeriodSeconds;
		if ((bool)combatDirector)
		{
			combatDirector.monsterCredit += immediateCreditsFraction * totalWaveCredits;
			combatDirector.currentSpawnTarget = spawnTarget;
		}
		NetworkwaveIndex = waveIndex;
		this.enemyInventory = enemyInventory;
		this.spawnTarget = spawnTarget;
		rng = new Xoroshiro128Plus((ulong)waveIndex ^ Run.instance.seed);
		if (!string.IsNullOrEmpty(beginChatToken))
		{
			Chat.SendBroadcastChat(new Chat.SimpleChatMessage
			{
				baseToken = beginChatToken
			});
		}
	}

	public void InstantiateUi(Transform uiRoot)
	{
		if ((bool)uiRoot && (bool)uiPrefab)
		{
			uiInstance = UnityEngine.Object.Instantiate(uiPrefab, uiRoot);
		}
		OverlayEntry[] array = overlayEntries;
		for (int i = 0; i < array.Length; i++)
		{
			OverlayEntry overlayEntry = array[i];
			OverlayCreationParams overlayCreationParams = default(OverlayCreationParams);
			overlayCreationParams.prefab = overlayEntry.prefab;
			overlayCreationParams.childLocatorEntry = overlayEntry.hudChildName;
			OverlayController item = HudOverlayManager.AddGlobalOverlay(overlayCreationParams);
			overlayControllerList.Add(item);
		}
	}

	public void PlayBeginSound()
	{
		Util.PlaySound(beginSoundString, RoR2Application.instance.gameObject);
	}

	public void PlayAllEnemiesDefeatedSound()
	{
		hasPlayedEnemiesDefeatedSound = true;
		Util.PlaySound(onAllEnemiesDefeatedSoundString, RoR2Application.instance.gameObject);
	}

	public int GetSquadCount()
	{
		if (NetworkServer.active)
		{
			if ((bool)combatSquad)
			{
				return combatSquad.memberCount;
			}
			return 0;
		}
		return squadCount;
	}

	public virtual float GetNormalizedProgress()
	{
		if (totalWaveCredits != 0f)
		{
			return totalCreditsSpent / totalWaveCredits;
		}
		return 1f;
	}

	public virtual bool HasFullProgress()
	{
		return totalCreditsSpent >= totalWaveCredits;
	}

	[Server]
	public virtual void ForceFinish()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.InfiniteTowerWaveController::ForceFinish()' called on client");
			return;
		}
		KillSquad();
		if ((bool)combatDirector)
		{
			combatDirector.monsterCredit = 0f;
		}
		MarkAsFinished();
	}

	[Server]
	private void KillSquad()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.InfiniteTowerWaveController::KillSquad()' called on client");
		}
		else
		{
			if (!combatSquad)
			{
				return;
			}
			foreach (CharacterMaster item in new List<CharacterMaster>(combatSquad.readOnlyMembersList))
			{
				item.TrueKill();
			}
		}
	}

	[Server]
	protected void MarkAsFinished()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.InfiniteTowerWaveController::MarkAsFinished()' called on client");
			return;
		}
		isFinished = true;
		OnFinishedServer();
	}

	[Server]
	protected void StartTimer()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.InfiniteTowerWaveController::StartTimer()' called on client");
			return;
		}
		Network_isTimerActive = true;
		Network_timerStart = Run.instance.GetRunStopwatch();
	}

	[Server]
	protected virtual void OnAllEnemiesDefeatedServer()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.InfiniteTowerWaveController::OnAllEnemiesDefeatedServer()' called on client");
			return;
		}
		DropRewards();
		this.onAllEnemiesDefeatedServer?.Invoke(this);
	}

	[Server]
	protected virtual void OnTimerExpire()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.InfiniteTowerWaveController::OnTimerExpire()' called on client");
		}
		else
		{
			MarkAsFinished();
		}
	}

	[Server]
	protected virtual void OnFinishedServer()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.InfiniteTowerWaveController::OnFinishedServer()' called on client");
		}
		else
		{
			if (!convertGoldOnWaveFinish)
			{
				return;
			}
			ReadOnlyCollection<PlayerCharacterMasterController> instances = PlayerCharacterMasterController.instances;
			for (int i = 0; i < instances.Count; i++)
			{
				CharacterMaster component = instances[i].gameObject.GetComponent<CharacterMaster>();
				uint num = Math.Max(component.money, (uint)Mathf.CeilToInt(component.money));
				ulong num2 = (ulong)((float)num * goldToExpConversionRatio / (float)instances.Count);
				component.money -= num;
				GameObject bodyObject = component.GetBodyObject();
				if ((bool)bodyObject)
				{
					ExperienceManager.instance.AwardExperience(base.transform.position, bodyObject.GetComponent<CharacterBody>(), num2);
				}
				else
				{
					TeamManager.instance.GiveTeamExperience(component.teamIndex, num2);
				}
			}
		}
	}

	private void FixedUpdate()
	{
		if (haveAllEnemiesBeenDefeated && !hasPlayedEnemiesDefeatedSound)
		{
			PlayAllEnemiesDefeatedSound();
			if (waveIndex % clearMemoryEveryXRounds == 0)
			{
				Resources.UnloadUnusedAssets();
			}
		}
		if (NetworkServer.active)
		{
			if ((bool)combatDirector)
			{
				NetworksquadCount = GetSquadCount();
				Network_totalCreditsSpent = combatDirector.totalCreditsSpent;
			}
			if (!isFinished)
			{
				if ((bool)combatSquad && combatSquad.memberCount == 0 && !haveAllEnemiesBeenDefeated)
				{
					Network_failsafeTimer = _failsafeTimer + Time.fixedDeltaTime;
					if (_failsafeTimer > secondsBeforeFailsafe)
					{
						Debug.LogError($"Failsafe detected!  Ending wave {waveIndex}");
						totalWaveCredits = 0f;
						if ((bool)combatDirector)
						{
							combatDirector.monsterCredit = 0f;
						}
					}
				}
				else
				{
					Network_failsafeTimer = 0f;
				}
				if (isInSuddenDeath)
				{
					if (!hasNotifiedSuddenDeath)
					{
						if (!string.IsNullOrEmpty(suddenDeathChatToken))
						{
							Chat.SendBroadcastChat(new Chat.SimpleChatMessage
							{
								baseToken = suddenDeathChatToken
							});
						}
						hasNotifiedSuddenDeath = true;
					}
					Network_zoneRadiusPercentage = Math.Max(0f, _zoneRadiusPercentage - suddenDeathRadiusConstrictingPerSecond * Time.fixedDeltaTime);
				}
				else
				{
					Network_zoneRadiusPercentage = 1f;
				}
				if (!isTimerActive)
				{
					if ((bool)combatDirector)
					{
						if (combatDirector.totalCreditsSpent < totalWaveCredits)
						{
							if (combatSquad.memberCount < maxSquadSize)
							{
								float num = Time.fixedDeltaTime * creditsPerSecond;
								combatDirector.monsterCredit += num;
							}
						}
						else
						{
							if (_suddenDeathTimerStart == 0f)
							{
								Network_suddenDeathTimerStart = Run.instance.GetRunStopwatch();
							}
							combatDirector.monsterCredit = 0f;
							if (combatSquad.memberCount == 0)
							{
								if (squadDefeatTimer <= 0f)
								{
									NetworkhaveAllEnemiesBeenDefeated = true;
									StartTimer();
									OnAllEnemiesDefeatedServer();
								}
								else
								{
									squadDefeatTimer -= Time.fixedDeltaTime;
								}
							}
						}
					}
				}
				else
				{
					Network_zoneRadiusPercentage = 1f;
					if (!hasTimerExpired && secondsRemaining <= 0f)
					{
						hasTimerExpired = true;
						OnTimerExpire();
					}
				}
			}
			else
			{
				Network_zoneRadiusPercentage = 1f;
			}
		}
		if (hasEnabledEnemyIndicators || !combatSquad || !HasFullProgress())
		{
			return;
		}
		hasEnabledEnemyIndicators = true;
		foreach (CharacterMaster readOnlyMembers in combatSquad.readOnlyMembersList)
		{
			RequestIndicatorForMaster(readOnlyMembers);
		}
	}

	private void OnDestroy()
	{
		if ((bool)uiInstance)
		{
			UnityEngine.Object.Destroy(uiInstance);
		}
		foreach (OverlayController overlayController in overlayControllerList)
		{
			HudOverlayManager.RemoveGlobalOverlay(overlayController);
		}
	}

	private void OnEnable()
	{
		if ((bool)combatSquad)
		{
			combatSquad.onMemberDiscovered += OnCombatSquadMemberDiscovered;
		}
		squadDefeatTimer = squadDefeatGracePeriod;
	}

	private void OnDisable()
	{
		if ((bool)combatSquad)
		{
			combatSquad.onMemberDiscovered -= OnCombatSquadMemberDiscovered;
		}
	}

	protected virtual void OnCombatSquadMemberDiscovered(CharacterMaster master)
	{
		if (hasEnabledEnemyIndicators)
		{
			RequestIndicatorForMaster(master);
		}
		squadDefeatTimer = squadDefeatGracePeriod;
		if (NetworkServer.active)
		{
			master.inventory.AddItemsFrom(enemyInventory);
		}
		master.isBoss = isBossWave;
	}

	protected virtual void RequestIndicatorForMaster(CharacterMaster master)
	{
		GameObject bodyObject = master.GetBodyObject();
		if ((bool)bodyObject)
		{
			TeamComponent component = bodyObject.GetComponent<TeamComponent>();
			if ((bool)component)
			{
				component.RequestDefaultIndicator(defaultEnemyIndicatorPrefab);
			}
		}
	}

	[Server]
	private void DropRewards()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.InfiniteTowerWaveController::DropRewards()' called on client");
			return;
		}
		int participatingPlayerCount = Run.instance.participatingPlayerCount;
		if (participatingPlayerCount > 0 && (bool)spawnTarget && (bool)rewardDropTable)
		{
			int num = participatingPlayerCount;
			float angle = 360f / (float)num;
			Vector3 vector = Quaternion.AngleAxis(UnityEngine.Random.Range(0, 360), Vector3.up) * (Vector3.up * 40f + Vector3.forward * 5f);
			Quaternion quaternion = Quaternion.AngleAxis(angle, Vector3.up);
			Vector3 position = spawnTarget.transform.position + rewardOffset;
			int num2 = 0;
			while (num2 < num)
			{
				GenericPickupController.CreatePickupInfo createPickupInfo = default(GenericPickupController.CreatePickupInfo);
				createPickupInfo.pickupIndex = PickupCatalog.FindPickupIndex(rewardDisplayTier);
				createPickupInfo.pickerOptions = PickupPickerController.GenerateOptionsFromDropTable(rewardOptionCount, rewardDropTable, rng);
				createPickupInfo.rotation = Quaternion.identity;
				createPickupInfo.prefabOverride = rewardPickupPrefab;
				createPickupInfo.position = position;
				GenericPickupController.CreatePickupInfo pickupInfo = createPickupInfo;
				PickupDropletController.CreatePickupDroplet(pickupInfo, pickupInfo.position, vector);
				num2++;
				vector = quaternion * vector;
			}
		}
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.Write(_totalWaveCredits);
			writer.Write(_totalCreditsSpent);
			writer.Write(_timerStart);
			writer.Write(_failsafeTimer);
			writer.Write(_suddenDeathTimerStart);
			writer.Write(_isFinished);
			writer.Write(_isTimerActive);
			writer.Write(_zoneRadiusPercentage);
			writer.WritePackedUInt32((uint)waveIndex);
			writer.WritePackedUInt32((uint)squadCount);
			writer.Write(haveAllEnemiesBeenDefeated);
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
			writer.Write(_totalWaveCredits);
		}
		if ((base.syncVarDirtyBits & 2u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(_totalCreditsSpent);
		}
		if ((base.syncVarDirtyBits & 4u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(_timerStart);
		}
		if ((base.syncVarDirtyBits & 8u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(_failsafeTimer);
		}
		if ((base.syncVarDirtyBits & 0x10u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(_suddenDeathTimerStart);
		}
		if ((base.syncVarDirtyBits & 0x20u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(_isFinished);
		}
		if ((base.syncVarDirtyBits & 0x40u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(_isTimerActive);
		}
		if ((base.syncVarDirtyBits & 0x80u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(_zoneRadiusPercentage);
		}
		if ((base.syncVarDirtyBits & 0x100u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.WritePackedUInt32((uint)waveIndex);
		}
		if ((base.syncVarDirtyBits & 0x200u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.WritePackedUInt32((uint)squadCount);
		}
		if ((base.syncVarDirtyBits & 0x400u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(haveAllEnemiesBeenDefeated);
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
			_totalWaveCredits = reader.ReadSingle();
			_totalCreditsSpent = reader.ReadSingle();
			_timerStart = reader.ReadSingle();
			_failsafeTimer = reader.ReadSingle();
			_suddenDeathTimerStart = reader.ReadSingle();
			_isFinished = reader.ReadBoolean();
			_isTimerActive = reader.ReadBoolean();
			_zoneRadiusPercentage = reader.ReadSingle();
			waveIndex = (int)reader.ReadPackedUInt32();
			squadCount = (int)reader.ReadPackedUInt32();
			haveAllEnemiesBeenDefeated = reader.ReadBoolean();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			_totalWaveCredits = reader.ReadSingle();
		}
		if (((uint)num & 2u) != 0)
		{
			_totalCreditsSpent = reader.ReadSingle();
		}
		if (((uint)num & 4u) != 0)
		{
			_timerStart = reader.ReadSingle();
		}
		if (((uint)num & 8u) != 0)
		{
			_failsafeTimer = reader.ReadSingle();
		}
		if (((uint)num & 0x10u) != 0)
		{
			_suddenDeathTimerStart = reader.ReadSingle();
		}
		if (((uint)num & 0x20u) != 0)
		{
			_isFinished = reader.ReadBoolean();
		}
		if (((uint)num & 0x40u) != 0)
		{
			_isTimerActive = reader.ReadBoolean();
		}
		if (((uint)num & 0x80u) != 0)
		{
			_zoneRadiusPercentage = reader.ReadSingle();
		}
		if (((uint)num & 0x100u) != 0)
		{
			waveIndex = (int)reader.ReadPackedUInt32();
		}
		if (((uint)num & 0x200u) != 0)
		{
			squadCount = (int)reader.ReadPackedUInt32();
		}
		if (((uint)num & 0x400u) != 0)
		{
			haveAllEnemiesBeenDefeated = reader.ReadBoolean();
		}
	}

	public override void PreStartClient()
	{
	}
}
