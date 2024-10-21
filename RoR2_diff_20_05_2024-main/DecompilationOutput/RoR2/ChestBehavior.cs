using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using EntityStates;
using EntityStates.Barrel;
using RoR2.Networking;
using Unity;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace RoR2;

public class ChestBehavior : NetworkBehaviour, IChestBehavior
{
	public PickupDropTable dropTable;

	public Transform dropTransform;

	public float dropUpVelocityStrength = 20f;

	public float dropForwardVelocityStrength = 2f;

	public int minDropCount = 1;

	public int maxDropCount = 1;

	public SerializableEntityStateType openState = new SerializableEntityStateType(typeof(Opening));

	public SerializableEntityStateType closingState = new SerializableEntityStateType(typeof(Closing));

	[Header("Deprecated")]
	[Tooltip("Deprecated.  Use DropTable instead.")]
	public float tier1Chance = 0.8f;

	[Tooltip("Deprecated.  Use DropTable instead.")]
	public float tier2Chance = 0.2f;

	[Tooltip("Deprecated.  Use DropTable instead.")]
	public float tier3Chance = 0.01f;

	[Tooltip("Deprecated.  Use DropTable instead.")]
	public float lunarChance;

	[Tooltip("Deprecated.  Use DropTable instead.")]
	public ItemTag requiredItemTag;

	[Tooltip("Deprecated.  Use DropTable instead.")]
	public float lunarCoinChance;

	[Tooltip("Deprecated.  Use DropTable instead.")]
	public UnityEvent dropRoller;

	private Xoroshiro128Plus rng;

	public int dropCount;

	[SyncVar]
	public bool isCommandChest;

	[SyncVar]
	private bool isChestOpened;

	[HideInInspector]
	public PickupPickerController ppc;

	private static int kRpcRpcResetChests;

	private static int kRpcRpcSetDelusionPickupIndex;

	public PickupIndex dropPickup { get; private set; } = PickupIndex.none;

	public bool NetworkisCommandChest
	{
		get
		{
			return isCommandChest;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref isCommandChest, 1u);
		}
	}

	public bool NetworkisChestOpened
	{
		get
		{
			return isChestOpened;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref isChestOpened, 2u);
		}
	}

	public override int GetNetworkChannel()
	{
		return QosChannelIndex.defaultReliable.intVal;
	}

	[Server]
	public void Roll()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.ChestBehavior::Roll()' called on client");
		}
		else if ((bool)dropTable)
		{
			dropPickup = dropTable.GenerateDrop(rng);
		}
		else
		{
			dropRoller?.Invoke();
		}
	}

	public PickupIndex GetRandomPickupIndex()
	{
		return dropTable.GenerateDrop(rng);
	}

	[Server]
	private void PickFromList(List<PickupIndex> dropList)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.ChestBehavior::PickFromList(System.Collections.Generic.List`1<RoR2.PickupIndex>)' called on client");
			return;
		}
		dropPickup = PickupIndex.none;
		if (dropList != null && dropList.Count > 0)
		{
			dropPickup = rng.NextElementUniform(dropList);
		}
	}

	[Obsolete("Just use a drop table")]
	[Server]
	public void RollItem()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.ChestBehavior::RollItem()' called on client");
			return;
		}
		WeightedSelection<List<PickupIndex>> selector = new WeightedSelection<List<PickupIndex>>();
		List<PickupIndex> sourceDropList2 = new List<PickupIndex> { PickupCatalog.FindPickupIndex(RoR2Content.MiscPickups.LunarCoin.miscPickupIndex) };
		Add(Run.instance.availableTier1DropList, tier1Chance);
		Add(Run.instance.availableTier2DropList, tier2Chance);
		Add(Run.instance.availableTier3DropList, tier3Chance);
		Add(Run.instance.availableLunarCombinedDropList, lunarChance);
		Add(sourceDropList2, lunarCoinChance);
		List<PickupIndex> dropList = selector.Evaluate(rng.nextNormalizedFloat);
		PickFromList(dropList);
		void Add(List<PickupIndex> sourceDropList, float chance)
		{
			if (!(chance <= 0f))
			{
				selector.AddChoice(sourceDropList.Where(PassesFilter).ToList(), chance);
			}
		}
		bool PassesFilter(PickupIndex pickupIndex)
		{
			if (requiredItemTag == ItemTag.Any)
			{
				return true;
			}
			PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
			if (pickupDef.itemIndex != ItemIndex.None)
			{
				return ItemCatalog.GetItemDef(pickupDef.itemIndex).ContainsTag(requiredItemTag);
			}
			return true;
		}
	}

	[Obsolete("Just use a drop table")]
	[Server]
	public void RollEquipment()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.ChestBehavior::RollEquipment()' called on client");
		}
		else
		{
			PickFromList(Run.instance.availableEquipmentDropList);
		}
	}

	private void Awake()
	{
		if (dropTransform == null)
		{
			dropTransform = base.transform;
		}
	}

	private void Start()
	{
		if (NetworkServer.active)
		{
			rng = new Xoroshiro128Plus(Run.instance.treasureRng.nextUlong);
			dropCount = rng.RangeInt(minDropCount, Math.Max(minDropCount + 1, maxDropCount + 1));
			Roll();
		}
	}

	public void OnEnable()
	{
		InstanceTracker.Add(this);
	}

	public void OnDisable()
	{
		InstanceTracker.Remove(this);
	}

	[Server]
	public void Open()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.ChestBehavior::Open()' called on client");
			return;
		}
		EntityStateMachine component = GetComponent<EntityStateMachine>();
		if ((bool)component)
		{
			component.SetNextState(EntityStateCatalog.InstantiateState(ref openState));
		}
	}

	[ClientRpc]
	public void RpcResetChests()
	{
		if (TryGetComponent<DelusionChestController>(out var component) && isChestOpened)
		{
			component.ResetChestForDelusion();
		}
	}

	[ClientRpc]
	public void RpcSetDelusionPickupIndex(PickupIndex pickupIndex)
	{
		if (TryGetComponent<DelusionChestController>(out var component))
		{
			component.DelusionPickupIndex = pickupIndex;
		}
	}

	[Server]
	public void ItemDrop()
	{
		DelusionChestController component;
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.ChestBehavior::ItemDrop()' called on client");
		}
		else if (DelusionChestController.isDelusionEnable && TryGetComponent<DelusionChestController>(out component) && component.SelectedPickupIndex != PickupIndex.none)
		{
			if (component.IsSelectedCorrectly())
			{
				Util.PlaySound("Play_ui_artifact_delusion_success", base.gameObject);
				DelusionItemDrop();
			}
			else
			{
				Util.PlaySound("Play_ui_artifact_delusion_failure", base.gameObject);
				component.RemoveDelusionChoice();
			}
		}
		else
		{
			BaseItemDrop();
		}
	}

	private void DelusionItemDrop(DelusionChestController delusionChestController)
	{
		if (delusionChestController.isDelusionSelected)
		{
			if (delusionChestController.IsSelectedCorrectly())
			{
				Vector3 velocity = Vector3.up * dropUpVelocityStrength + dropTransform.forward * dropForwardVelocityStrength;
				GenericPickupController.CreatePickupInfo createPickupInfo = default(GenericPickupController.CreatePickupInfo);
				createPickupInfo.pickupIndex = delusionChestController.DelusionPickupIndex;
				createPickupInfo.position = dropTransform.position + Vector3.up * 1.5f;
				createPickupInfo.chest = this;
				createPickupInfo.artifactFlag = GenericPickupController.PickupArtifactFlag.DELUSION;
				GenericPickupController.CreatePickupInfo pickupInfo = createPickupInfo;
				PickupDropletController.CreatePickupDroplet(pickupInfo, pickupInfo.position, velocity);
				Util.PlaySound("Play_ui_artifact_delusion_success", base.gameObject);
			}
			else
			{
				Util.PlaySound("Play_ui_artifact_delusion_failure", base.gameObject);
				delusionChestController.RemoveDelusionChoice();
			}
		}
	}

	private void DelusionItemDrop()
	{
		if (TryGetComponent<DelusionChestController>(out var component) && component.SelectedPickupIndex == component.DelusionPickupIndex)
		{
			Vector3 velocity = Vector3.up * dropUpVelocityStrength + dropTransform.forward * dropForwardVelocityStrength;
			PickupDropletController.CreatePickupDroplet(component.DelusionPickupIndex, dropTransform.position + Vector3.up * 1.5f, velocity);
		}
	}

	private void BaseItemDrop()
	{
		if (!(dropPickup == PickupIndex.none) && dropCount >= 1)
		{
			float angle = 360f / (float)dropCount;
			Vector3 vector = Vector3.up * dropUpVelocityStrength + dropTransform.forward * dropForwardVelocityStrength;
			Quaternion quaternion = Quaternion.AngleAxis(angle, Vector3.up);
			GetComponent<DelusionChestController>();
			for (int i = 0; i < dropCount; i++)
			{
				GenericPickupController.CreatePickupInfo createPickupInfo = default(GenericPickupController.CreatePickupInfo);
				createPickupInfo.pickupIndex = dropPickup;
				createPickupInfo.position = dropTransform.position + Vector3.up * 1.5f;
				createPickupInfo.chest = this;
				createPickupInfo.artifactFlag = (isCommandChest ? GenericPickupController.PickupArtifactFlag.COMMAND : GenericPickupController.PickupArtifactFlag.NONE);
				GenericPickupController.CreatePickupInfo pickupInfo = createPickupInfo;
				PickupDropletController.CreatePickupDroplet(pickupInfo, pickupInfo.position, vector);
				vector = quaternion * vector;
				Roll();
			}
			dropPickup = PickupIndex.none;
			NetworkisChestOpened = true;
		}
	}

	public bool HasRolledPickup(PickupIndex pickupIndex)
	{
		return dropPickup == pickupIndex;
	}

	private void UNetVersion()
	{
	}

	protected static void InvokeRpcRpcResetChests(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcResetChests called on server.");
		}
		else
		{
			((ChestBehavior)obj).RpcResetChests();
		}
	}

	protected static void InvokeRpcRpcSetDelusionPickupIndex(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcSetDelusionPickupIndex called on server.");
		}
		else
		{
			((ChestBehavior)obj).RpcSetDelusionPickupIndex(GeneratedNetworkCode._ReadPickupIndex_None(reader));
		}
	}

	public void CallRpcResetChests()
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcResetChests called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcResetChests);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		SendRPCInternal(networkWriter, 0, "RpcResetChests");
	}

	public void CallRpcSetDelusionPickupIndex(PickupIndex pickupIndex)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcSetDelusionPickupIndex called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcSetDelusionPickupIndex);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		GeneratedNetworkCode._WritePickupIndex_None(networkWriter, pickupIndex);
		SendRPCInternal(networkWriter, 0, "RpcSetDelusionPickupIndex");
	}

	static ChestBehavior()
	{
		kRpcRpcResetChests = -115962472;
		NetworkBehaviour.RegisterRpcDelegate(typeof(ChestBehavior), kRpcRpcResetChests, InvokeRpcRpcResetChests);
		kRpcRpcSetDelusionPickupIndex = -2130336578;
		NetworkBehaviour.RegisterRpcDelegate(typeof(ChestBehavior), kRpcRpcSetDelusionPickupIndex, InvokeRpcRpcSetDelusionPickupIndex);
		NetworkCRC.RegisterBehaviour("ChestBehavior", 0);
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.Write(isCommandChest);
			writer.Write(isChestOpened);
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
			writer.Write(isCommandChest);
		}
		if ((base.syncVarDirtyBits & 2u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(isChestOpened);
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
			isCommandChest = reader.ReadBoolean();
			isChestOpened = reader.ReadBoolean();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			isCommandChest = reader.ReadBoolean();
		}
		if (((uint)num & 2u) != 0)
		{
			isChestOpened = reader.ReadBoolean();
		}
	}

	public override void PreStartClient()
	{
	}
}
