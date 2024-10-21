using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(PurchaseInteraction))]
public class ShrineBloodBehavior : ShrineBehavior
{
	public int maxPurchaseCount;

	public float goldToPaidHpRatio = 0.5f;

	public float costMultiplierPerPurchase;

	public Transform symbolTransform;

	private PurchaseInteraction purchaseInteraction;

	private int purchaseCount;

	private float refreshTimer;

	private const float refreshDuration = 2f;

	private bool waitingForRefresh;

	public override int GetNetworkChannel()
	{
		return QosChannelIndex.defaultReliable.intVal;
	}

	private void Start()
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
				purchaseInteraction.Networkcost = (int)(100f * (1f - Mathf.Pow(1f - (float)purchaseInteraction.cost / 100f, costMultiplierPerPurchase)));
				waitingForRefresh = false;
			}
		}
	}

	[Server]
	public void AddShrineStack(Interactor interactor)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.ShrineBloodBehavior::AddShrineStack(RoR2.Interactor)' called on client");
			return;
		}
		waitingForRefresh = true;
		CharacterBody component = interactor.GetComponent<CharacterBody>();
		if ((bool)component)
		{
			uint num = (uint)(component.healthComponent.fullCombinedHealth * (float)purchaseInteraction.cost / 100f * goldToPaidHpRatio);
			if ((bool)component.master)
			{
				if (component.inventory.GetItemCount(DLC2Content.Items.OnLevelUpFreeUnlock) > 0)
				{
					component.master.GiveExperience((ulong)((float)num * 2f));
				}
				else
				{
					component.master.GiveMoney(num);
				}
				Chat.SubjectFormatChatMessage subjectFormatChatMessage = new Chat.SubjectFormatChatMessage();
				subjectFormatChatMessage.subjectAsCharacterBody = component;
				subjectFormatChatMessage.baseToken = "SHRINE_BLOOD_USE_MESSAGE";
				subjectFormatChatMessage.paramTokens = new string[1] { num.ToString() };
				Chat.SendBroadcastChat(subjectFormatChatMessage);
			}
		}
		EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ShrineUseEffect"), new EffectData
		{
			origin = base.transform.position,
			rotation = Quaternion.identity,
			scale = 1f,
			color = Color.red
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
