using System;
using HG;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RoR2.Orbs;

public class ItemTransferOrb : Orb
{
	public ItemIndex itemIndex;

	public int stack;

	public Inventory inventoryToGrantTo;

	public Action<ItemTransferOrb> onArrival = DefaultOnArrivalBehavior;

	public NetworkIdentity orbEffectTargetObjectOverride;

	public float travelDuration = 1f;

	private static GameObject orbEffectPrefab;

	[InitDuringStartup]
	private static void Init()
	{
		AsyncOperationHandle<GameObject> asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/Effects/OrbEffects/ItemTransferOrbEffect");
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
		{
			orbEffectPrefab = x.Result;
		};
	}

	public override void Begin()
	{
		base.duration = travelDuration;
		if ((bool)target || (bool)orbEffectTargetObjectOverride)
		{
			EffectData effectData = new EffectData
			{
				origin = origin,
				genericFloat = base.duration,
				genericUInt = Util.IntToUintPlusOne((int)itemIndex)
			};
			if ((bool)orbEffectTargetObjectOverride)
			{
				effectData.SetNetworkedObjectReference(orbEffectTargetObjectOverride.gameObject);
			}
			else
			{
				effectData.SetHurtBoxReference(target);
			}
			EffectManager.SpawnEffect(orbEffectPrefab, effectData, transmit: true);
		}
	}

	public override void OnArrival()
	{
		onArrival?.Invoke(this);
	}

	public static void DefaultOnArrivalBehavior(ItemTransferOrb orb)
	{
		if ((bool)orb.inventoryToGrantTo)
		{
			orb.inventoryToGrantTo.GiveItem(orb.itemIndex, orb.stack);
		}
	}

	public static ItemTransferOrb DispatchItemTransferOrb(Vector3 origin, Inventory inventoryToGrantTo, ItemIndex itemIndex, int itemStack, Action<ItemTransferOrb> onArrivalBehavior = null, Either<NetworkIdentity, HurtBox> orbDestinationOverride = default(Either<NetworkIdentity, HurtBox>))
	{
		if (onArrivalBehavior == null)
		{
			onArrivalBehavior = DefaultOnArrivalBehavior;
		}
		ItemTransferOrb itemTransferOrb = new ItemTransferOrb();
		itemTransferOrb.origin = origin;
		itemTransferOrb.inventoryToGrantTo = inventoryToGrantTo;
		itemTransferOrb.itemIndex = itemIndex;
		itemTransferOrb.stack = itemStack;
		itemTransferOrb.onArrival = onArrivalBehavior;
		NetworkIdentity a = orbDestinationOverride.a;
		HurtBox b = orbDestinationOverride.b;
		if (!b)
		{
			if ((bool)inventoryToGrantTo)
			{
				CharacterMaster component = inventoryToGrantTo.GetComponent<CharacterMaster>();
				if ((bool)component)
				{
					CharacterBody body = component.GetBody();
					if ((bool)body)
					{
						itemTransferOrb.target = body.mainHurtBox;
					}
				}
			}
			if ((bool)a)
			{
				itemTransferOrb.orbEffectTargetObjectOverride = a;
			}
		}
		else
		{
			itemTransferOrb.target = b;
		}
		OrbManager.instance.AddOrb(itemTransferOrb);
		return itemTransferOrb;
	}
}
