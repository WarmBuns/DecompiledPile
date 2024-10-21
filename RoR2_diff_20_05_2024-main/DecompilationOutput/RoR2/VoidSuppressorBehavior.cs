using System.Collections.Generic;
using RoR2.Items;
using RoR2.Networking;
using Unity;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(PurchaseInteraction))]
public class VoidSuppressorBehavior : NetworkBehaviour
{
	public class SyncListPickupIndex : SyncListStruct<PickupIndex>
	{
		public override void SerializeItem(NetworkWriter writer, PickupIndex item)
		{
			writer.WritePackedUInt32((uint)item.value);
		}

		public override PickupIndex DeserializeItem(NetworkReader reader)
		{
			PickupIndex result = default(PickupIndex);
			result.value = (int)reader.ReadPackedUInt32();
			return result;
		}
	}

	[SerializeField]
	private int maxPurchaseCount;

	[SerializeField]
	private int itemsSuppressedPerPurchase;

	[SerializeField]
	private float costMultiplierPerPurchase;

	[SerializeField]
	private Transform symbolTransform;

	[SerializeField]
	private PurchaseInteraction purchaseInteraction;

	[SerializeField]
	private GameObject effectPrefab;

	[SerializeField]
	private Color effectColor;

	[SerializeField]
	private PickupDisplay[] pickupDisplays;

	[SerializeField]
	private int numItemsToReveal;

	[SerializeField]
	private bool transformItems;

	[SerializeField]
	private string animatorSuppressName;

	[SerializeField]
	private string animatorIsAvailableName;

	[SerializeField]
	private string animatorIsPlayerNearbyName;

	[SerializeField]
	private Animator animator;

	[SerializeField]
	private NetworkAnimator networkAnimator;

	[SerializeField]
	private string revealedName;

	[SerializeField]
	private string revealedContext;

	[SerializeField]
	private float useRefreshDelay = 2f;

	[SerializeField]
	private float itemRefreshDelay = 1f;

	private int purchaseCount;

	private float timeUntilUseRefresh;

	private float timeUntilItemRefresh;

	private Xoroshiro128Plus rng;

	private SyncListPickupIndex nextItemsToSuppress = new SyncListPickupIndex();

	private ItemIndex transformItemIndex;

	private bool hasRevealed;

	private static int kListnextItemsToSuppress;

	public override int GetNetworkChannel()
	{
		return QosChannelIndex.defaultReliable.intVal;
	}

	private void Start()
	{
		if (NetworkServer.active)
		{
			rng = new Xoroshiro128Plus(Run.instance.treasureRng.nextUlong);
		}
	}

	public override void OnStartClient()
	{
		nextItemsToSuppress.Callback = OnItemsUpdated;
	}

	public void FixedUpdate()
	{
		if (purchaseCount >= maxPurchaseCount)
		{
			return;
		}
		if (timeUntilUseRefresh > 0f)
		{
			timeUntilUseRefresh -= Time.fixedDeltaTime;
			if (timeUntilUseRefresh <= 0f)
			{
				animator.SetBool(animatorSuppressName, value: false);
				purchaseInteraction.SetAvailable(newAvailable: true);
				purchaseInteraction.Networkcost = (int)((float)purchaseInteraction.cost * costMultiplierPerPurchase);
			}
		}
		if (timeUntilItemRefresh > 0f)
		{
			timeUntilItemRefresh -= Time.fixedDeltaTime;
			if (timeUntilItemRefresh <= 0f)
			{
				nextItemsToSuppress.Clear();
				RefreshItems();
			}
		}
	}

	[Server]
	public void OnInteraction(Interactor interactor)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.VoidSuppressorBehavior::OnInteraction(RoR2.Interactor)' called on client");
			return;
		}
		timeUntilUseRefresh = useRefreshDelay;
		if (hasRevealed)
		{
			timeUntilItemRefresh = itemRefreshDelay;
			if (nextItemsToSuppress != null)
			{
				CharacterBody component = interactor.GetComponent<CharacterBody>();
				for (int i = 0; i < itemsSuppressedPerPurchase && i < nextItemsToSuppress.Count; i++)
				{
					ItemIndex itemIndex = nextItemsToSuppress[i].itemIndex;
					if (SuppressedItemManager.SuppressItem(itemIndex, transformItemIndex))
					{
						ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
						ItemTierDef itemTierDef = ItemTierCatalog.GetItemTierDef(itemDef.tier);
						ColoredTokenChatMessage coloredTokenChatMessage = new ColoredTokenChatMessage();
						coloredTokenChatMessage.subjectAsCharacterBody = component;
						coloredTokenChatMessage.baseToken = "VOID_SUPPRESSOR_USE_MESSAGE";
						coloredTokenChatMessage.paramTokens = new string[1] { itemDef.nameToken };
						coloredTokenChatMessage.paramColors = new Color32[1] { ColorCatalog.GetColor(itemTierDef.colorIndex) };
						Chat.SendBroadcastChat(coloredTokenChatMessage);
					}
				}
			}
			EffectManager.SpawnEffect(effectPrefab, new EffectData
			{
				origin = base.transform.position,
				rotation = Quaternion.identity,
				scale = 1f,
				color = effectColor
			}, transmit: true);
			purchaseCount++;
			if (purchaseCount >= maxPurchaseCount)
			{
				if ((bool)symbolTransform)
				{
					symbolTransform.gameObject.SetActive(value: false);
				}
				animator.SetBool(animatorIsAvailableName, value: false);
			}
			animator.SetBool(animatorSuppressName, value: true);
		}
		else
		{
			hasRevealed = true;
			animator.SetBool(animatorIsAvailableName, value: true);
			purchaseInteraction.NetworkcontextToken = revealedContext;
			purchaseInteraction.NetworkdisplayNameToken = revealedName;
			RefreshItems();
		}
	}

	[Server]
	public void RefreshItems()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.VoidSuppressorBehavior::RefreshItems()' called on client");
		}
		else
		{
			if (timeUntilItemRefresh > 0f)
			{
				return;
			}
			for (int num = nextItemsToSuppress.Count - 1; num >= 0; num--)
			{
				if (SuppressedItemManager.HasItemBeenSuppressed(nextItemsToSuppress[num].itemIndex))
				{
					nextItemsToSuppress.RemoveAt(num);
				}
			}
			int num2 = itemsSuppressedPerPurchase - nextItemsToSuppress.Count;
			if (num2 > 0)
			{
				List<PickupIndex> list = null;
				PickupIndex item = PickupIndex.none;
				switch (purchaseCount)
				{
				case 0:
					list = new List<PickupIndex>(Run.instance.availableTier1DropList);
					item = PickupCatalog.FindPickupIndex(DLC1Content.Items.ScrapWhiteSuppressed.itemIndex);
					transformItemIndex = (transformItems ? DLC1Content.Items.ScrapWhiteSuppressed.itemIndex : ItemIndex.None);
					break;
				case 1:
					list = new List<PickupIndex>(Run.instance.availableTier2DropList);
					item = PickupCatalog.FindPickupIndex(DLC1Content.Items.ScrapGreenSuppressed.itemIndex);
					transformItemIndex = (transformItems ? DLC1Content.Items.ScrapGreenSuppressed.itemIndex : ItemIndex.None);
					break;
				case 2:
					list = new List<PickupIndex>(Run.instance.availableTier3DropList);
					item = PickupCatalog.FindPickupIndex(DLC1Content.Items.ScrapRedSuppressed.itemIndex);
					transformItemIndex = (transformItems ? DLC1Content.Items.ScrapRedSuppressed.itemIndex : ItemIndex.None);
					break;
				}
				if (list != null && list.Count > 0)
				{
					list.Remove(item);
					foreach (PickupIndex item2 in nextItemsToSuppress)
					{
						list.Remove(item2);
					}
					Util.ShuffleList(list, rng);
					int num3 = list.Count - num2;
					if (num3 > 0)
					{
						list.RemoveRange(num2, num3);
					}
					foreach (PickupIndex item3 in list)
					{
						nextItemsToSuppress.Add(item3);
					}
				}
			}
			RefreshPickupDisplays();
			if (nextItemsToSuppress.Count != 0)
			{
				return;
			}
			if (purchaseCount < maxPurchaseCount)
			{
				purchaseCount++;
				RefreshItems();
				return;
			}
			if ((bool)symbolTransform)
			{
				symbolTransform.gameObject.SetActive(value: false);
			}
			purchaseInteraction.SetAvailable(newAvailable: false);
			animator.SetBool(animatorIsAvailableName, value: false);
		}
	}

	public void OnPlayerNearby()
	{
		animator.SetBool(animatorIsPlayerNearbyName, value: true);
	}

	public void OnPlayerFar()
	{
		animator.SetBool(animatorIsPlayerNearbyName, value: false);
	}

	private void OnItemsUpdated(SyncList<PickupIndex>.Operation op, int index)
	{
		RefreshPickupDisplays();
	}

	private void RefreshPickupDisplays()
	{
		for (int i = 0; i < pickupDisplays.Length; i++)
		{
			if (i < nextItemsToSuppress.Count)
			{
				pickupDisplays[i].gameObject.SetActive(value: true);
				pickupDisplays[i].SetPickupIndex(nextItemsToSuppress[i], i >= numItemsToReveal);
			}
			else
			{
				pickupDisplays[i].gameObject.SetActive(value: false);
			}
		}
	}

	private void UNetVersion()
	{
	}

	protected static void InvokeSyncListnextItemsToSuppress(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("SyncList nextItemsToSuppress called on server.");
		}
		else
		{
			((VoidSuppressorBehavior)obj).nextItemsToSuppress.HandleMsg(reader);
		}
	}

	static VoidSuppressorBehavior()
	{
		kListnextItemsToSuppress = 164192093;
		NetworkBehaviour.RegisterSyncListDelegate(typeof(VoidSuppressorBehavior), kListnextItemsToSuppress, InvokeSyncListnextItemsToSuppress);
		NetworkCRC.RegisterBehaviour("VoidSuppressorBehavior", 0);
	}

	private void Awake()
	{
		nextItemsToSuppress.InitializeBehaviour(this, kListnextItemsToSuppress);
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			GeneratedNetworkCode._WriteStructSyncListPickupIndex_VoidSuppressorBehavior(writer, nextItemsToSuppress);
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
			GeneratedNetworkCode._WriteStructSyncListPickupIndex_VoidSuppressorBehavior(writer, nextItemsToSuppress);
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
			GeneratedNetworkCode._ReadStructSyncListPickupIndex_VoidSuppressorBehavior(reader, nextItemsToSuppress);
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			GeneratedNetworkCode._ReadStructSyncListPickupIndex_VoidSuppressorBehavior(reader, nextItemsToSuppress);
		}
	}

	public override void PreStartClient()
	{
	}
}
