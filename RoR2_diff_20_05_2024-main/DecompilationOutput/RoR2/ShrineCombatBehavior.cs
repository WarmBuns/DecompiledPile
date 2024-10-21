using System;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(CombatDirector))]
[RequireComponent(typeof(CombatSquad))]
[RequireComponent(typeof(PurchaseInteraction))]
public class ShrineCombatBehavior : ShrineBehavior
{
	public Color shrineEffectColor;

	public int maxPurchaseCount;

	public int baseMonsterCredit;

	public float monsterCreditCoefficientPerPurchase;

	public Transform symbolTransform;

	public GameObject spawnPositionEffectPrefab;

	private CombatDirector combatDirector;

	private PurchaseInteraction purchaseInteraction;

	private int purchaseCount;

	private float refreshTimer;

	private const float refreshDuration = 2f;

	private bool waitingForRefresh;

	private DirectorCard chosenDirectorCard;

	private float monsterCredit => (float)baseMonsterCredit * Stage.instance.entryDifficultyCoefficient * (1f + (float)purchaseCount * (monsterCreditCoefficientPerPurchase - 1f));

	public static event Action<ShrineCombatBehavior> onDefeatedServerGlobal;

	public override int GetNetworkChannel()
	{
		return QosChannelIndex.defaultReliable.intVal;
	}

	private void Awake()
	{
		if (NetworkServer.active)
		{
			purchaseInteraction = GetComponent<PurchaseInteraction>();
			combatDirector = GetComponent<CombatDirector>();
			combatDirector.combatSquad.onDefeatedServer += OnDefeatedServer;
		}
	}

	private void OnDefeatedServer()
	{
		ShrineCombatBehavior.onDefeatedServerGlobal?.Invoke(this);
	}

	private void Start()
	{
		if (NetworkServer.active)
		{
			chosenDirectorCard = combatDirector.SelectMonsterCardForCombatShrine(monsterCredit);
			if (chosenDirectorCard == null)
			{
				purchaseInteraction.SetAvailable(newAvailable: false);
			}
		}
	}

	public void FixedUpdate()
	{
		if (waitingForRefresh)
		{
			refreshTimer -= Time.fixedDeltaTime;
			if (refreshTimer <= 0f && purchaseCount < maxPurchaseCount)
			{
				purchaseInteraction.SetAvailable(newAvailable: true);
				waitingForRefresh = false;
			}
		}
	}

	[Server]
	public void AddShrineStack(Interactor interactor)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.ShrineCombatBehavior::AddShrineStack(RoR2.Interactor)' called on client");
			return;
		}
		waitingForRefresh = true;
		combatDirector.CombatShrineActivation(interactor, monsterCredit, chosenDirectorCard);
		EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ShrineUseEffect"), new EffectData
		{
			origin = base.transform.position,
			rotation = Quaternion.identity,
			scale = 1f,
			color = shrineEffectColor
		}, transmit: true);
		purchaseCount++;
		refreshTimer = 2f;
		if (purchaseCount >= maxPurchaseCount)
		{
			symbolTransform.gameObject.SetActive(value: false);
			CallRpcSetPingable(value: false);
		}
	}

	private void OnValidate()
	{
		if (!GetComponent<CombatDirector>().combatSquad)
		{
			Debug.LogError("ShrineCombatBehavior's sibling CombatDirector must use a CombatSquad.", base.gameObject);
		}
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		bool flag = base.OnSerialize(writer, forceAll);
		bool flag2 = default(bool);
		return flag2 || flag;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
		base.OnDeserialize(reader, initialState);
	}

	public override void PreStartClient()
	{
		base.PreStartClient();
	}
}
