using System;
using System.Collections.Generic;
using System.Linq;
using RoR2.CharacterAI;
using RoR2.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(NetworkUIPromptController))]
public class PickupPickerController : NetworkBehaviour, IInteractable, IInspectable
{
	public struct Option
	{
		public PickupIndex pickupIndex;

		public bool available;
	}

	[Serializable]
	public class PickupIndexUnityEvent : UnityEvent<int>
	{
	}

	public GameObject panelPrefab;

	public PickupIndexUnityEvent onPickupSelected;

	public GenericInteraction.InteractorUnityEvent onServerInteractionBegin;

	public float cutoffDistance;

	private bool available = true;

	public string contextString = "";

	public bool shouldProximityHighlight = true;

	public bool synchronizeItemSelectionAcrossNetwork;

	public bool isDelusionChoice;

	public bool isRebirthChoice;

	private Inventory delusionInventory;

	private bool delusionOptionsSet;

	private int delusionChoicePickupIndex;

	private NetworkUIPromptController networkUIPromptController;

	private const byte msgSubmit = 0;

	private const byte msgCancel = 1;

	private GameObject panelInstance;

	private PickupPickerPanel panelInstanceController;

	public ChestBehavior chestGeneratedFrom;

	private Option[] options = Array.Empty<Option>();

	private static readonly uint optionsDirtyBit;

	private static readonly uint availableDirtyBit;

	private static readonly uint allDirtyBits;

	private int numberOfPickUpsSelected;

	public bool multiOptionsAvailable;

	private static int GrabNumbers;

	private EventFunctions eventFunctions;

	private static int kRpcRpcHandlePickupSelected;

	private void Awake()
	{
		networkUIPromptController = GetComponent<NetworkUIPromptController>();
		if (NetworkClient.active)
		{
			networkUIPromptController.onDisplayBegin += OnDisplayBegin;
			networkUIPromptController.onDisplayEnd += OnDisplayEnd;
		}
		if (NetworkServer.active)
		{
			networkUIPromptController.messageFromClientHandler = HandleClientMessage;
		}
		eventFunctions = base.gameObject.GetComponent<EventFunctions>();
		if (GrabNumbers > 1)
		{
			multiOptionsAvailable = true;
		}
	}

	private void OnEnable()
	{
		InstanceTracker.Add(this);
	}

	private void OnDisable()
	{
		InstanceTracker.Remove(this);
	}

	private void HandleClientMessage(NetworkReader reader)
	{
		switch (reader.ReadByte())
		{
		case 0:
		{
			int choiceIndex = reader.ReadInt32();
			HandlePickupSelected(choiceIndex);
			break;
		}
		case 1:
			networkUIPromptController.SetParticipantMaster(null);
			break;
		}
	}

	private void FixedUpdate()
	{
		if (NetworkServer.active)
		{
			FixedUpdateServer();
		}
	}

	private void FixedUpdateServer()
	{
		CharacterMaster currentParticipantMaster = networkUIPromptController.currentParticipantMaster;
		if ((bool)currentParticipantMaster)
		{
			CharacterBody body = currentParticipantMaster.GetBody();
			if (!body || (body.inputBank.aimOrigin - base.transform.position).sqrMagnitude > cutoffDistance * cutoffDistance)
			{
				networkUIPromptController.SetParticipantMaster(null);
			}
		}
	}

	private void OnPanelDestroyed(OnDestroyCallback onDestroyCallback)
	{
		NetworkWriter networkWriter = networkUIPromptController.BeginMessageToServer();
		networkWriter.Write((byte)1);
		networkUIPromptController.FinishMessageToServer(networkWriter);
	}

	private void OnDisplayBegin(NetworkUIPromptController networkUIPromptController, LocalUser localUser, CameraRigController cameraRigController)
	{
		panelInstance = UnityEngine.Object.Instantiate(panelPrefab, cameraRigController.hud.mainContainer.transform);
		panelInstanceController = panelInstance.GetComponent<PickupPickerPanel>();
		panelInstanceController.pickerController = this;
		panelInstanceController.SetPickupOptions(options);
		OnDestroyCallback.AddCallback(panelInstance, OnPanelDestroyed);
	}

	private void OnDisplayEnd(NetworkUIPromptController networkUIPromptController, LocalUser localUser, CameraRigController cameraRigController)
	{
		UnityEngine.Object.Destroy(panelInstance);
		panelInstance = null;
		panelInstanceController = null;
	}

	[Server]
	public void SetAvailable(bool newAvailable)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.PickupPickerController::SetAvailable(System.Boolean)' called on client");
			return;
		}
		available = newAvailable;
		SetDirtyBit(availableDirtyBit);
	}

	public void SubmitChoice(int choiceIndex)
	{
		if (!NetworkServer.active)
		{
			NetworkWriter networkWriter = networkUIPromptController.BeginMessageToServer();
			networkWriter.Write((byte)0);
			networkWriter.Write(choiceIndex);
			networkUIPromptController.FinishMessageToServer(networkWriter);
		}
		else
		{
			HandlePickupSelected(choiceIndex);
		}
	}

	public bool IsChoiceAvailable(PickupIndex choice)
	{
		for (int i = 0; i < options.Length; i++)
		{
			ref Option reference = ref options[i];
			if (reference.pickupIndex == choice)
			{
				return reference.available;
			}
		}
		return false;
	}

	[Server]
	private void HandlePickupSelected(int choiceIndex)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.PickupPickerController::HandlePickupSelected(System.Int32)' called on client");
		}
		else
		{
			if ((uint)choiceIndex >= options.Length)
			{
				return;
			}
			ref Option reference = ref options[choiceIndex];
			if (reference.available)
			{
				onPickupSelected?.Invoke(reference.pickupIndex.value);
				if (synchronizeItemSelectionAcrossNetwork)
				{
					CallRpcHandlePickupSelected(choiceIndex);
				}
			}
		}
	}

	[ClientRpc]
	private void RpcHandlePickupSelected(int choiceIndex)
	{
		if ((uint)choiceIndex < options.Length)
		{
			ref Option reference = ref options[choiceIndex];
			if (reference.available)
			{
				reference.available = false;
			}
		}
	}

	[Server]
	public void SetOptionsServer(Option[] newOptions)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.PickupPickerController::SetOptionsServer(RoR2.PickupPickerController/Option[])' called on client");
		}
		else
		{
			SetOptionsInternal(newOptions);
		}
	}

	private void SetOptionsInternal(Option[] newOptions)
	{
		Array.Resize(ref options, newOptions.Length);
		Array.Copy(newOptions, options, newOptions.Length);
		if ((bool)panelInstanceController)
		{
			panelInstanceController.SetPickupOptions(options);
		}
		if (NetworkServer.active)
		{
			SetDirtyBit(optionsDirtyBit);
		}
	}

	[Server]
	public void SetTestOptions()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.PickupPickerController::SetTestOptions()' called on client");
			return;
		}
		SetOptionsServer((from itemIndex in ItemCatalog.allItems
			select ItemCatalog.GetItemDef(itemIndex) into itemDef
			where itemDef.tier == ItemTier.Tier2
			select itemDef).Select(delegate(ItemDef itemDef)
		{
			Option result = default(Option);
			result.pickupIndex = PickupCatalog.FindPickupIndex(itemDef.itemIndex);
			result.available = true;
			return result;
		}).ToArray());
	}

	public static Option[] GenerateOptionsFromDropTable(int numOptions, PickupDropTable dropTable, Xoroshiro128Plus rng)
	{
		PickupIndex[] array = dropTable.GenerateUniqueDrops(numOptions, rng);
		GrabNumbers = numOptions - 2;
		Option[] array2 = new Option[array.Length];
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i] = new Option
			{
				available = true,
				pickupIndex = array[i]
			};
		}
		return array2;
	}

	public static Option[] GenerateOptionsFromDropTablePlusForcedStorm(int numOptions, PickupDropTable dropTable, PickupDropTable stormDropTable, Xoroshiro128Plus rng)
	{
		PickupIndex[] array = stormDropTable.GenerateUniqueDrops(numOptions, rng);
		PickupIndex[] array2 = dropTable.GenerateUniqueDrops(numOptions, rng);
		int num = 0;
		int num2 = 0;
		Option[] array3 = new Option[numOptions];
		for (int i = 0; i < array3.Length; i++)
		{
			if (num <= num2 - 2)
			{
				array3[i] = new Option
				{
					available = true,
					pickupIndex = array[i]
				};
				num++;
			}
			else
			{
				array3[i] = new Option
				{
					available = true,
					pickupIndex = array2[i]
				};
				num2++;
			}
		}
		Shuffle(new System.Random(), array3);
		GrabNumbers = numOptions - 2;
		return array3;
	}

	private static void Shuffle<T>(System.Random rng, T[] array)
	{
		int num = array.Length;
		while (num > 1)
		{
			int num2 = rng.Next(num--);
			T val = array[num];
			array[num] = array[num2];
			array[num2] = val;
		}
	}

	private static Option[] GetOptionsFromPickupIndex(PickupIndex pickupIndex)
	{
		PickupIndex[] groupFromPickupIndex = PickupTransmutationManager.GetGroupFromPickupIndex(pickupIndex);
		if (groupFromPickupIndex == null)
		{
			return new Option[1]
			{
				new Option
				{
					available = true,
					pickupIndex = pickupIndex
				}
			};
		}
		Option[] array = new Option[groupFromPickupIndex.Length];
		for (int i = 0; i < groupFromPickupIndex.Length; i++)
		{
			PickupIndex pickupIndex2 = groupFromPickupIndex[i];
			array[i] = new Option
			{
				available = Run.instance.IsPickupAvailable(pickupIndex2),
				pickupIndex = pickupIndex2
			};
		}
		return array;
	}

	public static Option[] GenerateOptionsFromArray(PickupIndex[] drops)
	{
		Option[] array = new Option[drops.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = new Option
			{
				available = true,
				pickupIndex = drops[i]
			};
		}
		return array;
	}

	[Server]
	public void SetOptionsFromPickupForCommandArtifact(PickupIndex pickupIndex)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.PickupPickerController::SetOptionsFromPickupForCommandArtifact(RoR2.PickupIndex)' called on client");
		}
		else
		{
			SetOptionsServer(GetOptionsFromPickupIndex(pickupIndex));
		}
	}

	[Server]
	public void CreatePickup(int intPickupIndex)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.PickupPickerController::CreatePickup(System.Int32)' called on client");
		}
		else
		{
			CreatePickup(new PickupIndex(intPickupIndex));
		}
	}

	[Server]
	public void CreatePickup(PickupIndex pickupIndex)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.PickupPickerController::CreatePickup(RoR2.PickupIndex)' called on client");
			return;
		}
		GenericPickupController.CreatePickupInfo createPickupInfo = default(GenericPickupController.CreatePickupInfo);
		createPickupInfo.rotation = Quaternion.identity;
		createPickupInfo.position = base.transform.position;
		createPickupInfo.pickupIndex = pickupIndex;
		createPickupInfo.chest = chestGeneratedFrom;
		GenericPickupController.CreatePickupInfo createPickupInfo2 = createPickupInfo;
		GenericPickupController.CreatePickup(in createPickupInfo2);
	}

	public override bool OnSerialize(NetworkWriter writer, bool initialState)
	{
		uint num = base.syncVarDirtyBits;
		if (initialState)
		{
			num = allDirtyBits;
		}
		bool num2 = (num & optionsDirtyBit) != 0;
		writer.WritePackedUInt32(num);
		if (num2)
		{
			writer.WritePackedUInt32((uint)options.Length);
			for (int i = 0; i < options.Length; i++)
			{
				ref Option reference = ref options[i];
				writer.Write(reference.pickupIndex);
				writer.Write(reference.available);
			}
		}
		if ((num & availableDirtyBit) != 0)
		{
			writer.Write(available);
		}
		return num != 0;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
		uint num = reader.ReadPackedUInt32();
		if ((num & optionsDirtyBit) != 0)
		{
			Option[] array = new Option[reader.ReadPackedUInt32()];
			for (int i = 0; i < array.Length; i++)
			{
				ref Option reference = ref array[i];
				reference.pickupIndex = reader.ReadPickupIndex();
				reference.available = reader.ReadBoolean();
			}
			SetOptionsInternal(array);
		}
		if ((num & availableDirtyBit) != 0)
		{
			available = reader.ReadBoolean();
		}
	}

	public string GetContextString(Interactor activator)
	{
		return Language.GetString(contextString);
	}

	public Interactability GetInteractability(Interactor activator)
	{
		if (networkUIPromptController.inUse)
		{
			return Interactability.ConditionsNotMet;
		}
		if (!available)
		{
			return Interactability.Disabled;
		}
		return Interactability.Available;
	}

	public void OnInteractionBegin(Interactor activator)
	{
		if (base.enabled)
		{
			onServerInteractionBegin.Invoke(activator);
			networkUIPromptController.SetParticipantMasterFromInteractor(activator);
		}
	}

	public void SetDelusionOptionsFromInteractor(Interactor activator)
	{
		if (DelusionChestController.isDelusionEnable && TryGetComponent<DelusionChestController>(out var component))
		{
			if (component.IsDelusionChoiceReady)
			{
				component.GenerateDelusionPickupIndexes(activator);
				PickupIndex[] pickupIndexes = component.GetPickupIndexes(activator);
				Option[] array = new Option[pickupIndexes.Count()];
				for (int i = 0; i < pickupIndexes.Count(); i++)
				{
					array[i] = new Option
					{
						available = true,
						pickupIndex = pickupIndexes[i]
					};
				}
				SetOptionsServer(array);
			}
		}
		else
		{
			Debug.LogError("delusion is not enable but set with pickup picker controller");
		}
	}

	public void SetOptionsFromInteractor(Interactor activator)
	{
		if (!activator)
		{
			return;
		}
		CharacterBody component = activator.GetComponent<CharacterBody>();
		if (!component)
		{
			return;
		}
		Inventory inventory = component.inventory;
		if (!inventory)
		{
			return;
		}
		List<Option> list = new List<Option>();
		for (int i = 0; i < inventory.itemAcquisitionOrder.Count; i++)
		{
			ItemIndex itemIndex = inventory.itemAcquisitionOrder[i];
			ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
			ItemTierDef itemTierDef = ItemTierCatalog.GetItemTierDef(itemDef.tier);
			PickupIndex pickupIndex = PickupCatalog.FindPickupIndex(itemIndex);
			if ((!itemTierDef || itemTierDef.canScrap) && itemDef.canRemove && !itemDef.hidden && itemDef.DoesNotContainTag(ItemTag.Scrap) && (!(GetComponent<LemurianEggController>() != null) || !itemDef.ContainsTag(ItemTag.CannotCopy)) && (!isRebirthChoice || !itemDef.ContainsTag(ItemTag.RebirthBlacklist)))
			{
				list.Add(new Option
				{
					available = true,
					pickupIndex = pickupIndex
				});
			}
		}
		SetOptionsServer(list.ToArray());
	}

	public bool ShouldIgnoreSpherecastForInteractibility(Interactor activator)
	{
		return false;
	}

	public bool ShouldShowOnScanner()
	{
		if (GetComponent<DelusionChestController>() != null)
		{
			return available;
		}
		return true;
	}

	public bool ShouldProximityHighlight()
	{
		return shouldProximityHighlight;
	}

	public void AddPickupSelected()
	{
		numberOfPickUpsSelected++;
		if (numberOfPickUpsSelected >= GrabNumbers)
		{
			multiOptionsAvailable = false;
			KillPanel();
		}
	}

	private void KillPanel()
	{
		eventFunctions.DestroySelf();
	}

	public void OnDelusionPickupSelected(int pickupSelectedIndex)
	{
		if (base.enabled && TryGetComponent<DelusionChestController>(out var component))
		{
			component = GetComponent<DelusionChestController>();
			component.SelectedPickupIndex = new PickupIndex(pickupSelectedIndex);
			component.delusionChest.Open();
			SetAvailable(newAvailable: false);
		}
	}

	public IInspectInfoProvider GetInspectInfoProvider()
	{
		IInspectInfoProvider component = GetComponent<IInspectInfoProvider>();
		if (component != null)
		{
			return component;
		}
		return null;
	}

	static PickupPickerController()
	{
		optionsDirtyBit = 1u;
		availableDirtyBit = 2u;
		allDirtyBits = optionsDirtyBit | availableDirtyBit;
		GrabNumbers = 1;
		kRpcRpcHandlePickupSelected = 1904538683;
		NetworkBehaviour.RegisterRpcDelegate(typeof(PickupPickerController), kRpcRpcHandlePickupSelected, InvokeRpcRpcHandlePickupSelected);
		NetworkCRC.RegisterBehaviour("PickupPickerController", 0);
	}

	private void UNetVersion()
	{
	}

	protected static void InvokeRpcRpcHandlePickupSelected(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcHandlePickupSelected called on server.");
		}
		else
		{
			((PickupPickerController)obj).RpcHandlePickupSelected((int)reader.ReadPackedUInt32());
		}
	}

	public void CallRpcHandlePickupSelected(int choiceIndex)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcHandlePickupSelected called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcHandlePickupSelected);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.WritePackedUInt32((uint)choiceIndex);
		SendRPCInternal(networkWriter, 0, "RpcHandlePickupSelected");
	}

	public override void PreStartClient()
	{
	}
}
