using System;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(PurchaseInteraction))]
public class ShrineHealingBehavior : ShrineBehavior
{
	public GameObject wardPrefab;

	private GameObject wardInstance;

	public float baseRadius;

	public float radiusBonusPerPurchase;

	public int maxPurchaseCount;

	public float costMultiplierPerPurchase;

	public Transform symbolTransform;

	private PurchaseInteraction purchaseInteraction;

	private float refreshTimer;

	private const float refreshDuration = 2f;

	private bool waitingForRefresh;

	private HealingWard healingWard;

	public int purchaseCount { get; private set; }

	public static event Action<ShrineHealingBehavior, Interactor> onActivated;

	public override int GetNetworkChannel()
	{
		return QosChannelIndex.defaultReliable.intVal;
	}

	private void Awake()
	{
		purchaseInteraction = GetComponent<PurchaseInteraction>();
	}

	public void FixedUpdate()
	{
		if (waitingForRefresh)
		{
			refreshTimer -= Time.fixedDeltaTime;
			if (refreshTimer <= 0f && purchaseCount < maxPurchaseCount)
			{
				purchaseInteraction.SetAvailable(newAvailable: true);
				purchaseInteraction.Networkcost = (int)((float)purchaseInteraction.cost * costMultiplierPerPurchase);
				waitingForRefresh = false;
			}
		}
	}

	[Server]
	private void SetWardEnabled(bool enableWard)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.ShrineHealingBehavior::SetWardEnabled(System.Boolean)' called on client");
		}
		else if (enableWard != (bool)wardInstance)
		{
			if (enableWard)
			{
				wardInstance = UnityEngine.Object.Instantiate(wardPrefab, base.transform.position, base.transform.rotation);
				wardInstance.GetComponent<TeamFilter>().teamIndex = TeamIndex.Player;
				healingWard = wardInstance.GetComponent<HealingWard>();
				NetworkServer.Spawn(wardInstance);
			}
			else
			{
				UnityEngine.Object.Destroy(wardInstance);
				wardInstance = null;
				healingWard = null;
			}
		}
	}

	[Server]
	public void AddShrineStack(Interactor activator)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.ShrineHealingBehavior::AddShrineStack(RoR2.Interactor)' called on client");
			return;
		}
		SetWardEnabled(enableWard: true);
		Chat.SendBroadcastChat(new Chat.SubjectFormatChatMessage
		{
			subjectAsCharacterBody = activator.gameObject.GetComponent<CharacterBody>(),
			baseToken = "SHRINE_HEALING_USE_MESSAGE"
		});
		waitingForRefresh = true;
		purchaseCount++;
		float networkradius = baseRadius + radiusBonusPerPurchase * (float)(purchaseCount - 1);
		healingWard.Networkradius = networkradius;
		refreshTimer = 2f;
		EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ShrineUseEffect"), new EffectData
		{
			origin = base.transform.position,
			rotation = Quaternion.identity,
			scale = 1f,
			color = Color.green
		}, transmit: true);
		if (purchaseCount >= maxPurchaseCount)
		{
			symbolTransform.gameObject.SetActive(value: false);
			CallRpcSetPingable(value: false);
		}
		ShrineHealingBehavior.onActivated?.Invoke(this, activator);
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
