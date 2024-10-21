using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(PurchaseInteraction))]
public class ShrineRestackBehavior : ShrineBehavior
{
	public int maxPurchaseCount;

	public float costMultiplierPerPurchase;

	public Transform symbolTransform;

	private PurchaseInteraction purchaseInteraction;

	private int purchaseCount;

	private float refreshTimer;

	private const float refreshDuration = 2f;

	private bool waitingForRefresh;

	private Xoroshiro128Plus rng;

	public override int GetNetworkChannel()
	{
		return QosChannelIndex.defaultReliable.intVal;
	}

	private void Start()
	{
		purchaseInteraction = GetComponent<PurchaseInteraction>();
		if (NetworkServer.active)
		{
			rng = new Xoroshiro128Plus(Run.instance.stageRng.nextUlong);
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
				purchaseInteraction.Networkcost = (int)((float)purchaseInteraction.cost * costMultiplierPerPurchase);
				waitingForRefresh = false;
			}
		}
	}

	[Server]
	public void AddShrineStack(Interactor interactor)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.ShrineRestackBehavior::AddShrineStack(RoR2.Interactor)' called on client");
			return;
		}
		waitingForRefresh = true;
		CharacterBody component = interactor.GetComponent<CharacterBody>();
		if ((bool)component && (bool)component.master)
		{
			Inventory inventory = component.master.inventory;
			if ((bool)inventory)
			{
				inventory.ShrineRestackInventory(rng);
				Chat.SendBroadcastChat(new Chat.SubjectFormatChatMessage
				{
					subjectAsCharacterBody = component,
					baseToken = "SHRINE_RESTACK_USE_MESSAGE"
				});
			}
		}
		EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ShrineUseEffect"), new EffectData
		{
			origin = base.transform.position,
			rotation = Quaternion.identity,
			scale = 1f,
			color = new Color(1f, 0.23f, 0.6337214f)
		}, transmit: true);
		purchaseCount++;
		refreshTimer = 2f;
		if (purchaseCount >= maxPurchaseCount)
		{
			symbolTransform.gameObject.SetActive(value: false);
			CallRpcSetPingable(value: false);
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
