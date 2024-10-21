using System;
using System.Collections.Generic;
using HG;
using RoR2.Orbs;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace RoR2;

public class ItemStealController : NetworkBehaviour
{
	[Serializable]
	private class StolenInventoryInfo : IDisposable
	{
		[SerializeField]
		private ItemStealController owner;

		public Inventory victimInventory;

		public bool allowLending = true;

		public bool showStealOrbs = true;

		private int[] stolenItemStacks;

		private int[] lentItemStacks;

		private int[] itemAcqusitionSet;

		private ItemIndex[] itemAcquisitionOrder;

		[SerializeField]
		private int itemAcquisitionOrderCount;

		[SerializeField]
		private List<ItemTransferOrb> inFlightOrbs;

		public int stolenItemCount => itemAcquisitionOrderCount;

		[SerializeField]
		private Inventory lendeeInventory => owner.lendeeInventory;

		public bool hasOrbsInFlight => inFlightOrbs.Count > 0;

		public bool hasItemsToReclaim { get; private set; }

		public StolenInventoryInfo(ItemStealController owner, Inventory victimInventory)
		{
			this.owner = owner;
			this.victimInventory = victimInventory;
			stolenItemStacks = ItemCatalog.RequestItemStackArray();
			lentItemStacks = ItemCatalog.RequestItemStackArray();
			itemAcqusitionSet = ItemCatalog.RequestItemStackArray();
			itemAcquisitionOrder = ItemCatalog.RequestItemOrderBuffer();
			itemAcquisitionOrderCount = 0;
			inFlightOrbs = new List<ItemTransferOrb>();
		}

		public void Dispose()
		{
			victimInventory = null;
			if (stolenItemStacks != null)
			{
				ItemCatalog.ReturnItemStackArray(stolenItemStacks);
				stolenItemStacks = null;
			}
			if (lentItemStacks != null)
			{
				ItemCatalog.ReturnItemStackArray(lentItemStacks);
				lentItemStacks = null;
			}
			if (itemAcqusitionSet != null)
			{
				ItemCatalog.ReturnItemStackArray(itemAcqusitionSet);
				itemAcqusitionSet = null;
			}
			if (itemAcquisitionOrder != null)
			{
				ItemCatalog.ReturnItemOrderBuffer(itemAcquisitionOrder);
				itemAcquisitionOrder = null;
			}
			inFlightOrbs = null;
		}

		public bool StealNewestItem(int maxStackToSteal = int.MaxValue, bool? useOrbOverride = null)
		{
			if (!victimInventory)
			{
				return false;
			}
			List<ItemIndex> list = victimInventory.itemAcquisitionOrder;
			for (int num = list.Count - 1; num >= 0; num--)
			{
				ItemIndex itemIndex = list[num];
				if (StealItem(itemIndex, maxStackToSteal, useOrbOverride) > 0)
				{
					return true;
				}
			}
			return false;
		}

		private int StealItem(ItemIndex itemIndex, int maxStackToSteal, bool? useOrbOverride = null)
		{
			if (!owner.itemStealFilter(itemIndex))
			{
				return 0;
			}
			int itemCount = victimInventory.GetItemCount(itemIndex);
			int itemsToSteal = Math.Min(itemCount, maxStackToSteal);
			if (itemsToSteal > 0)
			{
				victimInventory.RemoveItem(itemIndex, itemsToSteal);
				Vector3? vector = ((useOrbOverride ?? showStealOrbs) ? FindInventoryOrbOrigin(victimInventory) : null);
				if (vector.HasValue)
				{
					ItemTransferOrb item = ItemTransferOrb.DispatchItemTransferOrb(vector.Value, null, itemIndex, itemsToSteal, delegate(ItemTransferOrb orb)
					{
						GiveItemToSelf(itemIndex, itemsToSteal);
						inFlightOrbs.Remove(orb);
					}, owner.networkIdentity);
					inFlightOrbs.Add(item);
				}
				else
				{
					GiveItemToSelf(itemIndex, itemsToSteal);
				}
			}
			return itemsToSteal;
		}

		private void RegisterItemAsAcquired(ItemIndex itemIndex)
		{
			ref int reference = ref itemAcqusitionSet[(int)itemIndex];
			if (reference == 0)
			{
				reference = 1;
				itemAcquisitionOrder[itemAcquisitionOrderCount++] = itemIndex;
			}
		}

		public bool LendNewestStolenItem(bool useOrb, int maxStackToGive = int.MaxValue)
		{
			for (int num = itemAcquisitionOrderCount - 1; num >= 0; num--)
			{
				ItemIndex itemIndex = itemAcquisitionOrder[num];
				if (LendStolenItem(itemIndex, useOrb, maxStackToGive) > 0)
				{
					return true;
				}
			}
			return false;
		}

		public void LendAllItems(bool useOrb, int maxStackToGive = int.MaxValue)
		{
			for (int num = itemAcquisitionOrderCount - 1; num >= 0; num--)
			{
				ItemIndex itemIndex = itemAcquisitionOrder[num];
				LendStolenItem(itemIndex, useOrb, maxStackToGive);
			}
		}

		private int LendStolenItem(ItemIndex itemIndex, bool useOrb, int maxStackToGive = int.MaxValue)
		{
			if (!lendeeInventory)
			{
				return 0;
			}
			int num = TakeItemFromSelf(itemIndex, maxStackToGive);
			if (num > 0)
			{
				if (useOrb)
				{
					ItemTransferOrb item = ItemTransferOrb.DispatchItemTransferOrb(owner.orbTargetTransform.position, lendeeInventory, itemIndex, num, delegate(ItemTransferOrb orb)
					{
						GiveItemToLendee(orb.itemIndex, orb.stack);
						inFlightOrbs.Remove(orb);
					});
					inFlightOrbs.Add(item);
				}
				else
				{
					GiveItemToLendee(itemIndex, num);
				}
			}
			hasItemsToReclaim = true;
			return num;
		}

		private int ReclaimLentItem(ItemIndex itemToReclaim, int maxStackToReclaim = int.MaxValue, bool? useOrbOverride = null)
		{
			int num = TakeItemFromLendee(itemToReclaim, maxStackToReclaim);
			if (num > 0)
			{
				Vector3? orbOrigin = ((useOrbOverride ?? showStealOrbs) ? FindInventoryOrbOrigin(lendeeInventory) : null);
				DispatchOrbOrGiveItem(victimInventory, itemToReclaim, num, orbOrigin, useOrbOverride);
			}
			return num;
		}

		private int ReclaimStolenItem(ItemIndex itemToReclaim, int maxStacksToReclaim = int.MaxValue, bool? useOrbOverride = null)
		{
			int num = TakeItemFromSelf(itemToReclaim, maxStacksToReclaim);
			if (num > 0)
			{
				DispatchOrbOrGiveItem(victimInventory, itemToReclaim, num, owner.orbTargetTransform.position, useOrbOverride);
			}
			return num;
		}

		private void DispatchOrbOrGiveItem(Inventory inventoryToGrantTo, ItemIndex itemToReclaim, int stacks, Vector3? orbOrigin, bool? useOrbOverride = null)
		{
			if (!inventoryToGrantTo)
			{
				return;
			}
			if (orbOrigin.HasValue && (useOrbOverride ?? showStealOrbs))
			{
				ItemTransferOrb item = ItemTransferOrb.DispatchItemTransferOrb(orbOrigin.Value, victimInventory, itemToReclaim, stacks, delegate(ItemTransferOrb orb)
				{
					ItemTransferOrb.DefaultOnArrivalBehavior(orb);
					inFlightOrbs.Remove(orb);
				});
				inFlightOrbs.Add(item);
			}
			else if ((bool)inventoryToGrantTo)
			{
				inventoryToGrantTo.GiveItem(itemToReclaim, stacks);
			}
		}

		public bool ReclaimNewestLentItem(int maxStackToReclaim = int.MaxValue)
		{
			for (int num = itemAcquisitionOrderCount - 1; num >= 0; num--)
			{
				ItemIndex itemToReclaim = itemAcquisitionOrder[num];
				if (ReclaimLentItem(itemToReclaim, maxStackToReclaim) > 0)
				{
					return true;
				}
			}
			hasItemsToReclaim = false;
			return false;
		}

		public bool ReclaimOldestLentItem(int maxStackToReclaim = int.MaxValue)
		{
			for (int i = 0; i < itemAcquisitionOrderCount; i++)
			{
				ItemIndex itemToReclaim = itemAcquisitionOrder[i];
				if (ReclaimLentItem(itemToReclaim, maxStackToReclaim) > 0)
				{
					return true;
				}
			}
			hasItemsToReclaim = false;
			return false;
		}

		private void ForceAllOrbsToFinish()
		{
			while (inFlightOrbs.Count > 0)
			{
				OrbManager.instance.ForceImmediateArrival(inFlightOrbs[0]);
			}
		}

		private void GiveItemToSelf(ItemIndex itemIndex, int stack)
		{
			stolenItemStacks[(int)itemIndex] += stack;
			RegisterItemAsAcquired(itemIndex);
		}

		private int TakeItemFromSelf(ItemIndex itemIndex, int maxStackToTake)
		{
			ref int reference = ref stolenItemStacks[(int)itemIndex];
			int num = Math.Min(reference, maxStackToTake);
			reference -= num;
			return num;
		}

		private void GiveItemToLendee(ItemIndex itemIndex, int stack)
		{
			lentItemStacks[(int)itemIndex] += stack;
			if ((bool)lendeeInventory && allowLending && owner.itemLendFilter(itemIndex))
			{
				lendeeInventory.GiveItem(itemIndex, stack);
			}
		}

		private int TakeItemFromLendee(ItemIndex itemIndex, int maxStackToTake)
		{
			ref int reference = ref lentItemStacks[(int)itemIndex];
			int num = Math.Min(reference, maxStackToTake);
			if ((bool)lendeeInventory && owner.itemLendFilter(itemIndex))
			{
				lendeeInventory.RemoveItem(itemIndex, num);
			}
			reference -= num;
			return num;
		}

		public void ForceReclaimAllItemsImmediately()
		{
			ForceAllOrbsToFinish();
			for (int num = itemAcquisitionOrderCount - 1; num >= 0; num--)
			{
				ItemIndex itemIndex = itemAcquisitionOrder[num];
				int num2 = 0;
				num2 += TakeItemFromSelf(itemIndex, int.MaxValue);
				num2 += TakeItemFromLendee(itemIndex, int.MaxValue);
				if ((bool)victimInventory)
				{
					victimInventory.GiveItem(itemIndex, num2);
				}
			}
		}

		public void ReclaimAllItems(bool? useOrbOverride)
		{
			for (int num = itemAcquisitionOrderCount - 1; num >= 0; num--)
			{
				ItemIndex itemToReclaim = itemAcquisitionOrder[num];
				ReclaimLentItem(itemToReclaim, int.MaxValue, useOrbOverride);
				ReclaimStolenItem(itemToReclaim, int.MaxValue, useOrbOverride);
			}
			hasItemsToReclaim = false;
		}

		public void TakeBackItemsFromLendee()
		{
			Vector3? vector = FindInventoryOrbOrigin(lendeeInventory);
			for (int num = itemAcquisitionOrderCount - 1; num >= 0; num--)
			{
				ItemIndex itemIndex = itemAcquisitionOrder[num];
				int itemStackToTake = TakeItemFromLendee(itemIndex, int.MaxValue);
				if (vector.HasValue)
				{
					ItemTransferOrb item = ItemTransferOrb.DispatchItemTransferOrb(vector.Value, null, itemIndex, itemStackToTake, delegate(ItemTransferOrb orb)
					{
						GiveItemToSelf(itemIndex, itemStackToTake);
						inFlightOrbs.Remove(orb);
					}, owner.orbTarget);
					inFlightOrbs.Add(item);
				}
				else
				{
					GiveItemToSelf(itemIndex, itemStackToTake);
				}
			}
		}

		private static Vector3? FindInventoryOrbOrigin(Inventory inventory)
		{
			if ((bool)inventory)
			{
				CharacterMaster component = inventory.GetComponent<CharacterMaster>();
				if ((bool)component)
				{
					CharacterBody body = component.GetBody();
					if ((bool)body)
					{
						return body.corePosition;
					}
				}
			}
			return null;
		}
	}

	public UnityEvent onStealBeginServer;

	public UnityEvent onStealFinishServer;

	public UnityEvent onLendingBeginServer;

	public UnityEvent onLendingFinishServer;

	public float stealInterval = 0.2f;

	public HurtBox orbDestinationHurtBoxOverride;

	private NetworkIdentity networkIdentity;

	public Func<ItemIndex, bool> itemStealFilter = DefaultItemFilter;

	public Func<ItemIndex, bool> itemLendFilter = AIItemFilter;

	private StolenInventoryInfo[] stolenInventoryInfos = Array.Empty<StolenInventoryInfo>();

	private bool _inItemSteal;

	private bool _inLending;

	private float stealTimer;

	private float lendTimer;

	private Func<CharacterMaster, bool> stealMasterFilter;

	private static int kRpcRpcOnStealBeginClient;

	private static int kRpcRpcOnStealFinishClient;

	private static int kRpcRpcOnLendingBeginClient;

	private static int kRpcRpcOnLendingFinishClient;

	public Inventory lendeeInventory { get; private set; }

	public bool inItemSteal
	{
		get
		{
			return _inItemSteal;
		}
		private set
		{
			if (_inItemSteal != value)
			{
				if (_inItemSteal)
				{
					hasStolen = true;
				}
				_inItemSteal = value;
				(_inItemSteal ? onStealBeginServer : onStealFinishServer)?.Invoke();
				if (_inItemSteal)
				{
					CallRpcOnStealBeginClient();
				}
				else
				{
					CallRpcOnStealFinishClient();
				}
			}
		}
	}

	public bool hasStolen { get; private set; }

	public bool inLending
	{
		get
		{
			return _inLending;
		}
		private set
		{
			if (_inLending != value)
			{
				_inLending = value;
				(_inLending ? onLendingBeginServer : onLendingFinishServer)?.Invoke();
				if (_inItemSteal)
				{
					CallRpcOnLendingBeginClient();
				}
				else
				{
					CallRpcOnLendingFinishClient();
				}
			}
		}
	}

	private Either<NetworkIdentity, HurtBox> orbTarget
	{
		get
		{
			if ((bool)orbDestinationHurtBoxOverride)
			{
				return orbDestinationHurtBoxOverride;
			}
			return networkIdentity;
		}
	}

	private Transform orbTargetTransform
	{
		get
		{
			if ((bool)orbDestinationHurtBoxOverride)
			{
				return orbDestinationHurtBoxOverride.transform;
			}
			return networkIdentity.transform;
		}
	}

	public event Action onStealBeginClient;

	public event Action onStealFinishClient;

	public event Action onLendingBeginClient;

	public event Action onLendingFinishClient;

	private void Awake()
	{
		networkIdentity = GetComponent<NetworkIdentity>();
	}

	private void OnEnable()
	{
		if (!NetworkServer.active)
		{
			base.enabled = false;
		}
	}

	private void OnDisable()
	{
		inItemSteal = false;
		ForceReclaimAllItemsImmediately();
	}

	private void OnDestroy()
	{
		StolenInventoryInfo[] array = stolenInventoryInfos;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Dispose();
		}
		stolenInventoryInfos = Array.Empty<StolenInventoryInfo>();
	}

	private void FixedUpdate()
	{
		if (inItemSteal)
		{
			stealTimer -= Time.fixedDeltaTime;
			if (stealTimer <= 0f)
			{
				stealTimer = stealInterval;
				StepSteal();
			}
		}
		if (inLending)
		{
			lendTimer -= Time.fixedDeltaTime;
			if (lendTimer <= 0f)
			{
				lendTimer = stealInterval;
				StepLend();
			}
		}
	}

	public static bool DefaultItemFilter(ItemIndex itemIndex)
	{
		ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
		if (itemDef.canRemove)
		{
			return itemDef.DoesNotContainTag(ItemTag.CannotSteal);
		}
		return false;
	}

	public static bool AIItemFilter(ItemIndex itemIndex)
	{
		ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
		if (itemDef.canRemove)
		{
			return itemDef.DoesNotContainTag(ItemTag.AIBlacklist);
		}
		return false;
	}

	public static bool BrotherItemFilter(ItemIndex itemIndex)
	{
		ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
		if (itemDef.canRemove && itemDef.DoesNotContainTag(ItemTag.AIBlacklist))
		{
			return itemDef.DoesNotContainTag(ItemTag.BrotherBlacklist);
		}
		return false;
	}

	public void StartLending(Inventory newLendeeInventory)
	{
		if (lendeeInventory != newLendeeInventory)
		{
			StolenInventoryInfo[] array = stolenInventoryInfos;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].TakeBackItemsFromLendee();
			}
		}
		lendeeInventory = newLendeeInventory;
		inLending = true;
	}

	public void LendImmediately(Inventory newLendeeInventory)
	{
		Debug.LogFormat("LendImmediately({0})", newLendeeInventory);
		StartLending(newLendeeInventory);
		StolenInventoryInfo[] array = stolenInventoryInfos;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].LendAllItems(useOrb: false);
		}
		inLending = false;
	}

	public void StartStealingFromInventory(Inventory victimInventory)
	{
		if (GetStolenInventoryInfo(victimInventory) == null)
		{
			StolenInventoryInfo value = new StolenInventoryInfo(this, victimInventory);
			if ((bool)victimInventory.GetComponent<MinionOwnership>().ownerMaster)
			{
				value.allowLending = false;
				value.showStealOrbs = false;
			}
			ArrayUtils.ArrayAppend(ref stolenInventoryInfos, in value);
			inItemSteal = true;
		}
	}

	public void StepSteal()
	{
		if (stealMasterFilter != null)
		{
			foreach (CharacterMaster readOnlyInstances in CharacterMaster.readOnlyInstancesList)
			{
				if (stealMasterFilter(readOnlyInstances))
				{
					StartStealingFromInventory(readOnlyInstances.inventory);
				}
			}
		}
		bool flag = false;
		StolenInventoryInfo[] array = stolenInventoryInfos;
		foreach (StolenInventoryInfo stolenInventoryInfo in array)
		{
			flag |= stolenInventoryInfo.hasOrbsInFlight;
			flag |= stolenInventoryInfo.StealNewestItem();
		}
		if (!flag)
		{
			inItemSteal = false;
		}
	}

	public void StepLend()
	{
		bool flag = false;
		StolenInventoryInfo[] array = stolenInventoryInfos;
		foreach (StolenInventoryInfo stolenInventoryInfo in array)
		{
			flag |= stolenInventoryInfo.hasOrbsInFlight;
			flag |= stolenInventoryInfo.LendNewestStolenItem(useOrb: true);
		}
		if (!flag)
		{
			inLending = false;
		}
	}

	public int GetStolenItemCount(Inventory victimInventory)
	{
		return GetStolenInventoryInfo(victimInventory)?.stolenItemCount ?? 0;
	}

	private StolenInventoryInfo GetStolenInventoryInfo(Inventory victimInventory)
	{
		for (int i = 0; i < stolenInventoryInfos.Length; i++)
		{
			if (stolenInventoryInfos[i].victimInventory == victimInventory)
			{
				return stolenInventoryInfos[i];
			}
		}
		return null;
	}

	private void ForceReclaimAllItemsImmediately()
	{
		StolenInventoryInfo[] array = stolenInventoryInfos;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].ForceReclaimAllItemsImmediately();
		}
	}

	public void ReclaimAllItems()
	{
		StolenInventoryInfo[] array = stolenInventoryInfos;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].ReclaimAllItems(null);
		}
	}

	public void StartSteal(Func<CharacterMaster, bool> filter)
	{
		if (NetworkServer.active)
		{
			stealMasterFilter = filter;
			inItemSteal = true;
		}
	}

	public bool ReclaimItemForInventory(Inventory inventory, int maxStack = int.MaxValue)
	{
		return GetStolenInventoryInfo(inventory)?.ReclaimNewestLentItem(maxStack) ?? false;
	}

	public void FindDeadOwnersOfStolenItems(List<Inventory> dest)
	{
		List<Inventory> list = CollectionPool<Inventory, List<Inventory>>.RentCollection();
		List<Inventory> list2 = CollectionPool<Inventory, List<Inventory>>.RentCollection();
		for (int i = 0; i < stolenInventoryInfos.Length; i++)
		{
			ref StolenInventoryInfo reference = ref stolenInventoryInfos[i];
			if ((bool)reference.victimInventory)
			{
				CharacterMaster component = reference.victimInventory.GetComponent<CharacterMaster>();
				if ((bool)component && !component.hasBody && reference.hasItemsToReclaim)
				{
					(reference.allowLending ? list : list2).Add(reference.victimInventory);
				}
			}
		}
		dest.AddRange(list);
		dest.AddRange(list2);
		CollectionPool<Inventory, List<Inventory>>.ReturnCollection(list2);
		CollectionPool<Inventory, List<Inventory>>.ReturnCollection(list);
	}

	public void AddValidStolenInventoriesToList(List<Inventory> list)
	{
		if (list == null)
		{
			return;
		}
		StolenInventoryInfo[] array = stolenInventoryInfos;
		foreach (StolenInventoryInfo stolenInventoryInfo in array)
		{
			if ((bool)stolenInventoryInfo.victimInventory)
			{
				list.Add(stolenInventoryInfo.victimInventory);
			}
		}
	}

	[ClientRpc]
	private void RpcOnStealBeginClient()
	{
		this.onStealBeginClient?.Invoke();
	}

	[ClientRpc]
	private void RpcOnStealFinishClient()
	{
		this.onStealFinishClient?.Invoke();
	}

	[ClientRpc]
	private void RpcOnLendingBeginClient()
	{
		this.onLendingBeginClient?.Invoke();
	}

	[ClientRpc]
	private void RpcOnLendingFinishClient()
	{
		this.onLendingFinishClient?.Invoke();
	}

	private void UNetVersion()
	{
	}

	protected static void InvokeRpcRpcOnStealBeginClient(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcOnStealBeginClient called on server.");
		}
		else
		{
			((ItemStealController)obj).RpcOnStealBeginClient();
		}
	}

	protected static void InvokeRpcRpcOnStealFinishClient(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcOnStealFinishClient called on server.");
		}
		else
		{
			((ItemStealController)obj).RpcOnStealFinishClient();
		}
	}

	protected static void InvokeRpcRpcOnLendingBeginClient(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcOnLendingBeginClient called on server.");
		}
		else
		{
			((ItemStealController)obj).RpcOnLendingBeginClient();
		}
	}

	protected static void InvokeRpcRpcOnLendingFinishClient(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcOnLendingFinishClient called on server.");
		}
		else
		{
			((ItemStealController)obj).RpcOnLendingFinishClient();
		}
	}

	public void CallRpcOnStealBeginClient()
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcOnStealBeginClient called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcOnStealBeginClient);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		SendRPCInternal(networkWriter, 0, "RpcOnStealBeginClient");
	}

	public void CallRpcOnStealFinishClient()
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcOnStealFinishClient called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcOnStealFinishClient);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		SendRPCInternal(networkWriter, 0, "RpcOnStealFinishClient");
	}

	public void CallRpcOnLendingBeginClient()
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcOnLendingBeginClient called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcOnLendingBeginClient);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		SendRPCInternal(networkWriter, 0, "RpcOnLendingBeginClient");
	}

	public void CallRpcOnLendingFinishClient()
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcOnLendingFinishClient called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcOnLendingFinishClient);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		SendRPCInternal(networkWriter, 0, "RpcOnLendingFinishClient");
	}

	static ItemStealController()
	{
		kRpcRpcOnStealBeginClient = 166425938;
		NetworkBehaviour.RegisterRpcDelegate(typeof(ItemStealController), kRpcRpcOnStealBeginClient, InvokeRpcRpcOnStealBeginClient);
		kRpcRpcOnStealFinishClient = -1161796992;
		NetworkBehaviour.RegisterRpcDelegate(typeof(ItemStealController), kRpcRpcOnStealFinishClient, InvokeRpcRpcOnStealFinishClient);
		kRpcRpcOnLendingBeginClient = -1214064562;
		NetworkBehaviour.RegisterRpcDelegate(typeof(ItemStealController), kRpcRpcOnLendingBeginClient, InvokeRpcRpcOnLendingBeginClient);
		kRpcRpcOnLendingFinishClient = -1007329532;
		NetworkBehaviour.RegisterRpcDelegate(typeof(ItemStealController), kRpcRpcOnLendingFinishClient, InvokeRpcRpcOnLendingFinishClient);
		NetworkCRC.RegisterBehaviour("ItemStealController", 0);
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		bool result = default(bool);
		return result;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
	}

	public override void PreStartClient()
	{
	}
}
