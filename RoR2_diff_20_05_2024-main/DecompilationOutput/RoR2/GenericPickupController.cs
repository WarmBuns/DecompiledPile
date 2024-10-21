using System;
using System.Runtime.InteropServices;
using RoR2.Items;
using RoR2.Networking;
using RoR2.UI;
using Unity;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public sealed class GenericPickupController : NetworkBehaviour, IInteractable, IInspectable, IInspectInfoProvider, IDisplayNameProvider
{
	private class PickupMessage : MessageBase
	{
		public GameObject masterGameObject;

		public PickupIndex pickupIndex;

		public uint pickupQuantity;

		public void Reset()
		{
			masterGameObject = null;
			pickupIndex = PickupIndex.none;
			pickupQuantity = 0u;
		}

		public override void Serialize(NetworkWriter writer)
		{
			writer.Write(masterGameObject);
			GeneratedNetworkCode._WritePickupIndex_None(writer, pickupIndex);
			writer.WritePackedUInt32(pickupQuantity);
		}

		public override void Deserialize(NetworkReader reader)
		{
			masterGameObject = reader.ReadGameObject();
			pickupIndex = GeneratedNetworkCode._ReadPickupIndex_None(reader);
			pickupQuantity = reader.ReadPackedUInt32();
		}
	}

	public enum PickupArtifactFlag
	{
		NONE,
		COMMAND,
		DELUSION
	}

	public struct CreatePickupInfo
	{
		public Vector3 position;

		public Quaternion rotation;

		private PickupIndex? _pickupIndex;

		public PickupPickerController.Option[] pickerOptions;

		public GameObject prefabOverride;

		public ChestBehavior chest;

		public PickupArtifactFlag artifactFlag;

		public ItemIndex delusionItemIndex;

		public ItemIndex falseChoice1;

		public ItemIndex falseChoice2;

		public PickupIndex pickupIndex
		{
			get
			{
				if (!_pickupIndex.HasValue)
				{
					return PickupIndex.none;
				}
				return _pickupIndex.Value;
			}
			set
			{
				_pickupIndex = value;
			}
		}
	}

	public PickupDisplay pickupDisplay;

	public ChestBehavior chestGeneratedFrom;

	[SyncVar(hook = "SyncPickupIndex")]
	public PickupIndex pickupIndex = PickupIndex.none;

	[SyncVar(hook = "SyncRecycled")]
	public bool Recycled;

	public bool selfDestructIfPickupIndexIsNotIdeal;

	public SerializablePickupIndex idealPickupIndex;

	private static readonly PickupMessage pickupMessageInstance = new PickupMessage();

	public float waitDuration = 0.5f;

	private Run.FixedTimeStamp waitStartTime;

	private bool consumed;

	public const string pickupSoundString = "Play_UI_item_pickup";

	private static GameObject pickupPrefab;

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
				SyncPickupIndex(value);
				base.syncVarHookGuard = false;
			}
			SetSyncVar(value, ref pickupIndex, 1u);
		}
	}

	public bool NetworkRecycled
	{
		get
		{
			return Recycled;
		}
		[param: In]
		set
		{
			if (NetworkServer.localClientActive && !base.syncVarHookGuard)
			{
				base.syncVarHookGuard = true;
				SyncRecycled(value);
				base.syncVarHookGuard = false;
			}
			SetSyncVar(value, ref Recycled, 2u);
		}
	}

	private void SyncPickupIndex(PickupIndex newPickupIndex)
	{
		NetworkpickupIndex = newPickupIndex;
		UpdatePickupDisplay();
	}

	private void SyncRecycled(bool isRecycled)
	{
		NetworkRecycled = isRecycled;
	}

	[Server]
	public static void SendPickupMessage(CharacterMaster master, PickupIndex pickupIndex)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.GenericPickupController::SendPickupMessage(RoR2.CharacterMaster,RoR2.PickupIndex)' called on client");
			return;
		}
		uint pickupQuantity = 1u;
		if ((bool)master.inventory)
		{
			ItemIndex itemIndex = PickupCatalog.GetPickupDef(pickupIndex)?.itemIndex ?? ItemIndex.None;
			if (itemIndex != ItemIndex.None)
			{
				pickupQuantity = (uint)master.inventory.GetItemCount(itemIndex);
			}
		}
		try
		{
			PickupMessage msg = new PickupMessage
			{
				masterGameObject = master.gameObject,
				pickupIndex = pickupIndex,
				pickupQuantity = pickupQuantity
			};
			NetworkServer.SendByChannelToAll(57, msg, QosChannelIndex.chat.intVal);
		}
		catch (Exception arg)
		{
			Debug.Log($"Failed to send pickupMessage for pickupIndex {pickupIndex} - gameObject {master.gameObject.name}\r\n{arg}");
		}
	}

	[NetworkMessageHandler(msgType = 57, client = true)]
	private static void HandlePickupMessage(NetworkMessage netMsg)
	{
		PickupMessage pickupMessage = pickupMessageInstance;
		netMsg.ReadMessage(pickupMessage);
		GameObject masterGameObject = pickupMessage.masterGameObject;
		PickupIndex pickupIndex = pickupMessage.pickupIndex;
		PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
		uint pickupQuantity = pickupMessage.pickupQuantity;
		pickupMessage.Reset();
		if (!masterGameObject)
		{
			return;
		}
		CharacterMaster component = masterGameObject.GetComponent<CharacterMaster>();
		if (!component)
		{
			return;
		}
		PlayerCharacterMasterController component2 = component.GetComponent<PlayerCharacterMasterController>();
		if ((bool)component2)
		{
			NetworkUser networkUser = component2.networkUser;
			if ((bool)networkUser)
			{
				networkUser.localUser?.userProfile.DiscoverPickup(pickupIndex);
			}
		}
		CharacterBody body = component.GetBody();
		_ = (bool)body;
		ItemDef itemDef = ItemCatalog.GetItemDef(pickupDef?.itemIndex ?? ItemIndex.None);
		if (itemDef != null && itemDef.hidden)
		{
			return;
		}
		if (pickupIndex != PickupIndex.none)
		{
			ItemIndex transformedItemIndex = ContagiousItemManager.GetTransformedItemIndex(itemDef?.itemIndex ?? ItemIndex.None);
			if (itemDef == null || transformedItemIndex == ItemIndex.None || component.inventory.GetItemCount(transformedItemIndex) <= 0)
			{
				CharacterMasterNotificationQueue.PushPickupNotification(component, pickupIndex);
			}
		}
		Chat.AddPickupMessage(body, pickupDef?.nameToken ?? PickupCatalog.invalidPickupToken, pickupDef?.baseColor ?? Color.black, pickupQuantity);
		if ((bool)body)
		{
			Util.PlaySound("Play_UI_item_pickup", body.gameObject);
		}
	}

	public void StartWaitTime()
	{
		waitStartTime = Run.FixedTimeStamp.now;
	}

	private void OnTriggerStay(Collider other)
	{
		if (!NetworkServer.active || !(waitStartTime.timeSince >= waitDuration) || consumed)
		{
			return;
		}
		CharacterBody component = other.GetComponent<CharacterBody>();
		if (!component)
		{
			return;
		}
		PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
		ItemIndex itemIndex = pickupDef?.itemIndex ?? ItemIndex.None;
		if (itemIndex != ItemIndex.None)
		{
			ItemTierDef itemTierDef = ItemTierCatalog.GetItemTierDef(ItemCatalog.GetItemDef(itemIndex).tier);
			if ((bool)itemTierDef && (itemTierDef.pickupRules == ItemTierDef.PickupRules.ConfirmAll || (itemTierDef.pickupRules == ItemTierDef.PickupRules.ConfirmFirst && (bool)component.inventory && component.inventory.GetItemCount(itemIndex) <= 0)))
			{
				return;
			}
		}
		EquipmentIndex equipmentIndex = pickupDef?.equipmentIndex ?? EquipmentIndex.None;
		if ((equipmentIndex == EquipmentIndex.None || (!EquipmentCatalog.GetEquipmentDef(equipmentIndex).isLunar && (!component.inventory || component.inventory.currentEquipmentIndex == EquipmentIndex.None))) && (pickupDef == null || pickupDef.coinValue == 0) && BodyHasPickupPermission(component))
		{
			AttemptGrant(component);
		}
	}

	private static bool BodyHasPickupPermission(CharacterBody body)
	{
		if ((bool)(body.masterObject ? body.masterObject.GetComponent<PlayerCharacterMasterController>() : null))
		{
			return body.inventory;
		}
		return false;
	}

	public bool ShouldIgnoreSpherecastForInteractibility(Interactor activator)
	{
		return false;
	}

	public string GetContextString(Interactor activator)
	{
		return string.Format(Language.GetString(PickupCatalog.GetPickupDef(pickupIndex)?.interactContextToken ?? string.Empty), GetDisplayName());
	}

	private void UpdatePickupDisplay()
	{
		if (!pickupDisplay)
		{
			return;
		}
		pickupDisplay.SetPickupIndex(pickupIndex);
		if ((bool)pickupDisplay.modelRenderer)
		{
			Highlight component = GetComponent<Highlight>();
			if ((bool)component)
			{
				component.targetRenderer = pickupDisplay.modelRenderer;
				component.ResetHighlight();
			}
		}
	}

	[Server]
	private void AttemptGrant(CharacterBody body)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.GenericPickupController::AttemptGrant(RoR2.CharacterBody)' called on client");
			return;
		}
		TeamComponent component = body.GetComponent<TeamComponent>();
		if (!component || component.teamIndex != TeamIndex.Player)
		{
			return;
		}
		PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
		if ((bool)body.inventory && pickupDef != null)
		{
			PickupDef.GrantContext grantContext = default(PickupDef.GrantContext);
			grantContext.body = body;
			grantContext.controller = this;
			PickupDef.GrantContext context = grantContext;
			pickupDef.attemptGrant?.Invoke(ref context);
			consumed = context.shouldDestroy;
			if (context.shouldNotify)
			{
				SendPickupMessage(body.master, pickupDef.pickupIndex);
			}
			if ((bool)chestGeneratedFrom && DelusionChestController.isDelusionEnable)
			{
				chestGeneratedFrom.CallRpcSetDelusionPickupIndex(pickupIndex);
			}
			if (context.shouldDestroy)
			{
				UnityEngine.Object.Destroy(base.gameObject);
			}
		}
	}

	private void Start()
	{
		waitStartTime = Run.FixedTimeStamp.now;
		consumed = false;
		UpdatePickupDisplay();
	}

	private void OnEnable()
	{
		InstanceTracker.Add(this);
	}

	private void OnDisable()
	{
		InstanceTracker.Remove(this);
	}

	public Interactability GetInteractability(Interactor activator)
	{
		if (!base.enabled)
		{
			return Interactability.Disabled;
		}
		if (waitStartTime.timeSince < waitDuration || consumed)
		{
			return Interactability.Disabled;
		}
		CharacterBody component = activator.GetComponent<CharacterBody>();
		if ((bool)component)
		{
			if (!BodyHasPickupPermission(component))
			{
				return Interactability.Disabled;
			}
			return Interactability.Available;
		}
		return Interactability.Disabled;
	}

	public void OnInteractionBegin(Interactor activator)
	{
		AttemptGrant(activator.GetComponent<CharacterBody>());
	}

	public bool ShouldShowOnScanner()
	{
		return true;
	}

	public bool ShouldProximityHighlight()
	{
		return true;
	}

	public string GetDisplayName()
	{
		return Language.GetString(PickupCatalog.GetPickupDef(pickupIndex)?.nameToken ?? PickupCatalog.invalidPickupToken);
	}

	public void SetPickupIndexFromString(string pickupString)
	{
		if (NetworkServer.active)
		{
			PickupIndex networkpickupIndex = PickupCatalog.FindPickupIndex(pickupString);
			NetworkpickupIndex = networkpickupIndex;
		}
	}

	[InitDuringStartup]
	private static void Init()
	{
		LegacyResourcesAPI.LoadAsyncCallback("Prefabs/NetworkedObjects/GenericPickup", delegate(GameObject operationResult)
		{
			pickupPrefab = operationResult;
		});
	}

	public static GenericPickupController CreatePickup(in CreatePickupInfo createPickupInfo)
	{
		GameObject gameObject = UnityEngine.Object.Instantiate(createPickupInfo.prefabOverride ?? pickupPrefab, createPickupInfo.position, createPickupInfo.rotation);
		GenericPickupController component = gameObject.GetComponent<GenericPickupController>();
		if ((bool)component)
		{
			component.NetworkpickupIndex = createPickupInfo.pickupIndex;
			component.chestGeneratedFrom = createPickupInfo.chest;
		}
		else
		{
			PickupDisplay componentInChildren = gameObject.GetComponentInChildren<PickupDisplay>();
			if ((bool)componentInChildren)
			{
				GameObject modelObjectOverride = gameObject.GetComponentInChildren<MeshRenderer>().gameObject;
				componentInChildren.RebuildModel(modelObjectOverride);
			}
		}
		PickupIndexNetworker component2 = gameObject.GetComponent<PickupIndexNetworker>();
		if ((bool)component2)
		{
			component2.NetworkpickupIndex = createPickupInfo.pickupIndex;
		}
		PickupPickerController component3 = gameObject.GetComponent<PickupPickerController>();
		if ((bool)component3 && createPickupInfo.pickerOptions != null)
		{
			component3.SetOptionsServer(createPickupInfo.pickerOptions);
		}
		NetworkServer.Spawn(gameObject);
		return component;
	}

	[ContextMenu("Print Pickup Index")]
	private void PrintPickupIndex()
	{
		Debug.LogFormat("pickupIndex={0}", PickupCatalog.GetPickupDef(pickupIndex)?.internalName ?? "Invalid");
	}

	public IInspectInfoProvider GetInspectInfoProvider()
	{
		return this;
	}

	public bool CanBeInspected()
	{
		if (pickupIndex != PickupIndex.none)
		{
			return pickupIndex.isValid;
		}
		return false;
	}

	public InspectInfo GetInfo()
	{
		return PickupCatalog.GetPickupDef(pickupIndex) ?? throw new InvalidOperationException("Attempted to get info for invalid pickup. Should be impossible. Investigate me.");
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			GeneratedNetworkCode._WritePickupIndex_None(writer, pickupIndex);
			writer.Write(Recycled);
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
			writer.Write(Recycled);
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
			Recycled = reader.ReadBoolean();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			SyncPickupIndex(GeneratedNetworkCode._ReadPickupIndex_None(reader));
		}
		if (((uint)num & 2u) != 0)
		{
			SyncRecycled(reader.ReadBoolean());
		}
	}

	public override void PreStartClient()
	{
	}
}
