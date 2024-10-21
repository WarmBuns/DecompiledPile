using System;
using System.Collections.Generic;
using EntityStates;
using HG;
using RoR2.UI;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(CombatSquad))]
public class BossGroup : MonoBehaviour
{
	private struct BossMemory
	{
		public NetworkInstanceId masterInstanceId;

		public float maxObservedMaxHealth;

		public float lastObservedHealth;

		public CharacterMaster cachedMaster;

		public CharacterBody cachedBody;
	}

	private class DefeatBossObjectiveTracker : ObjectivePanelController.ObjectiveTracker
	{
		public DefeatBossObjectiveTracker()
		{
			baseToken = "OBJECTIVE_DEFEAT_BOSS";
		}
	}

	public class EnableHudHealthBarState : EntityState
	{
		private BossGroup bossGroup;

		public override void OnEnter()
		{
			base.OnEnter();
			bossGroup = GetComponent<BossGroup>();
			if ((object)bossGroup != null)
			{
				bossGroup.shouldDisplayHealthBarOnHud = true;
			}
		}

		public override void OnExit()
		{
			if ((object)bossGroup != null)
			{
				bossGroup.shouldDisplayHealthBarOnHud = false;
			}
			base.OnExit();
		}
	}

	public float bossDropChance = 0.15f;

	public Transform dropPosition;

	public PickupDropTable dropTable;

	public bool scaleRewardsByPlayerCount = true;

	[Tooltip("Whether or not this boss group should display a health bar on the HUD while any of its members are alive. Other scripts can change this at runtime to suppress a health bar until the boss is angered, for example. This field is not networked, so whatever is driving the value should be synchronized over the network.")]
	public bool shouldDisplayHealthBarOnHud = true;

	private Xoroshiro128Plus rng;

	private List<PickupDropTable> bossDropTables;

	private Run.FixedTimeStamp enabledTime;

	[Header("Deprecated")]
	public bool forceTier3Reward;

	private List<PickupIndex> bossDrops;

	private static readonly int initialBossMemoryCapacity = 8;

	private BossMemory[] bossMemories = new BossMemory[initialBossMemoryCapacity];

	private int bossMemoryCount;

	private static int lastTotalBossCount = 0;

	private static bool totalBossCountDirty = false;

	public float fixedAge => combatSquad.awakeTime.timeSince;

	public float fixedTimeSinceEnabled => enabledTime.timeSince;

	public int bonusRewardCount { get; set; }

	public CombatSquad combatSquad { get; private set; }

	public string bestObservedName { get; private set; } = "";

	public string bestObservedSubtitle { get; private set; } = "";

	public float totalMaxObservedMaxHealth { get; private set; }

	public float totalObservedHealth { get; private set; }

	public static event Action<BossGroup> onBossGroupStartServer;

	public static event Action<BossGroup> onBossGroupDefeatedServer;

	private void Awake()
	{
		base.enabled = false;
		combatSquad = GetComponent<CombatSquad>();
		combatSquad.onMemberDiscovered += OnMemberDiscovered;
		combatSquad.onMemberLost += OnMemberLost;
		if (NetworkServer.active)
		{
			combatSquad.onDefeatedServer += OnDefeatedServer;
			combatSquad.onMemberAddedServer += OnMemberAddedServer;
			combatSquad.onMemberDefeatedServer += OnMemberDefeatedServer;
			rng = new Xoroshiro128Plus(Run.instance.bossRewardRng.nextUlong);
			bossDrops = new List<PickupIndex>();
			bossDropTables = new List<PickupDropTable>();
		}
	}

	private void Start()
	{
		if (NetworkServer.active)
		{
			BossGroup.onBossGroupStartServer?.Invoke(this);
		}
	}

	private void OnEnable()
	{
		InstanceTracker.Add(this);
		ObjectivePanelController.collectObjectiveSources += ReportObjective;
		enabledTime = Run.FixedTimeStamp.now;
	}

	private void OnDisable()
	{
		ObjectivePanelController.collectObjectiveSources -= ReportObjective;
		InstanceTracker.Remove(this);
	}

	private void FixedUpdate()
	{
		UpdateBossMemories();
	}

	private void OnDefeatedServer()
	{
		DropRewards();
		Run.instance.OnServerBossDefeated(this);
		BossGroup.onBossGroupDefeatedServer?.Invoke(this);
	}

	private void OnMemberAddedServer(CharacterMaster memberMaster)
	{
		Run.instance.OnServerBossAdded(this, memberMaster);
	}

	private void OnMemberDefeatedServer(CharacterMaster memberMaster, DamageReport damageReport)
	{
		DeathRewards deathRewards = memberMaster.GetBodyObject()?.GetComponent<DeathRewards>();
		if (!deathRewards)
		{
			return;
		}
		if ((bool)deathRewards.bossDropTable)
		{
			bossDropTables.Add(deathRewards.bossDropTable);
			return;
		}
		PickupIndex pickupIndex = (PickupIndex)deathRewards.bossPickup;
		if (pickupIndex != PickupIndex.none)
		{
			bossDrops.Add(pickupIndex);
		}
	}

	private void OnMemberDiscovered(CharacterMaster memberMaster)
	{
		base.enabled = true;
		memberMaster.isBoss = true;
		totalBossCountDirty = true;
		RememberBoss(memberMaster);
	}

	private void OnMemberLost(CharacterMaster memberMaster)
	{
		memberMaster.isBoss = false;
		totalBossCountDirty = true;
		if (combatSquad.memberCount == 0)
		{
			base.enabled = false;
		}
	}

	private void DropRewards()
	{
		if (!Run.instance)
		{
			Debug.LogError("No valid run instance!");
			return;
		}
		if (rng == null)
		{
			Debug.LogError("RNG is null!");
			return;
		}
		int participatingPlayerCount = Run.instance.participatingPlayerCount;
		if (participatingPlayerCount == 0)
		{
			return;
		}
		if ((bool)dropPosition)
		{
			PickupIndex none = PickupIndex.none;
			if ((bool)dropTable)
			{
				none = dropTable.GenerateDrop(rng);
			}
			else
			{
				List<PickupIndex> list = Run.instance.availableTier2DropList;
				if (forceTier3Reward)
				{
					list = Run.instance.availableTier3DropList;
				}
				none = rng.NextElementUniform(list);
			}
			int num = 1 + bonusRewardCount;
			if (scaleRewardsByPlayerCount)
			{
				num *= participatingPlayerCount;
			}
			float angle = 360f / (float)num;
			Vector3 vector = Quaternion.AngleAxis(UnityEngine.Random.Range(0, 360), Vector3.up) * (Vector3.up * 40f + Vector3.forward * 5f);
			Quaternion quaternion = Quaternion.AngleAxis(angle, Vector3.up);
			bool flag = bossDrops != null && bossDrops.Count > 0;
			bool flag2 = bossDropTables != null && bossDropTables.Count > 0;
			int num2 = 0;
			while (num2 < num)
			{
				PickupIndex pickupIndex = none;
				if (bossDrops != null && (flag || flag2) && rng.nextNormalizedFloat <= bossDropChance)
				{
					if (flag2)
					{
						PickupDropTable pickupDropTable = rng.NextElementUniform(bossDropTables);
						if (pickupDropTable != null)
						{
							pickupIndex = pickupDropTable.GenerateDrop(rng);
						}
					}
					else
					{
						pickupIndex = rng.NextElementUniform(bossDrops);
					}
				}
				PickupDropletController.CreatePickupDroplet(pickupIndex, dropPosition.position, vector);
				num2++;
				vector = quaternion * vector;
			}
		}
		else
		{
			Debug.LogWarning("dropPosition not set for BossGroup! No item will be spawned.");
		}
	}

	private int FindBossMemoryIndex(NetworkInstanceId id)
	{
		for (int i = 0; i < bossMemoryCount; i++)
		{
			if (bossMemories[i].masterInstanceId == id)
			{
				return i;
			}
		}
		return -1;
	}

	private void RememberBoss(CharacterMaster master)
	{
		if ((bool)master)
		{
			int num = FindBossMemoryIndex(master.netId);
			if (num == -1)
			{
				num = AddBossMemory(master);
			}
			ref BossMemory reference = ref bossMemories[num];
			reference.cachedMaster = master;
			reference.cachedBody = master.GetBody();
			UpdateObservations(ref reference);
		}
	}

	private void UpdateObservations(ref BossMemory memory)
	{
		memory.lastObservedHealth = 0f;
		if ((bool)memory.cachedMaster && !memory.cachedBody)
		{
			memory.cachedBody = memory.cachedMaster.GetBody();
		}
		if (!memory.cachedBody)
		{
			return;
		}
		if (bestObservedName.Length == 0 && bestObservedSubtitle.Length == 0 && Time.fixedDeltaTime * 3f < memory.cachedBody.localStartTime.timeSince)
		{
			bestObservedName = Util.GetBestBodyName(memory.cachedBody.gameObject);
			bestObservedSubtitle = memory.cachedBody.GetSubtitle();
			if (bestObservedSubtitle.Length == 0)
			{
				bestObservedSubtitle = Language.GetString("NULL_SUBTITLE");
			}
			bestObservedSubtitle = "<sprite name=\"CloudLeft\" tint=1> " + bestObservedSubtitle + "<sprite name=\"CloudRight\" tint=1>";
		}
		HealthComponent healthComponent = memory.cachedBody.healthComponent;
		memory.maxObservedMaxHealth = Mathf.Max(memory.maxObservedMaxHealth, healthComponent.fullCombinedHealth);
		memory.lastObservedHealth = healthComponent.combinedHealth;
	}

	private int AddBossMemory(CharacterMaster master)
	{
		BossMemory bossMemory = default(BossMemory);
		bossMemory.masterInstanceId = master.netId;
		bossMemory.maxObservedMaxHealth = 0f;
		bossMemory.cachedMaster = master;
		BossMemory value = bossMemory;
		ArrayUtils.ArrayAppend(ref bossMemories, ref bossMemoryCount, in value);
		return bossMemoryCount - 1;
	}

	private void UpdateBossMemories()
	{
		totalMaxObservedMaxHealth = 0f;
		totalObservedHealth = 0f;
		for (int i = 0; i < bossMemoryCount; i++)
		{
			ref BossMemory reference = ref bossMemories[i];
			UpdateObservations(ref reference);
			totalMaxObservedMaxHealth += reference.maxObservedMaxHealth;
			totalObservedHealth += Mathf.Max(reference.lastObservedHealth, 0f);
		}
	}

	public static int GetTotalBossCount()
	{
		if (totalBossCountDirty)
		{
			totalBossCountDirty = false;
			lastTotalBossCount = 0;
			List<BossGroup> instancesList = InstanceTracker.GetInstancesList<BossGroup>();
			for (int i = 0; i < instancesList.Count; i++)
			{
				lastTotalBossCount += instancesList[i].combatSquad.readOnlyMembersList.Count;
			}
		}
		return lastTotalBossCount;
	}

	public static BossGroup FindBossGroup(CharacterBody body)
	{
		if (!body || !body.isBoss)
		{
			return null;
		}
		CharacterMaster master = body.master;
		if (!master)
		{
			return null;
		}
		List<BossGroup> instancesList = InstanceTracker.GetInstancesList<BossGroup>();
		for (int i = 0; i < instancesList.Count; i++)
		{
			BossGroup bossGroup = instancesList[i];
			if (bossGroup.combatSquad.ContainsMember(master))
			{
				return bossGroup;
			}
		}
		return null;
	}

	public void ReportObjective(CharacterMaster master, List<ObjectivePanelController.ObjectiveSourceDescriptor> output)
	{
		if (combatSquad.readOnlyMembersList.Count != 0)
		{
			output.Add(new ObjectivePanelController.ObjectiveSourceDescriptor
			{
				source = this,
				master = master,
				objectiveType = typeof(DefeatBossObjectiveTracker)
			});
		}
	}
}
