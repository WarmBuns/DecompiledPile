using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using RoR2.UI;
using Unity;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class ShopTerminalBehavior : NetworkBehaviour, IInspectInfoProvider, IHasInspectHintOverride
{
	[SyncVar(hook = "OnSyncPickupIndex")]
	private PickupIndex pickupIndex = PickupIndex.none;

	[SyncVar(hook = "OnSyncHidden")]
	private bool hidden;

	[SyncVar(hook = "SetHasBeenPurchased")]
	private bool hasBeenPurchased;

	[Tooltip("The PickupDisplay component that should show which item this shop terminal is offering.")]
	public PickupDisplay pickupDisplay;

	[Tooltip("The position from which the drop will be emitted")]
	public Transform dropTransform;

	[Tooltip("The drop table to select a pickup index from - only works if the pickup generates itself")]
	public PickupDropTable dropTable;

	[Tooltip("The velocity with which the drop will be emitted. Rotates with this object.")]
	public Vector3 dropVelocity;

	public Animator animator;

	[Header("Inspect")]
	public InspectDef MysteryItemInspectDef;

	[Tooltip("Set this to a non-empty token to override the default Inspect hint on the HUD.")]
	public string inspectHintOverrideToken;

	[Tooltip("If the hint token's localization wants to have an item inserted at {0}, this must be set.")]
	public bool hintWantsItemFormatting;

	[Tooltip("Determines if purchasing disables inspection")]
	public bool disablesInspectionOnPurchase = true;

	[Tooltip("Whether or not to inspect shop itself or item in the shop")]
	public bool inspectShop;

	[Tooltip("If inspectShop is true, assign overriding InspectDef to inspect")]
	public InspectDef shopInspectDef;

	private IInspectInfoProvider[] cachedInspectInfoProviders;

	[Header("Deprecated")]
	[Tooltip("The tier of items to drop - only works if the pickup generates itself and the dropTable field is empty.")]
	public ItemTier itemTier;

	public ItemTag bannedItemTag;

	[Tooltip("Whether or not the shop terminal should drive itself")]
	public bool selfGeneratePickup = true;

	private Xoroshiro128Plus rng;

	private bool hasStarted;

	[NonSerialized]
	public MultiShopController serverMultiShopController;

	public bool pickupIndexIsHidden => hidden;

	public PickupIndex NetworkpickupIndex
	{
		get
		{
			return pickupIndex;
		}
		[param: In]
		set
		{
			if (NetworkServer.localClientActive && !base.syncVarHookGuard)
			{
				base.syncVarHookGuard = true;
				OnSyncPickupIndex(value);
				base.syncVarHookGuard = false;
			}
			SetSyncVar(value, ref pickupIndex, 1u);
		}
	}

	public bool Networkhidden
	{
		get
		{
			return hidden;
		}
		[param: In]
		set
		{
			if (NetworkServer.localClientActive && !base.syncVarHookGuard)
			{
				base.syncVarHookGuard = true;
				OnSyncHidden(value);
				base.syncVarHookGuard = false;
			}
			SetSyncVar(value, ref hidden, 2u);
		}
	}

	public bool NetworkhasBeenPurchased
	{
		get
		{
			return hasBeenPurchased;
		}
		[param: In]
		set
		{
			if (NetworkServer.localClientActive && !base.syncVarHookGuard)
			{
				base.syncVarHookGuard = true;
				SetHasBeenPurchased(value);
				base.syncVarHookGuard = false;
			}
			SetSyncVar(value, ref hasBeenPurchased, 4u);
		}
	}

	public void SetHasBeenPurchased(bool newHasBeenPurchased)
	{
		if (hasBeenPurchased != newHasBeenPurchased)
		{
			NetworkhasBeenPurchased = newHasBeenPurchased;
		}
	}

	private void OnSyncHidden(bool newHidden)
	{
		SetPickupIndex(pickupIndex, newHidden);
	}

	private void OnSyncPickupIndex(PickupIndex newPickupIndex)
	{
		SetPickupIndex(newPickupIndex, hidden);
		if (NetworkClient.active)
		{
			UpdatePickupDisplayAndAnimations();
		}
	}

	public void Start()
	{
		hasStarted = true;
		if (NetworkServer.active)
		{
			rng = new Xoroshiro128Plus(Run.instance.treasureRng.nextUlong);
			if (selfGeneratePickup)
			{
				GenerateNewPickupServer();
			}
		}
		if (NetworkClient.active)
		{
			UpdatePickupDisplayAndAnimations();
		}
	}

	[Server]
	public void GenerateNewPickupServer()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.ShopTerminalBehavior::GenerateNewPickupServer()' called on client");
		}
		else
		{
			GenerateNewPickupServer(hidden);
		}
	}

	[Server]
	public void GenerateNewPickupServer(bool newHidden)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.ShopTerminalBehavior::GenerateNewPickupServer(System.Boolean)' called on client");
		}
		else if (!hasBeenPurchased)
		{
			PickupIndex none = PickupIndex.none;
			if ((bool)dropTable)
			{
				none = dropTable.GenerateDrop(rng);
			}
			else
			{
				List<PickupIndex> list2 = null;
				none = Pick(itemTier switch
				{
					ItemTier.Tier1 => Run.instance.availableTier1DropList, 
					ItemTier.Tier2 => Run.instance.availableTier2DropList, 
					ItemTier.Tier3 => Run.instance.availableTier3DropList, 
					ItemTier.Lunar => Run.instance.availableLunarCombinedDropList, 
					ItemTier.Boss => Run.instance.availableBossDropList, 
					ItemTier.VoidTier1 => Run.instance.availableVoidTier1DropList, 
					ItemTier.VoidTier2 => Run.instance.availableVoidTier2DropList, 
					ItemTier.VoidTier3 => Run.instance.availableVoidTier3DropList, 
					ItemTier.VoidBoss => Run.instance.availableVoidBossDropList, 
					_ => throw new ArgumentOutOfRangeException(), 
				});
			}
			SetPickupIndex(none, newHidden);
		}
		bool PassesFilter(PickupIndex pickupIndex)
		{
			if (bannedItemTag == ItemTag.Any)
			{
				return true;
			}
			PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
			if (pickupDef.itemIndex != ItemIndex.None)
			{
				return !ItemCatalog.GetItemDef(pickupDef.itemIndex).ContainsTag(bannedItemTag);
			}
			return true;
		}
		PickupIndex Pick(List<PickupIndex> list)
		{
			return rng.NextElementUniform(list.Where(PassesFilter).ToList());
		}
	}

	public void SetPickupIndex(PickupIndex newPickupIndex, bool newHidden = false)
	{
		if (pickupIndex != newPickupIndex || hidden != newHidden)
		{
			NetworkpickupIndex = newPickupIndex;
			Networkhidden = newHidden;
		}
	}

	public void SetHidden(bool newHidden)
	{
		SetPickupIndex(pickupIndex, newHidden);
	}

	private void UpdatePickupDisplayAndAnimations()
	{
		if ((bool)pickupDisplay)
		{
			pickupDisplay.SetPickupIndex(pickupIndex, hidden);
		}
		if (!hasStarted)
		{
			return;
		}
		if (pickupIndex == PickupIndex.none)
		{
			Util.PlaySound("Play_UI_tripleChestShutter", base.gameObject);
			if ((bool)animator)
			{
				int layerIndex = animator.GetLayerIndex("Body");
				animator.PlayInFixedTime(hasBeenPurchased ? "Open" : "Closing", layerIndex);
			}
		}
		else if ((bool)animator && !hasBeenPurchased)
		{
			int layerIndex2 = animator.GetLayerIndex("Body");
			animator.PlayInFixedTime("Idle", layerIndex2);
		}
	}

	public PickupIndex CurrentPickupIndex()
	{
		return pickupIndex;
	}

	[Server]
	public void SetNoPickup()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.ShopTerminalBehavior::SetNoPickup()' called on client");
		}
		else
		{
			SetPickupIndex(PickupIndex.none);
		}
	}

	[Server]
	public void DropPickup()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.ShopTerminalBehavior::DropPickup()' called on client");
			return;
		}
		SetHasBeenPurchased(newHasBeenPurchased: true);
		PickupDropletController.CreatePickupDroplet(pickupIndex, (dropTransform ? dropTransform : base.transform).position, base.transform.TransformVector(dropVelocity));
	}

	public bool CanBeInspected()
	{
		if ((!hasBeenPurchased || !disablesInspectionOnPurchase) && pickupIndex != PickupIndex.none)
		{
			return pickupIndex.isValid;
		}
		return false;
	}

	public InspectInfo GetInfo()
	{
		if (inspectShop && (bool)shopInspectDef)
		{
			return shopInspectDef;
		}
		if (pickupIndexIsHidden)
		{
			return MysteryItemInspectDef;
		}
		return PickupCatalog.GetPickupDef(CurrentPickupIndex()) ?? throw new InvalidOperationException("Attempted to get info for invalid pickup in shop terminal. Should be impossible. Please investigate.");
	}

	public bool GetInspectHintOverride(out string hintOverride)
	{
		if (inspectHintOverrideToken.Length > 0)
		{
			if (hintWantsItemFormatting)
			{
				string @string = Language.GetString(GetInfo().TitleToken);
				hintOverride = Language.GetStringFormatted(inspectHintOverrideToken, @string);
			}
			else
			{
				hintOverride = Language.GetString(inspectHintOverrideToken);
			}
			return true;
		}
		hintOverride = null;
		return false;
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			GeneratedNetworkCode._WritePickupIndex_None(writer, pickupIndex);
			writer.Write(hidden);
			writer.Write(hasBeenPurchased);
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
			GeneratedNetworkCode._WritePickupIndex_None(writer, pickupIndex);
		}
		if ((base.syncVarDirtyBits & 2u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(hidden);
		}
		if ((base.syncVarDirtyBits & 4u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(hasBeenPurchased);
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
			pickupIndex = GeneratedNetworkCode._ReadPickupIndex_None(reader);
			hidden = reader.ReadBoolean();
			hasBeenPurchased = reader.ReadBoolean();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			OnSyncPickupIndex(GeneratedNetworkCode._ReadPickupIndex_None(reader));
		}
		if (((uint)num & 2u) != 0)
		{
			OnSyncHidden(reader.ReadBoolean());
		}
		if (((uint)num & 4u) != 0)
		{
			SetHasBeenPurchased(reader.ReadBoolean());
		}
	}

	public override void PreStartClient()
	{
	}
}
