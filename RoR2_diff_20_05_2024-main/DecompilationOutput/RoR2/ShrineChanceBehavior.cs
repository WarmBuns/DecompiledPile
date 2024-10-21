using System;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;

namespace RoR2;

[RequireComponent(typeof(PurchaseInteraction))]
public class ShrineChanceBehavior : ShrineBehavior
{
	public int maxPurchaseCount;

	public float costMultiplierPerPurchase;

	public float failureChance;

	public PickupDropTable dropTable;

	public PickupDropTable chanceDollDropTable;

	public Transform symbolTransform;

	public Transform dropletOrigin;

	public GameObject effectPrefabShrineRewardJackpot;

	public GameObject effectPrefabShrineRewardNormal;

	[FormerlySerializedAs("shrineColor")]
	public Color colorShrineRewardNormal;

	[FormerlySerializedAs("chanceDollSuccessColor")]
	public Color colorShrineRewardJackpot;

	private Color colorToEmit;

	private bool chanceDollWin;

	private PurchaseInteraction purchaseInteraction;

	private int successfulPurchaseCount;

	private float refreshTimer;

	private const float refreshDuration = 2f;

	private bool waitingForRefresh;

	private Xoroshiro128Plus rng;

	public Transform firstDropletOrigin;

	public Transform secondDropletOrigin;

	[Header("Deprecated")]
	public float failureWeight;

	public float equipmentWeight;

	public float tier1Weight;

	public float tier2Weight;

	public float tier3Weight;

	public static event Action<bool, Interactor> onShrineChancePurchaseGlobal;

	private void Awake()
	{
		purchaseInteraction = GetComponent<PurchaseInteraction>();
	}

	public void Start()
	{
		if (NetworkServer.active)
		{
			rng = new Xoroshiro128Plus(Run.instance.treasureRng.nextUlong);
		}
	}

	public void FixedUpdate()
	{
		if (waitingForRefresh)
		{
			refreshTimer -= Time.fixedDeltaTime;
			if (refreshTimer <= 0f && successfulPurchaseCount < maxPurchaseCount)
			{
				purchaseInteraction.SetAvailable(newAvailable: true);
				purchaseInteraction.Networkcost = (int)((float)purchaseInteraction.cost * costMultiplierPerPurchase);
				waitingForRefresh = false;
			}
		}
	}

	[Server]
	public void AddShrineStack(Interactor activator)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.ShrineChanceBehavior::AddShrineStack(RoR2.Interactor)' called on client");
			return;
		}
		PickupIndex pickupIndex = PickupIndex.none;
		CharacterBody component = activator.GetComponent<CharacterBody>();
		if (component == null)
		{
			return;
		}
		Inventory inventory = component.inventory;
		if (inventory == null)
		{
			return;
		}
		if ((bool)dropTable)
		{
			if (rng.nextNormalizedFloat > failureChance)
			{
				if ((bool)chanceDollDropTable && inventory.GetItemCount(DLC2Content.Items.ExtraShrineItem) > 0)
				{
					int itemCount = inventory.GetItemCount(DLC2Content.Items.ExtraShrineItem);
					if (Util.CheckRoll(30 + itemCount * 10, component.master))
					{
						pickupIndex = chanceDollDropTable.GenerateDrop(rng);
						chanceDollWin = true;
					}
					else
					{
						pickupIndex = dropTable.GenerateDrop(rng);
						chanceDollWin = false;
					}
				}
				else
				{
					pickupIndex = dropTable.GenerateDrop(rng);
					chanceDollWin = false;
				}
			}
			else
			{
				chanceDollWin = false;
			}
		}
		else
		{
			PickupIndex none = PickupIndex.none;
			PickupIndex value = rng.NextElementUniform(Run.instance.availableTier1DropList);
			PickupIndex value2 = rng.NextElementUniform(Run.instance.availableTier2DropList);
			PickupIndex value3 = rng.NextElementUniform(Run.instance.availableTier3DropList);
			PickupIndex value4 = rng.NextElementUniform(Run.instance.availableEquipmentDropList);
			WeightedSelection<PickupIndex> weightedSelection = new WeightedSelection<PickupIndex>();
			weightedSelection.AddChoice(none, failureWeight);
			weightedSelection.AddChoice(value, tier1Weight);
			weightedSelection.AddChoice(value2, tier2Weight);
			weightedSelection.AddChoice(value3, tier3Weight);
			weightedSelection.AddChoice(value4, equipmentWeight);
			pickupIndex = weightedSelection.Evaluate(rng.nextNormalizedFloat);
		}
		bool flag = pickupIndex == PickupIndex.none;
		string baseToken;
		if (flag)
		{
			baseToken = "SHRINE_CHANCE_FAIL_MESSAGE";
		}
		else
		{
			baseToken = "SHRINE_CHANCE_SUCCESS_MESSAGE";
			successfulPurchaseCount++;
			PickupDropletController.CreatePickupDroplet(pickupIndex, dropletOrigin.position, dropletOrigin.forward * 20f);
		}
		Chat.SendBroadcastChat(new Chat.SubjectFormatChatMessage
		{
			subjectAsCharacterBody = component,
			baseToken = baseToken
		});
		ShrineChanceBehavior.onShrineChancePurchaseGlobal?.Invoke(flag, activator);
		waitingForRefresh = true;
		refreshTimer = 2f;
		if (chanceDollWin)
		{
			EffectManager.SpawnEffect(effectPrefabShrineRewardJackpot, new EffectData
			{
				origin = base.transform.position,
				rotation = Quaternion.identity,
				scale = 1f,
				color = colorShrineRewardJackpot
			}, transmit: true);
		}
		else
		{
			EffectManager.SpawnEffect(effectPrefabShrineRewardNormal, new EffectData
			{
				origin = base.transform.position,
				rotation = Quaternion.identity,
				scale = 1f,
				color = colorShrineRewardNormal
			}, transmit: true);
		}
		if (successfulPurchaseCount >= maxPurchaseCount)
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
