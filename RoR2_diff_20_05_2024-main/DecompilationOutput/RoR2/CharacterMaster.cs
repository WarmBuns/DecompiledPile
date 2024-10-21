using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using EntityStates;
using EntityStates.GummyClone;
using RoR2.CharacterAI;
using RoR2.Items;
using RoR2.Stats;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Serialization;

namespace RoR2;

[DisallowMultipleComponent]
[RequireComponent(typeof(Inventory))]
[RequireComponent(typeof(MinionOwnership))]
public class CharacterMaster : NetworkBehaviour
{
	private static class CommonAssets
	{
		public static GameObject goldOnStageStartEffect;

		public static void Resolve()
		{
			AsyncOperationHandle<GameObject> asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/Effects/GoldOnStageStartCoinGain");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				goldOnStageStartEffect = x.Result;
			};
		}
	}

	public enum SEEKER_REVIVE_STATUS
	{
		UNUSED,
		USED,
		BURNED
	}

	[HideInInspector]
	[SerializeField]
	[Tooltip("This is assigned to the prefab automatically by MasterCatalog at runtime. Do not set this value manually.")]
	private int _masterIndex;

	[Tooltip("The prefab of this character's body.")]
	public GameObject bodyPrefab;

	[Tooltip("Whether or not to spawn the body at the position of this manager object as soon as Start runs.")]
	public bool spawnOnStart;

	[FormerlySerializedAs("teamIndex")]
	[SerializeField]
	[Tooltip("The team of the body.")]
	private TeamIndex _teamIndex;

	public UnityEvent onBodyDeath;

	[Tooltip("Whether or not to destroy this master when the body dies.")]
	public bool destroyOnBodyDeath = true;

	private static List<CharacterMaster> instancesList;

	private static ReadOnlyCollection<CharacterMaster> _readOnlyInstancesList;

	private BaseAI[] aiComponents;

	private const uint bodyDirtyBit = 1u;

	private const uint moneyDirtyBit = 2u;

	private const uint survivalTimeDirtyBit = 4u;

	private const uint teamDirtyBit = 8u;

	private const uint loadoutDirtyBit = 16u;

	private const uint miscFlagsDirtyBit = 32u;

	private const uint voidCoinsDirtyBit = 64u;

	private const uint allDirtyBits = 127u;

	public readonly Loadout loadout = new Loadout();

	private NetworkInstanceId _bodyInstanceId = NetworkInstanceId.Invalid;

	private GameObject resolvedBodyInstance;

	private bool bodyResolved;

	private ulong beadExperience;

	private int numberOfBeadStatsGained;

	private uint oldBeadLevel = 1u;

	private uint newBeadLevel;

	private ulong beadXPNeededForCurrentLevel = 20uL;

	private uint _money;

	public uint lssMoneyExp;

	private uint _voidCoins;

	public bool isBoss;

	private Xoroshiro128Plus cloverVoidRng;

	[NonSerialized]
	private List<DeployableInfo> deployablesList;

	public bool preventGameOver = true;

	private Vector3 deathAimVector = Vector3.zero;

	private bool killedByUnsafeArea;

	private const float respawnDelayDuration = 2f;

	private float _internalSurvivalTime;

	private BodyIndex killerBodyIndex = BodyIndex.None;

	private bool preventRespawnUntilNextStageServer;

	private SEEKER_REVIVE_STATUS seekerUsedRevive;

	[HideInInspector]
	public GameObject devotionInventoryPrefab;

	[HideInInspector]
	public DevotionInventoryController devotionInventoryUpdaterRef;

	[HideInInspector]
	public ItemIndex devotionItem;

	[HideInInspector]
	public int devotedEvolutionTracker;

	[HideInInspector]
	public bool isDevotedMinion;

	[HideInInspector]
	public Interactor summonerRef;

	private bool godMode;

	private uint lostBodyToDeathFlag = 1u;

	private uint _miscFlags;

	private static int kRpcRpcOnPlayerBodyDamaged;

	private static int kCmdCmdRespawn;

	private static int kRpcRpcSetSeekerRevive;

	public MasterCatalog.MasterIndex masterIndex
	{
		get
		{
			return (MasterCatalog.MasterIndex)_masterIndex;
		}
		set
		{
			_masterIndex = (int)value;
		}
	}

	public NetworkIdentity networkIdentity { get; private set; }

	public bool hasEffectiveAuthority { get; private set; }

	public TeamIndex teamIndex
	{
		get
		{
			return _teamIndex;
		}
		set
		{
			if (_teamIndex != value)
			{
				_teamIndex = value;
				if (NetworkServer.active)
				{
					SetDirtyBit(8u);
				}
			}
		}
	}

	public static ReadOnlyCollection<CharacterMaster> readOnlyInstancesList => _readOnlyInstancesList;

	public Inventory inventory { get; private set; }

	public PlayerCharacterMasterController playerCharacterMasterController { get; private set; }

	public PlayerStatsComponent playerStatsComponent { get; private set; }

	public MinionOwnership minionOwnership { get; private set; }

	private NetworkInstanceId bodyInstanceId
	{
		get
		{
			return _bodyInstanceId;
		}
		set
		{
			if (!(value == _bodyInstanceId))
			{
				SetDirtyBit(1u);
				_bodyInstanceId = value;
			}
		}
	}

	public BodyIndex backupBodyIndex { get; private set; }

	private GameObject bodyInstanceObject
	{
		get
		{
			if (!bodyResolved)
			{
				resolvedBodyInstance = Util.FindNetworkObject(bodyInstanceId);
				if ((bool)resolvedBodyInstance)
				{
					bodyResolved = true;
					StoreBackupBodyIndex();
				}
			}
			return resolvedBodyInstance;
		}
		set
		{
			NetworkInstanceId invalid = NetworkInstanceId.Invalid;
			resolvedBodyInstance = null;
			bodyResolved = true;
			if ((bool)value)
			{
				NetworkIdentity component = value.GetComponent<NetworkIdentity>();
				if ((bool)component)
				{
					invalid = component.netId;
					resolvedBodyInstance = value;
					StoreBackupBodyIndex();
				}
			}
			bodyInstanceId = invalid;
		}
	}

	public uint money
	{
		get
		{
			return _money;
		}
		set
		{
			if (value != _money)
			{
				SetDirtyBit(2u);
				_money = value;
			}
		}
	}

	public uint voidCoins
	{
		get
		{
			return _voidCoins;
		}
		set
		{
			if (value != _voidCoins)
			{
				SetDirtyBit(64u);
				_voidCoins = value;
			}
		}
	}

	public float luck { get; set; }

	public bool hasBody => bodyInstanceObject;

	public Vector3 deathFootPosition { get; private set; } = Vector3.zero;

	private float internalSurvivalTime
	{
		get
		{
			return _internalSurvivalTime;
		}
		set
		{
			if (value != _internalSurvivalTime)
			{
				SetDirtyBit(4u);
				_internalSurvivalTime = value;
			}
		}
	}

	public float currentLifeStopwatch
	{
		get
		{
			if (internalSurvivalTime <= 0f)
			{
				return 0f - internalSurvivalTime;
			}
			if ((bool)Run.instance)
			{
				return Run.instance.GetRunStopwatch() - internalSurvivalTime;
			}
			return 0f;
		}
	}

	private uint miscFlags
	{
		get
		{
			return _miscFlags;
		}
		set
		{
			if (value != _miscFlags)
			{
				_miscFlags = value;
				if (NetworkServer.active)
				{
					SetDirtyBit(32u);
				}
			}
		}
	}

	public bool lostBodyToDeath
	{
		get
		{
			return (miscFlags & lostBodyToDeathFlag) != 0;
		}
		private set
		{
			if (value)
			{
				miscFlags |= lostBodyToDeathFlag;
			}
			else
			{
				miscFlags &= ~lostBodyToDeathFlag;
			}
		}
	}

	public static event Action<CharacterMaster> onStartGlobal;

	public static event Action<CharacterMaster> onCharacterMasterDiscovered;

	public static event Action<CharacterMaster> onCharacterMasterLost;

	public event Action<CharacterBody> onBodyStart;

	public event Action<CharacterBody> onBodyDestroyed;

	public event Action<PlayerCharacterMasterController> onPlayerBodyDamaged;

	private void UpdateAuthority()
	{
		hasEffectiveAuthority = Util.HasEffectiveAuthority(networkIdentity);
	}

	public override void OnStartAuthority()
	{
		base.OnStartAuthority();
		UpdateAuthority();
	}

	public override void OnStopAuthority()
	{
		UpdateAuthority();
		base.OnStopAuthority();
	}

	[Server]
	public void SetLoadoutServer(Loadout newLoadout)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.CharacterMaster::SetLoadoutServer(RoR2.Loadout)' called on client");
			return;
		}
		newLoadout.Copy(loadout);
		SetDirtyBit(16u);
	}

	private void StoreBackupBodyIndex()
	{
		if ((bool)resolvedBodyInstance)
		{
			CharacterBody component = resolvedBodyInstance.GetComponent<CharacterBody>();
			if ((bool)component)
			{
				backupBodyIndex = component.bodyIndex;
			}
		}
	}

	private void OnSyncBodyInstanceId(NetworkInstanceId value)
	{
		resolvedBodyInstance = null;
		bodyResolved = value == NetworkInstanceId.Invalid;
		_bodyInstanceId = value;
	}

	public void TrackBeadExperience(ulong amount)
	{
		if (!inventory)
		{
			return;
		}
		int itemCount = inventory.GetItemCount(DLC2Content.Items.ExtraStatsOnLevelUp);
		if (itemCount <= 0)
		{
			return;
		}
		ulong num = amount * (ulong)(0.25f * (float)(itemCount - 1));
		ulong num2 = amount * (ulong)(0.025f * (float)numberOfBeadStatsGained);
		if (num2 >= num + amount)
		{
			num2 = (ulong)(0.8f * (float)(num + amount));
		}
		beadExperience += amount + num - num2;
		newBeadLevel = TeamManager.instance.FindLevelForBeadExperience(beadExperience);
		if (beadExperience >= beadXPNeededForCurrentLevel)
		{
			CharacterBody body = GetBody();
			body.SetBuffCount(DLC2Content.Buffs.ExtraStatsOnLevelUpBuff.buffIndex, (int)oldBeadLevel);
			float num3 = body.GetBuffCount(DLC2Content.Buffs.ExtraStatsOnLevelUpBuff);
			float num4 = 1f;
			switch (body.hullClassification)
			{
			case HullClassification.Golem:
				num4 = 2f;
				break;
			case HullClassification.BeetleQueen:
				num4 = 3f;
				break;
			}
			num4 += num3 / (num3 * 0.25f + 1f);
			EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ExtraStatsOnLevelUpEffect"), new EffectData
			{
				origin = body.footPosition,
				scale = num4
			}, transmit: true);
			Util.PlaySound("Play_item_proc_extraStatsOnLevelUp", body.gameObject);
			oldBeadLevel = newBeadLevel;
			beadXPNeededForCurrentLevel = TeamManager.GetExperienceForLevel(oldBeadLevel + 1);
		}
	}

	public void OnBeadReset(bool gainedStats)
	{
		if (gainedStats)
		{
			numberOfBeadStatsGained += (int)oldBeadLevel;
		}
		oldBeadLevel = 1u;
		beadExperience = 0uL;
		beadXPNeededForCurrentLevel = 20uL;
	}

	[Server]
	public void GiveExperience(ulong amount)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.CharacterMaster::GiveExperience(System.UInt64)' called on client");
		}
		else
		{
			TeamManager.instance.GiveTeamExperience(teamIndex, amount);
		}
	}

	public void GiveMoney(uint amount)
	{
		int itemCount = inventory.GetItemCount(DLC2Content.Items.OnLevelUpFreeUnlock);
		int num = TeamManager.LongstandingSolitudesInParty();
		if (itemCount > 0)
		{
			double num2 = TeamManager.GetExperienceForLevel((uint)GetBody().level + 1);
			uint difficultyScaledCost = (uint)Run.instance.GetDifficultyScaledCost(25, Run.instance.difficultyCoefficient);
			lssMoneyExp += amount;
			if ((float)lssMoneyExp >= (float)(difficultyScaledCost * num) + GetBody().level)
			{
				GiveExperience((ulong)(num2 / (double)(10 + (itemCount - 1) * 15)));
				lssMoneyExp = 0u;
			}
		}
		else
		{
			money += amount;
			StatManager.OnGoldCollected(this, amount);
		}
	}

	public void GiveVoidCoins(uint amount)
	{
		voidCoins += amount;
	}

	public int GetDeployableSameSlotLimit(DeployableSlot slot)
	{
		int result = 0;
		int num = 1;
		if (RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.swarmsArtifactDef))
		{
			num = 2;
		}
		switch (slot)
		{
		case DeployableSlot.EngiMine:
			result = 4;
			if ((bool)bodyInstanceObject)
			{
				result = bodyInstanceObject.GetComponent<SkillLocator>().secondary.maxStock;
			}
			break;
		case DeployableSlot.EngiTurret:
			result = ((inventory.GetItemCount(DLC1Content.Items.EquipmentMagazineVoid) <= 0) ? 2 : 3);
			break;
		case DeployableSlot.BeetleGuardAlly:
			result = inventory.GetItemCount(RoR2Content.Items.BeetleGland) * num;
			break;
		case DeployableSlot.EngiBubbleShield:
			result = 1;
			break;
		case DeployableSlot.LoaderPylon:
			result = 3;
			break;
		case DeployableSlot.EngiSpiderMine:
			result = 4;
			if ((bool)bodyInstanceObject)
			{
				result = bodyInstanceObject.GetComponent<SkillLocator>().secondary.maxStock;
			}
			break;
		case DeployableSlot.RoboBallMini:
			result = 3;
			break;
		case DeployableSlot.ParentPodAlly:
			result = inventory.GetItemCount(JunkContent.Items.Incubator) * num;
			break;
		case DeployableSlot.ParentAlly:
			result = inventory.GetItemCount(JunkContent.Items.Incubator) * num;
			break;
		case DeployableSlot.PowerWard:
			result = 1;
			break;
		case DeployableSlot.CrippleWard:
			result = 5;
			break;
		case DeployableSlot.DeathProjectile:
			result = 3;
			break;
		case DeployableSlot.RoboBallRedBuddy:
		case DeployableSlot.RoboBallGreenBuddy:
			result = num;
			break;
		case DeployableSlot.GummyClone:
			result = 3;
			break;
		case DeployableSlot.LunarSunBomb:
			result = LunarSunBehavior.GetMaxProjectiles(inventory);
			break;
		case DeployableSlot.VendingMachine:
			result = 1;
			break;
		case DeployableSlot.VoidMegaCrabItem:
			result = VoidMegaCrabItemBehavior.GetMaxProjectiles(inventory);
			break;
		case DeployableSlot.DroneWeaponsDrone:
			result = 1;
			break;
		case DeployableSlot.MinorConstructOnKill:
			result = inventory.GetItemCount(DLC1Content.Items.MinorConstructOnKill) * 4;
			break;
		case DeployableSlot.CaptainSupplyDrop:
			result = 2;
			break;
		}
		return result;
	}

	[Server]
	public void AddDeployable(Deployable deployable, DeployableSlot slot)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.CharacterMaster::AddDeployable(RoR2.Deployable,RoR2.DeployableSlot)' called on client");
			return;
		}
		if ((bool)deployable.ownerMaster)
		{
			Debug.LogErrorFormat("Attempted to add deployable {0} which already belongs to master {1} to master {2}.", deployable.gameObject, deployable.ownerMaster.gameObject, base.gameObject);
		}
		if (deployablesList == null)
		{
			deployablesList = new List<DeployableInfo>();
		}
		int num = 0;
		int deployableSameSlotLimit = GetDeployableSameSlotLimit(slot);
		for (int num2 = deployablesList.Count - 1; num2 >= 0; num2--)
		{
			if (deployablesList[num2].slot == slot)
			{
				num++;
				if (num >= deployableSameSlotLimit)
				{
					Deployable deployable2 = deployablesList[num2].deployable;
					deployablesList.RemoveAt(num2);
					deployable2.ownerMaster = null;
					deployable2.onUndeploy.Invoke();
				}
			}
		}
		deployablesList.Add(new DeployableInfo
		{
			deployable = deployable,
			slot = slot
		});
		deployable.ownerMaster = this;
	}

	[Server]
	public int GetDeployableCount(DeployableSlot slot)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Int32 RoR2.CharacterMaster::GetDeployableCount(RoR2.DeployableSlot)' called on client");
			return 0;
		}
		if (deployablesList == null)
		{
			return 0;
		}
		int num = 0;
		for (int num2 = deployablesList.Count - 1; num2 >= 0; num2--)
		{
			if (deployablesList[num2].slot == slot)
			{
				num++;
			}
		}
		return num;
	}

	[Server]
	public bool IsDeployableLimited(DeployableSlot slot)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Boolean RoR2.CharacterMaster::IsDeployableLimited(RoR2.DeployableSlot)' called on client");
			return false;
		}
		return GetDeployableCount(slot) >= GetDeployableSameSlotLimit(slot);
	}

	[Server]
	public void RemoveDeployable(Deployable deployable)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.CharacterMaster::RemoveDeployable(RoR2.Deployable)' called on client");
		}
		else
		{
			if (deployablesList == null || deployable.ownerMaster != this)
			{
				return;
			}
			for (int num = deployablesList.Count - 1; num >= 0; num--)
			{
				if (deployablesList[num].deployable == deployable)
				{
					deployablesList.RemoveAt(num);
				}
			}
			deployable.ownerMaster = null;
		}
	}

	[Server]
	public bool IsDeployableSlotAvailable(DeployableSlot deployableSlot)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Boolean RoR2.CharacterMaster::IsDeployableSlotAvailable(RoR2.DeployableSlot)' called on client");
			return false;
		}
		return GetDeployableCount(deployableSlot) < GetDeployableSameSlotLimit(deployableSlot);
	}

	[Server]
	public CharacterBody SpawnBody(Vector3 position, Quaternion rotation)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'RoR2.CharacterBody RoR2.CharacterMaster::SpawnBody(UnityEngine.Vector3,UnityEngine.Quaternion)' called on client");
			return null;
		}
		if ((bool)bodyInstanceObject)
		{
			Debug.LogError("Character cannot have more than one body at this time.");
			return null;
		}
		if (!bodyPrefab)
		{
			Debug.LogErrorFormat("Attempted to spawn body of character master {0} with no body prefab.", base.gameObject);
		}
		if (!bodyPrefab.GetComponent<CharacterBody>())
		{
			Debug.LogErrorFormat("Attempted to spawn body of character master {0} with a body prefab that has no {1} component attached.", base.gameObject, typeof(CharacterBody).Name);
		}
		bool flag = bodyPrefab.GetComponent<CharacterDirection>();
		GameObject gameObject = UnityEngine.Object.Instantiate(bodyPrefab, position, flag ? Quaternion.identity : rotation);
		CharacterBody component = gameObject.GetComponent<CharacterBody>();
		component.masterObject = base.gameObject;
		component.teamComponent.teamIndex = teamIndex;
		component.SetLoadoutServer(loadout);
		if (flag)
		{
			CharacterDirection component2 = gameObject.GetComponent<CharacterDirection>();
			float y = rotation.eulerAngles.y;
			component2.yaw = y;
		}
		NetworkConnection clientAuthorityOwner = GetComponent<NetworkIdentity>().clientAuthorityOwner;
		if (clientAuthorityOwner != null)
		{
			clientAuthorityOwner.isReady = true;
			NetworkServer.SpawnWithClientAuthority(gameObject, clientAuthorityOwner);
		}
		else
		{
			NetworkServer.Spawn(gameObject);
		}
		bodyInstanceObject = gameObject;
		Run.instance.OnServerCharacterBodySpawned(component);
		return component;
	}

	[Server]
	public void DestroyBody()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.CharacterMaster::DestroyBody()' called on client");
		}
		else if ((bool)bodyInstanceObject)
		{
			CharacterBody body = GetBody();
			UnityEngine.Object.Destroy(bodyInstanceObject);
			OnBodyDestroyed(body);
			bodyInstanceObject = null;
		}
	}

	public GameObject GetBodyObject()
	{
		return bodyInstanceObject;
	}

	public CharacterBody GetBody()
	{
		GameObject bodyObject = GetBodyObject();
		if (!bodyObject)
		{
			return null;
		}
		return bodyObject.GetComponent<CharacterBody>();
	}

	private void Awake()
	{
		networkIdentity = GetComponent<NetworkIdentity>();
		inventory = GetComponent<Inventory>();
		aiComponents = (NetworkServer.active ? GetComponents<BaseAI>() : Array.Empty<BaseAI>());
		playerCharacterMasterController = GetComponent<PlayerCharacterMasterController>();
		playerStatsComponent = GetComponent<PlayerStatsComponent>();
		minionOwnership = GetComponent<MinionOwnership>();
		inventory.onInventoryChanged += OnInventoryChanged;
		inventory.onItemAddedClient += OnItemAddedClient;
		inventory.onEquipmentExternalRestockServer += OnInventoryEquipmentExternalRestockServer;
		OnInventoryChanged();
		Stage.onServerStageBegin += OnServerStageBegin;
	}

	private void OnItemAddedClient(ItemIndex itemIndex)
	{
		StartCoroutine(HighlightNewItem(itemIndex));
	}

	private IEnumerator HighlightNewItem(ItemIndex itemIndex)
	{
		yield return new WaitForSeconds(0.05f);
		GameObject bodyObject = GetBodyObject();
		if (!bodyObject)
		{
			yield break;
		}
		ModelLocator component = bodyObject.GetComponent<ModelLocator>();
		if (!component)
		{
			yield break;
		}
		Transform modelTransform = component.modelTransform;
		if ((bool)modelTransform)
		{
			CharacterModel component2 = modelTransform.GetComponent<CharacterModel>();
			if ((bool)component2)
			{
				component2.HighlightItemDisplay(itemIndex);
			}
		}
	}

	private void Start()
	{
		UpdateAuthority();
		if (NetworkServer.active && spawnOnStart && !bodyInstanceObject)
		{
			SpawnBodyHere();
		}
		CharacterMaster.onStartGlobal?.Invoke(this);
	}

	public void SetAIUpdateFrequency(bool updateEveryFrame)
	{
		BaseAI[] array = aiComponents;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].forceUpdateEveryFrame = updateEveryFrame;
		}
	}

	private void OnInventoryChanged()
	{
		luck = 0f;
		luck += inventory.GetItemCount(RoR2Content.Items.Clover);
		luck -= inventory.GetItemCount(RoR2Content.Items.LunarBadLuck);
		if (NetworkServer.active && (bool)inventory)
		{
			CharacterBody body = GetBody();
			if ((bool)body && body.bodyIndex != BodyCatalog.SpecialCases.HereticBody() && inventory.GetItemCount(RoR2Content.Items.LunarPrimaryReplacement.itemIndex) > 0 && inventory.GetItemCount(RoR2Content.Items.LunarSecondaryReplacement.itemIndex) > 0 && inventory.GetItemCount(RoR2Content.Items.LunarSpecialReplacement.itemIndex) > 0 && inventory.GetItemCount(RoR2Content.Items.LunarUtilityReplacement.itemIndex) > 0)
			{
				TransformBody("HereticBody");
			}
			if (inventory.GetItemCount(DLC2Content.Items.OnLevelUpFreeUnlock) > 0 && money != 0)
			{
				GiveMoney(money);
				money = 0u;
			}
		}
		SetUpGummyClone();
	}

	private void OnInventoryEquipmentExternalRestockServer()
	{
		CharacterBody body = GetBody();
		if ((bool)body)
		{
			EffectData effectData = new EffectData();
			effectData.origin = body.corePosition;
			effectData.SetNetworkedObjectReference(body.gameObject);
			EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/EquipmentRestockEffect"), effectData, transmit: true);
		}
	}

	[Server]
	public void SpawnBodyHere()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.CharacterMaster::SpawnBodyHere()' called on client");
		}
		else
		{
			SpawnBody(base.transform.position, base.transform.rotation);
		}
	}

	private void OnEnable()
	{
		instancesList.Add(this);
		CharacterMaster.onCharacterMasterDiscovered?.Invoke(this);
	}

	private void OnDisable()
	{
		try
		{
			CharacterMaster.onCharacterMasterLost?.Invoke(this);
		}
		catch (Exception message)
		{
			Debug.LogError(message);
		}
		instancesList.Remove(this);
	}

	private void OnDestroy()
	{
		if (isBoss)
		{
			isBoss = false;
		}
		Stage.onServerStageBegin -= OnServerStageBegin;
	}

	public void OnBodyStart(CharacterBody body)
	{
		if (NetworkServer.active)
		{
			lostBodyToDeath = false;
		}
		preventGameOver = true;
		killerBodyIndex = BodyIndex.None;
		killedByUnsafeArea = false;
		body.RecalculateStats();
		if (NetworkServer.active)
		{
			BaseAI[] array = aiComponents;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].OnBodyStart(body);
			}
		}
		if ((bool)playerCharacterMasterController)
		{
			if ((bool)playerCharacterMasterController.networkUserObject)
			{
				_ = playerCharacterMasterController.networkUserObject.GetComponent<NetworkIdentity>().isLocalPlayer;
			}
			playerCharacterMasterController.OnBodyStart();
		}
		if (inventory.GetItemCount(RoR2Content.Items.Ghost) > 0)
		{
			Util.PlaySound("Play_item_proc_ghostOnKill", body.gameObject);
		}
		if (NetworkServer.active)
		{
			HealthComponent healthComponent = body.healthComponent;
			if ((bool)healthComponent)
			{
				if (teamIndex == TeamIndex.Player && Run.instance.selectedDifficulty >= DifficultyIndex.Eclipse1)
				{
					healthComponent.Networkhealth = healthComponent.fullHealth * 0.5f;
				}
				else
				{
					healthComponent.Networkhealth = healthComponent.fullHealth;
				}
			}
			UpdateBodyGodMode();
			StartLifeStopwatch();
		}
		SetUpGummyClone();
		this.onBodyStart?.Invoke(body);
		if (inventory.GetItemCount(DLC2Content.Items.ExtraStatsOnLevelUp) > 0)
		{
			uint num = TeamManager.instance.FindLevelForBeadExperience(beadExperience);
			body.SetBuffCount(DLC2Content.Buffs.ExtraStatsOnLevelUpBuff.buffIndex, (int)(num - 1));
		}
	}

	[Server]
	public bool IsExtraLifePendingServer()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Boolean RoR2.CharacterMaster::IsExtraLifePendingServer()' called on client");
			return false;
		}
		if (!IsInvoking("RespawnExtraLife") && !IsInvoking("RespawnExtraLifeVoid") && !IsInvoking("RespawnExtraLifeShrine"))
		{
			return IsInvoking("RespawnExtraLifeHealAndRevive");
		}
		return true;
	}

	[Server]
	public bool IsDeadAndOutOfLivesServer()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Boolean RoR2.CharacterMaster::IsDeadAndOutOfLivesServer()' called on client");
			return false;
		}
		CharacterBody body = GetBody();
		if (body != null)
		{
			if (body.healthComponent.alive)
			{
				return false;
			}
			if (body.HasBuff(DLC2Content.Buffs.ExtraLifeBuff))
			{
				return false;
			}
			bool flag = false;
			if (body.equipmentSlot != null && body.equipmentSlot.equipmentIndex == DLC2Content.Equipment.HealAndRevive.equipmentIndex)
			{
				flag = true;
			}
			if (flag)
			{
				return false;
			}
		}
		return inventory.GetItemCount(RoR2Content.Items.ExtraLife) <= 0 && inventory.GetItemCount(DLC1Content.Items.ExtraLifeVoid) <= 0 && !IsExtraLifePendingServer();
	}

	public void OnBodyDeath(CharacterBody body)
	{
		if (NetworkServer.active)
		{
			lostBodyToDeath = true;
			deathFootPosition = body.footPosition;
			BaseAI[] array = aiComponents;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].OnBodyDeath(body);
			}
			if ((bool)playerCharacterMasterController)
			{
				playerCharacterMasterController.OnBodyDeath();
			}
			if (body.HasBuff(DLC2Content.Buffs.ExtraLifeBuff))
			{
				body.RemoveBuff(DLC2Content.Buffs.ExtraLifeBuff);
				Invoke("RespawnExtraLifeShrine", 2f);
				Invoke("PlayExtraLifeSFX", 1f);
			}
			else
			{
				bool flag = false;
				if ((bool)body.equipmentSlot && body.equipmentSlot.equipmentIndex == DLC2Content.Equipment.HealAndRevive.equipmentIndex)
				{
					flag = true;
				}
				if (flag)
				{
					CharacterMasterNotificationQueue.SendTransformNotification(this, inventory.currentEquipmentIndex, DLC2Content.Equipment.HealAndReviveConsumed.equipmentIndex, CharacterMasterNotificationQueue.TransformationType.Default);
					inventory.SetEquipmentIndex(DLC2Content.Equipment.HealAndReviveConsumed.equipmentIndex);
					Invoke("RespawnExtraLifeHealAndRevive", 2f);
					Invoke("PlayExtraLifeSFX", 1f);
				}
				else if (inventory.GetItemCount(RoR2Content.Items.ExtraLife) > 0)
				{
					inventory.RemoveItem(RoR2Content.Items.ExtraLife);
					Invoke("RespawnExtraLife", 2f);
					Invoke("PlayExtraLifeSFX", 1f);
				}
				else if (inventory.GetItemCount(DLC1Content.Items.ExtraLifeVoid) > 0)
				{
					inventory.RemoveItem(DLC1Content.Items.ExtraLifeVoid);
					Invoke("RespawnExtraLifeVoid", 2f);
					Invoke("PlayExtraLifeVoidSFX", 1f);
				}
				else
				{
					if (destroyOnBodyDeath)
					{
						UnityEngine.Object.Destroy(base.gameObject, 1f);
					}
					preventGameOver = false;
					preventRespawnUntilNextStageServer = true;
				}
			}
			if (TryGetComponent<DevotedLemurianController>(out var component))
			{
				component.OnDevotedBodyDead();
			}
			if ((bool)devotionInventoryPrefab)
			{
				devotionInventoryPrefab.GetComponent<Inventory>().RemoveItem(devotionItem, devotedEvolutionTracker + 1);
				devotionInventoryUpdaterRef.DropScrapOnDeath(devotionItem, body);
				devotionInventoryUpdaterRef.UpdateAllMinions();
			}
			ResetLifeStopwatch();
		}
		onBodyDeath?.Invoke();
	}

	public void TrueKill()
	{
		TrueKill(null, null, DamageType.Generic);
	}

	public void TrueKill(GameObject killerOverride = null, GameObject inflictorOverride = null, DamageTypeCombo damageTypeOverride = default(DamageTypeCombo))
	{
		int itemCount = inventory.GetItemCount(RoR2Content.Items.ExtraLife);
		if (itemCount > 0)
		{
			inventory.ResetItem(RoR2Content.Items.ExtraLife);
			inventory.GiveItem(RoR2Content.Items.ExtraLifeConsumed, itemCount);
			CharacterMasterNotificationQueue.SendTransformNotification(this, RoR2Content.Items.ExtraLife.itemIndex, RoR2Content.Items.ExtraLifeConsumed.itemIndex, CharacterMasterNotificationQueue.TransformationType.Default);
		}
		if (inventory.GetItemCount(DLC1Content.Items.ExtraLifeVoid) > 0)
		{
			inventory.ResetItem(DLC1Content.Items.ExtraLifeVoid);
			inventory.GiveItem(DLC1Content.Items.ExtraLifeVoidConsumed, itemCount);
			CharacterMasterNotificationQueue.SendTransformNotification(this, DLC1Content.Items.ExtraLifeVoid.itemIndex, DLC1Content.Items.ExtraLifeVoidConsumed.itemIndex, CharacterMasterNotificationQueue.TransformationType.Default);
		}
		if (GetBody().HasBuff(DLC2Content.Buffs.ExtraLifeBuff))
		{
			GetBody().RemoveBuff(DLC2Content.Buffs.ExtraLifeBuff);
		}
		bool flag = false;
		if ((bool)GetBody().equipmentSlot && GetBody().equipmentSlot.equipmentIndex == DLC2Content.Equipment.HealAndRevive.equipmentIndex)
		{
			flag = true;
		}
		if (flag)
		{
			CharacterMasterNotificationQueue.SendTransformNotification(this, inventory.currentEquipmentIndex, DLC2Content.Equipment.HealAndReviveConsumed.equipmentIndex, CharacterMasterNotificationQueue.TransformationType.Default);
			inventory.SetEquipmentIndex(DLC2Content.Equipment.HealAndReviveConsumed.equipmentIndex);
		}
		CancelInvoke("RespawnExtraLife");
		CancelInvoke("PlayExtraLifeSFX");
		CancelInvoke("RespawnExtraLifeVoid");
		CancelInvoke("PlayExtraLifeVoidSFX");
		CancelInvoke("RespawnExtraLifeShrine");
		CharacterBody body = GetBody();
		if ((bool)body)
		{
			body.healthComponent.Suicide(killerOverride, inflictorOverride, damageTypeOverride);
		}
	}

	private void PlayExtraLifeSFX()
	{
		GameObject gameObject = bodyInstanceObject;
		if ((bool)gameObject)
		{
			Util.PlaySound("Play_item_proc_extraLife", gameObject);
		}
	}

	private void PlayExtraLifeVoidSFX()
	{
		GameObject gameObject = bodyInstanceObject;
		if ((bool)gameObject)
		{
			Util.PlaySound("Play_item_void_extraLife", gameObject);
		}
	}

	public void RespawnExtraLife()
	{
		inventory.GiveItem(RoR2Content.Items.ExtraLifeConsumed);
		CharacterMasterNotificationQueue.SendTransformNotification(this, RoR2Content.Items.ExtraLife.itemIndex, RoR2Content.Items.ExtraLifeConsumed.itemIndex, CharacterMasterNotificationQueue.TransformationType.Default);
		Vector3 vector = deathFootPosition;
		if (killedByUnsafeArea)
		{
			vector = TeleportHelper.FindSafeTeleportDestination(deathFootPosition, bodyPrefab.GetComponent<CharacterBody>(), RoR2Application.rng) ?? deathFootPosition;
		}
		Respawn(vector, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f), wasRevivedMidStage: true);
		GetBody().AddTimedBuff(RoR2Content.Buffs.Immune, 3f);
		if (NetworkServer.active)
		{
			inventory.SetEquipmentDisabled(_active: false);
		}
		GameObject gameObject = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/HippoRezEffect");
		if ((bool)bodyInstanceObject)
		{
			EntityStateMachine[] components = bodyInstanceObject.GetComponents<EntityStateMachine>();
			foreach (EntityStateMachine obj in components)
			{
				obj.initialStateType = obj.mainStateType;
			}
			if ((bool)gameObject)
			{
				EffectManager.SpawnEffect(gameObject, new EffectData
				{
					origin = vector,
					rotation = bodyInstanceObject.transform.rotation
				}, transmit: true);
			}
		}
	}

	public void RespawnExtraLifeVoid()
	{
		inventory.GiveItem(DLC1Content.Items.ExtraLifeVoidConsumed);
		CharacterMasterNotificationQueue.SendTransformNotification(this, DLC1Content.Items.ExtraLifeVoid.itemIndex, DLC1Content.Items.ExtraLifeVoidConsumed.itemIndex, CharacterMasterNotificationQueue.TransformationType.Default);
		Vector3 vector = deathFootPosition;
		if (killedByUnsafeArea)
		{
			vector = TeleportHelper.FindSafeTeleportDestination(deathFootPosition, bodyPrefab.GetComponent<CharacterBody>(), RoR2Application.rng) ?? deathFootPosition;
		}
		Respawn(vector, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f), wasRevivedMidStage: true);
		GetBody().AddTimedBuff(RoR2Content.Buffs.Immune, 3f);
		if (NetworkServer.active)
		{
			inventory.SetEquipmentDisabled(_active: false);
		}
		if ((bool)bodyInstanceObject)
		{
			EntityStateMachine[] components = bodyInstanceObject.GetComponents<EntityStateMachine>();
			foreach (EntityStateMachine obj in components)
			{
				obj.initialStateType = obj.mainStateType;
			}
			if ((bool)ExtraLifeVoidManager.rezEffectPrefab)
			{
				EffectManager.SpawnEffect(ExtraLifeVoidManager.rezEffectPrefab, new EffectData
				{
					origin = vector,
					rotation = bodyInstanceObject.transform.rotation
				}, transmit: true);
			}
		}
		foreach (ContagiousItemManager.TransformationInfo transformationInfo in ContagiousItemManager.transformationInfos)
		{
			ContagiousItemManager.TryForceReplacement(inventory, transformationInfo.originalItem);
		}
	}

	public void RespawnExtraLifeShrine()
	{
		Vector3 vector = deathFootPosition;
		if (killedByUnsafeArea)
		{
			vector = TeleportHelper.FindSafeTeleportDestination(deathFootPosition, bodyPrefab.GetComponent<CharacterBody>(), RoR2Application.rng) ?? deathFootPosition;
		}
		Respawn(vector, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f), wasRevivedMidStage: true);
		GetBody().AddTimedBuff(RoR2Content.Buffs.Immune, 3f);
		GameObject gameObject = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/fxHealAndReviveGold");
		if ((bool)bodyInstanceObject)
		{
			EntityStateMachine[] components = bodyInstanceObject.GetComponents<EntityStateMachine>();
			foreach (EntityStateMachine obj in components)
			{
				obj.initialStateType = obj.mainStateType;
			}
			if ((bool)gameObject)
			{
				EffectManager.SpawnEffect(gameObject, new EffectData
				{
					origin = vector,
					rotation = bodyInstanceObject.transform.rotation
				}, transmit: true);
				Util.PlaySound("Play_item_use_healAndRevive_activate", base.gameObject);
			}
		}
	}

	public void RespawnExtraLifeHealAndRevive()
	{
		Vector3 vector = deathFootPosition;
		if (killedByUnsafeArea)
		{
			vector = TeleportHelper.FindSafeTeleportDestination(deathFootPosition, bodyPrefab.GetComponent<CharacterBody>(), RoR2Application.rng) ?? deathFootPosition;
		}
		Respawn(vector, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f), wasRevivedMidStage: true);
		GetBody().AddTimedBuff(RoR2Content.Buffs.Immune, 3f);
		GameObject gameObject = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/fxHealAndReviveGold");
		if ((bool)bodyInstanceObject)
		{
			EntityStateMachine[] components = bodyInstanceObject.GetComponents<EntityStateMachine>();
			foreach (EntityStateMachine obj in components)
			{
				obj.initialStateType = obj.mainStateType;
			}
			if ((bool)gameObject)
			{
				EffectManager.SpawnEffect(gameObject, new EffectData
				{
					origin = vector,
					rotation = bodyInstanceObject.transform.rotation
				}, transmit: true);
				Util.PlaySound("Play_item_use_healAndRevive_activate", base.gameObject);
			}
		}
	}

	public void OnBodyDamaged(DamageReport damageReport)
	{
		BaseAI[] array = aiComponents;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].OnBodyDamaged(damageReport);
		}
		if ((bool)playerCharacterMasterController)
		{
			HandleOnPlayerBodyDamaged();
			CallRpcOnPlayerBodyDamaged();
		}
	}

	[ClientRpc]
	private void RpcOnPlayerBodyDamaged()
	{
		HandleOnPlayerBodyDamaged();
	}

	private void HandleOnPlayerBodyDamaged()
	{
		this.onPlayerBodyDamaged?.Invoke(playerCharacterMasterController);
	}

	public void OnBodyDestroyed(CharacterBody characterBody)
	{
		if ((object)characterBody != GetBody())
		{
			return;
		}
		if (NetworkServer.active)
		{
			BaseAI[] array = aiComponents;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].OnBodyDestroyed(characterBody);
			}
			PauseLifeStopwatch();
		}
		this.onBodyDestroyed?.Invoke(characterBody);
	}

	[Server]
	private void StartLifeStopwatch()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.CharacterMaster::StartLifeStopwatch()' called on client");
		}
		else if (!(internalSurvivalTime > 0f))
		{
			internalSurvivalTime = Run.instance.GetRunStopwatch() - currentLifeStopwatch;
		}
	}

	[Server]
	private void PauseLifeStopwatch()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.CharacterMaster::PauseLifeStopwatch()' called on client");
		}
		else if (!(internalSurvivalTime <= 0f))
		{
			internalSurvivalTime = 0f - currentLifeStopwatch;
		}
	}

	[Server]
	private void ResetLifeStopwatch()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.CharacterMaster::ResetLifeStopwatch()' called on client");
		}
		else
		{
			internalSurvivalTime = 0f;
		}
	}

	[Server]
	public BodyIndex GetKillerBodyIndex()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'RoR2.BodyIndex RoR2.CharacterMaster::GetKillerBodyIndex()' called on client");
			return default(BodyIndex);
		}
		return killerBodyIndex;
	}

	[InitDuringStartup]
	private static void Init()
	{
		GlobalEventManager.onCharacterDeathGlobal += delegate(DamageReport damageReport)
		{
			CharacterMaster victimMaster = damageReport.victimMaster;
			if ((bool)victimMaster)
			{
				victimMaster.killerBodyIndex = BodyCatalog.FindBodyIndex(damageReport.damageInfo.attacker);
				victimMaster.killedByUnsafeArea = (bool)damageReport.damageInfo.inflictor && (bool)damageReport.damageInfo.inflictor.GetComponent<MapZone>();
			}
		};
		Stage.onServerStageBegin += delegate
		{
			foreach (CharacterMaster instances in instancesList)
			{
				instances.preventRespawnUntilNextStageServer = false;
			}
		};
		CommonAssets.Resolve();
	}

	[Command]
	public void CmdRespawn(string bodyName)
	{
		if (preventRespawnUntilNextStageServer)
		{
			return;
		}
		if (!string.IsNullOrEmpty(bodyName))
		{
			bodyPrefab = BodyCatalog.FindBodyPrefab(bodyName);
			if (!bodyPrefab)
			{
				Debug.LogError("CmdRespawn failed to find bodyPrefab for name '" + bodyName + "'.");
			}
		}
		if ((bool)Stage.instance)
		{
			Stage.instance.RespawnCharacter(this);
		}
	}

	[Server]
	public void TransformBody(string bodyName)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.CharacterMaster::TransformBody(System.String)' called on client");
		}
		else if (!string.IsNullOrEmpty(bodyName))
		{
			bodyPrefab = BodyCatalog.FindBodyPrefab(bodyName);
			if (bodyPrefab != null)
			{
				Transform component = bodyInstanceObject.GetComponent<Transform>();
				Vector3 position = component.position;
				Quaternion rotation = component.rotation;
				DestroyBody();
				CharacterBody component2 = bodyPrefab.GetComponent<CharacterBody>();
				if ((bool)component2)
				{
					position = CalculateSafeGroundPosition(position, component2);
					SpawnBody(position, rotation).GetComponent<HereticInitialStateHelper>()?.PrepareTransformation();
				}
				else
				{
					Debug.LogErrorFormat("Trying to respawn as object {0} who has no Character Body!", bodyPrefab);
				}
			}
			else
			{
				Debug.LogError("Can't TransformBody because there's no prefab for body named '" + bodyName + "'");
			}
		}
		else
		{
			Debug.LogError("Can't TransformBody with null or empty body name.");
		}
	}

	[Server]
	private void OnServerStageBegin(Stage stage)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.CharacterMaster::OnServerStageBegin(RoR2.Stage)' called on client");
			return;
		}
		TryCloverVoidUpgrades();
		TryRegenerateScrap();
		TrySaleStar();
	}

	[ClientRpc]
	public void RpcSetSeekerRevive(SEEKER_REVIVE_STATUS s)
	{
		seekerUsedRevive = s;
	}

	public SEEKER_REVIVE_STATUS getSeekerUsedRevive()
	{
		return seekerUsedRevive;
	}

	private void TryRegenerateScrap()
	{
		int itemCount = inventory.GetItemCount(DLC1Content.Items.RegeneratingScrapConsumed);
		if (itemCount > 0)
		{
			inventory.RemoveItem(DLC1Content.Items.RegeneratingScrapConsumed, itemCount);
			inventory.GiveItem(DLC1Content.Items.RegeneratingScrap, itemCount);
			CharacterMasterNotificationQueue.SendTransformNotification(this, DLC1Content.Items.RegeneratingScrapConsumed.itemIndex, DLC1Content.Items.RegeneratingScrap.itemIndex, CharacterMasterNotificationQueue.TransformationType.RegeneratingScrapRegen);
		}
	}

	private void TrySaleStar()
	{
		int itemCount = inventory.GetItemCount(DLC2Content.Items.LowerPricedChestsConsumed);
		if (itemCount > 0)
		{
			inventory.RemoveItem(DLC2Content.Items.LowerPricedChestsConsumed, itemCount);
			inventory.GiveItem(DLC2Content.Items.LowerPricedChests, itemCount);
			CharacterMasterNotificationQueue.SendTransformNotification(this, DLC2Content.Items.LowerPricedChestsConsumed.itemIndex, DLC2Content.Items.LowerPricedChests.itemIndex, CharacterMasterNotificationQueue.TransformationType.SaleStarRegen);
		}
	}

	private void TryTeleportOnLowHealthRegen()
	{
		int itemCount = inventory.GetItemCount(DLC2Content.Items.TeleportOnLowHealthConsumed);
		if (itemCount > 0)
		{
			inventory.RemoveItem(DLC2Content.Items.TeleportOnLowHealthConsumed, itemCount);
			inventory.GiveItem(DLC2Content.Items.TeleportOnLowHealth, itemCount);
			CharacterMasterNotificationQueue.SendTransformNotification(this, DLC2Content.Items.TeleportOnLowHealthConsumed.itemIndex, DLC2Content.Items.TeleportOnLowHealth.itemIndex, CharacterMasterNotificationQueue.TransformationType.TeleportOnLowHealthRegen);
		}
	}

	[Server]
	public void TrackDevotionItem(ItemIndex itemIndex, Interactor summoner, GameObject devotedInventoryRef, DevotionInventoryController devotionInventoryUpdater)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.CharacterMaster::TrackDevotionItem(RoR2.ItemIndex,RoR2.Interactor,UnityEngine.GameObject,RoR2.DevotionInventoryController)' called on client");
			return;
		}
		_ = summoner == null;
		devotionItem = itemIndex;
		summonerRef = summoner;
		devotionInventoryPrefab = devotedInventoryRef;
		isDevotedMinion = true;
		devotionInventoryUpdaterRef = devotionInventoryUpdater;
	}

	[Server]
	private void TryCloverVoidUpgrades()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.CharacterMaster::TryCloverVoidUpgrades()' called on client");
			return;
		}
		if (cloverVoidRng == null)
		{
			cloverVoidRng = new Xoroshiro128Plus(Run.instance.seed);
		}
		int itemCount = inventory.GetItemCount(DLC1Content.Items.CloverVoid);
		List<PickupIndex> list = new List<PickupIndex>(Run.instance.availableTier2DropList);
		List<PickupIndex> list2 = new List<PickupIndex>(Run.instance.availableTier3DropList);
		List<ItemIndex> list3 = new List<ItemIndex>(inventory.itemAcquisitionOrder);
		Util.ShuffleList(list3, cloverVoidRng);
		int num = itemCount * 3;
		int num2 = 0;
		int num3 = 0;
		while (num2 < num && num3 < list3.Count)
		{
			ItemDef startingItemDef = ItemCatalog.GetItemDef(list3[num3]);
			ItemDef itemDef = null;
			List<PickupIndex> list4 = null;
			switch (startingItemDef.tier)
			{
			case ItemTier.Tier1:
				list4 = list;
				break;
			case ItemTier.Tier2:
				list4 = list2;
				break;
			}
			if (list4 != null && list4.Count > 0)
			{
				Util.ShuffleList(list4, cloverVoidRng);
				list4.Sort(CompareTags);
				itemDef = ItemCatalog.GetItemDef(list4[0].itemIndex);
			}
			if (itemDef != null)
			{
				if (inventory.GetItemCount(itemDef.itemIndex) == 0)
				{
					list3.Add(itemDef.itemIndex);
				}
				num2++;
				int itemCount2 = inventory.GetItemCount(startingItemDef.itemIndex);
				inventory.RemoveItem(startingItemDef.itemIndex, itemCount2);
				inventory.GiveItem(itemDef.itemIndex, itemCount2);
				CharacterMasterNotificationQueue.SendTransformNotification(this, startingItemDef.itemIndex, itemDef.itemIndex, CharacterMasterNotificationQueue.TransformationType.CloverVoid);
			}
			num3++;
			int CompareTags(PickupIndex lhs, PickupIndex rhs)
			{
				int num4 = 0;
				int num5 = 0;
				ItemDef itemDef2 = ItemCatalog.GetItemDef(lhs.itemIndex);
				ItemDef itemDef3 = ItemCatalog.GetItemDef(rhs.itemIndex);
				if (startingItemDef.ContainsTag(ItemTag.Damage))
				{
					if (itemDef2.ContainsTag(ItemTag.Damage))
					{
						num4 = 1;
					}
					if (itemDef3.ContainsTag(ItemTag.Damage))
					{
						num5 = 1;
					}
				}
				if (startingItemDef.ContainsTag(ItemTag.Healing))
				{
					if (itemDef2.ContainsTag(ItemTag.Healing))
					{
						num4 = 1;
					}
					if (itemDef3.ContainsTag(ItemTag.Healing))
					{
						num5 = 1;
					}
				}
				if (startingItemDef.ContainsTag(ItemTag.Utility))
				{
					if (itemDef2.ContainsTag(ItemTag.Utility))
					{
						num4 = 1;
					}
					if (itemDef3.ContainsTag(ItemTag.Utility))
					{
						num5 = 1;
					}
				}
				return num5 - num4;
			}
		}
		if (num2 > 0)
		{
			GameObject gameObject = bodyInstanceObject;
			if ((bool)gameObject)
			{
				Util.PlaySound("Play_item_proc_extraLife", gameObject);
			}
		}
	}

	private static GameObject PickRandomSurvivorBodyPrefab(Xoroshiro128Plus rng, NetworkUser networkUser, bool allowHidden)
	{
		SurvivorDef[] array = SurvivorCatalog.allSurvivorDefs.Where(SurvivorIsUnlockedAndAvailable).ToArray();
		return rng.NextElementUniform(array).bodyPrefab;
		bool SurvivorIsUnlockedAndAvailable(SurvivorDef survivorDef)
		{
			if (allowHidden || !survivorDef.hidden)
			{
				if (!survivorDef.CheckRequiredExpansionEnabled(networkUser))
				{
					return false;
				}
				UnlockableDef unlockableDef = survivorDef.unlockableDef;
				if ((object)unlockableDef != null)
				{
					return networkUser.unlockables.Contains(unlockableDef);
				}
				return true;
			}
			return false;
		}
	}

	[Server]
	public CharacterBody Respawn(Vector3 footPosition, Quaternion rotation, bool wasRevivedMidStage = false)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'RoR2.CharacterBody RoR2.CharacterMaster::Respawn(UnityEngine.Vector3,UnityEngine.Quaternion,System.Boolean)' called on client");
			return null;
		}
		if (!wasRevivedMidStage)
		{
			CallRpcSetSeekerRevive(SEEKER_REVIVE_STATUS.UNUSED);
		}
		else if (seekerUsedRevive == SEEKER_REVIVE_STATUS.USED)
		{
			CallRpcSetSeekerRevive(SEEKER_REVIVE_STATUS.BURNED);
		}
		DestroyBody();
		if ((bool)playerCharacterMasterController && RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.randomSurvivorOnRespawnArtifactDef))
		{
			bodyPrefab = PickRandomSurvivorBodyPrefab(Run.instance.randomSurvivorOnRespawnRng, playerCharacterMasterController.networkUser, allowHidden: false);
		}
		if ((bool)bodyPrefab)
		{
			CharacterBody component = bodyPrefab.GetComponent<CharacterBody>();
			if ((bool)component)
			{
				Vector3 position = footPosition;
				if (true)
				{
					position = CalculateSafeGroundPosition(footPosition, component);
				}
				return SpawnBody(position, rotation);
			}
			Debug.LogErrorFormat("Trying to respawn as object {0} who has no Character Body!", bodyPrefab);
		}
		else
		{
			Debug.LogErrorFormat("CharacterMaster.Respawn failed. {0} does not have a valid body prefab assigned.", base.gameObject.name);
		}
		return null;
	}

	private Vector3 CalculateSafeGroundPosition(Vector3 desiredFootPos, CharacterBody body)
	{
		if ((bool)body)
		{
			Vector3 result = desiredFootPos;
			RaycastHit hitInfo = default(RaycastHit);
			Ray ray = new Ray(desiredFootPos + Vector3.up * 2f, Vector3.down);
			float maxDistance = 4f;
			if (Physics.SphereCast(ray, body.radius, out hitInfo, maxDistance, LayerIndex.world.mask))
			{
				result.y = ray.origin.y - hitInfo.distance;
			}
			float bodyPrefabFootOffset = Util.GetBodyPrefabFootOffset(bodyPrefab);
			result.y += bodyPrefabFootOffset;
			return result;
		}
		Debug.LogError("Can't calculate safe ground position if the CharacterBody is null");
		return desiredFootPos;
	}

	private void SetUpGummyClone()
	{
		if (!NetworkServer.active || !inventory || inventory.GetItemCount(DLC1Content.Items.GummyCloneIdentifier.itemIndex) <= 0)
		{
			return;
		}
		if (!base.gameObject.GetComponent<MasterSuicideOnTimer>())
		{
			base.gameObject.AddComponent<MasterSuicideOnTimer>().lifeTimer = 30f;
		}
		CharacterBody body = GetBody();
		if ((bool)body)
		{
			CharacterDeathBehavior component = body.GetComponent<CharacterDeathBehavior>();
			if ((bool)component && component.deathState.stateType != typeof(GummyCloneDeathState))
			{
				component.deathState = new SerializableEntityStateType(typeof(GummyCloneDeathState));
			}
			body.portraitIcon = LegacyResourcesAPI.Load<Texture>("Textures/BodyIcons/texGummyCloneBody");
		}
	}

	public void SetMasterBodyDirtyBit()
	{
		SetDirtyBit(1u);
	}

	private void ToggleGod()
	{
		godMode = !godMode;
		UpdateBodyGodMode();
	}

	private void UpdateBodyGodMode()
	{
		if ((bool)bodyInstanceObject)
		{
			HealthComponent component = bodyInstanceObject.GetComponent<HealthComponent>();
			if ((bool)component)
			{
				component.godMode = godMode;
			}
		}
	}

	public override bool OnSerialize(NetworkWriter writer, bool initialState)
	{
		uint num = base.syncVarDirtyBits;
		if (initialState)
		{
			num = 127u;
		}
		bool num2 = (num & 1) != 0;
		bool flag = (num & 2) != 0;
		bool flag2 = (num & 0x40) != 0;
		bool flag3 = (num & 4) != 0;
		bool flag4 = (num & 8) != 0;
		bool flag5 = (num & 0x10) != 0;
		bool flag6 = (num & 0x20) != 0;
		writer.Write((byte)num);
		if (num2)
		{
			writer.Write(_bodyInstanceId);
		}
		if (flag)
		{
			writer.WritePackedUInt32(_money);
		}
		if (flag2)
		{
			writer.WritePackedUInt32(_voidCoins);
		}
		if (flag3)
		{
			writer.Write(_internalSurvivalTime);
		}
		if (flag4)
		{
			writer.Write(teamIndex);
		}
		if (flag5)
		{
			loadout.Serialize(writer);
		}
		if (flag6)
		{
			writer.WritePackedUInt32(miscFlags);
		}
		return num != 0;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
		byte num = reader.ReadByte();
		bool flag = (num & 1) != 0;
		bool flag2 = (num & 2) != 0;
		bool flag3 = (num & 0x40) != 0;
		bool flag4 = (num & 4) != 0;
		bool flag5 = (num & 8) != 0;
		bool flag6 = (num & 0x10) != 0;
		bool num2 = (num & 0x20) != 0;
		if (flag)
		{
			NetworkInstanceId value = reader.ReadNetworkId();
			OnSyncBodyInstanceId(value);
		}
		if (flag2)
		{
			_money = reader.ReadPackedUInt32();
		}
		if (flag3)
		{
			_voidCoins = reader.ReadPackedUInt32();
		}
		if (flag4)
		{
			_internalSurvivalTime = reader.ReadSingle();
		}
		if (flag5)
		{
			teamIndex = reader.ReadTeamIndex();
		}
		if (flag6)
		{
			loadout.Deserialize(reader);
		}
		if (num2)
		{
			miscFlags = reader.ReadPackedUInt32();
		}
	}

	static CharacterMaster()
	{
		instancesList = new List<CharacterMaster>();
		_readOnlyInstancesList = new ReadOnlyCollection<CharacterMaster>(instancesList);
		kCmdCmdRespawn = 1097984413;
		NetworkBehaviour.RegisterCommandDelegate(typeof(CharacterMaster), kCmdCmdRespawn, InvokeCmdCmdRespawn);
		kRpcRpcOnPlayerBodyDamaged = -1070481806;
		NetworkBehaviour.RegisterRpcDelegate(typeof(CharacterMaster), kRpcRpcOnPlayerBodyDamaged, InvokeRpcRpcOnPlayerBodyDamaged);
		kRpcRpcSetSeekerRevive = 458435899;
		NetworkBehaviour.RegisterRpcDelegate(typeof(CharacterMaster), kRpcRpcSetSeekerRevive, InvokeRpcRpcSetSeekerRevive);
		NetworkCRC.RegisterBehaviour("CharacterMaster", 0);
	}

	private void UNetVersion()
	{
	}

	protected static void InvokeCmdCmdRespawn(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdRespawn called on client.");
		}
		else
		{
			((CharacterMaster)obj).CmdRespawn(reader.ReadString());
		}
	}

	public void CallCmdRespawn(string bodyName)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("Command function CmdRespawn called on server.");
			return;
		}
		if (base.isServer)
		{
			CmdRespawn(bodyName);
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)5);
		networkWriter.WritePackedUInt32((uint)kCmdCmdRespawn);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.Write(bodyName);
		SendCommandInternal(networkWriter, 0, "CmdRespawn");
	}

	protected static void InvokeRpcRpcOnPlayerBodyDamaged(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcOnPlayerBodyDamaged called on server.");
		}
		else
		{
			((CharacterMaster)obj).RpcOnPlayerBodyDamaged();
		}
	}

	protected static void InvokeRpcRpcSetSeekerRevive(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcSetSeekerRevive called on server.");
		}
		else
		{
			((CharacterMaster)obj).RpcSetSeekerRevive((SEEKER_REVIVE_STATUS)reader.ReadInt32());
		}
	}

	public void CallRpcOnPlayerBodyDamaged()
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcOnPlayerBodyDamaged called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcOnPlayerBodyDamaged);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		SendRPCInternal(networkWriter, 0, "RpcOnPlayerBodyDamaged");
	}

	public void CallRpcSetSeekerRevive(SEEKER_REVIVE_STATUS s)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcSetSeekerRevive called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcSetSeekerRevive);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.Write((int)s);
		SendRPCInternal(networkWriter, 0, "RpcSetSeekerRevive");
	}

	public override void PreStartClient()
	{
	}
}
