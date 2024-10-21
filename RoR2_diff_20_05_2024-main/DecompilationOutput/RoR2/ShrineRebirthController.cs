using System;
using System.Runtime.InteropServices;
using EntityStates.ShrineRebirth;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace RoR2;

public class ShrineRebirthController : NetworkBehaviour, IDisplayNameProvider
{
	[Serializable]
	public class InteractorUnityEvent : UnityEvent
	{
	}

	private Interactor rebirthCandidate;

	private CharacterBody rebirthBody;

	[SerializeField]
	private int maxItemsTogive = 4;

	public EntityStateMachine stateMachine;

	public InteractorUnityEvent onRebirthChoiceFinished;

	private int itemsGiven;

	[SerializeField]
	private NetworkUIPromptController networkUIPromptController;

	private bool RebirthItemGiven;

	[SyncVar]
	public GameObject portalObject;

	public PurchaseInteraction purchaseInteraction;

	public ChildLocator childLocator;

	public bool isForTesting;

	public GameObject onShrineRevealObjectiveTextObject;

	public GameObject onRebirthOrContinueTextObject;

	[SerializeField]
	public InteractableSpawnCard helminthPortalISC;

	private ItemIndex rebirthIndex = ItemIndex.None;

	private PickupPickerController _pickupPickerController;

	private NetworkUIPromptController _netUIPromptController;

	private NetworkInstanceId ___portalObjectNetId;

	private static int kRpcRpcChangeInteractScript;

	public GameObject NetworkportalObject
	{
		get
		{
			return portalObject;
		}
		[param: In]
		set
		{
			SetSyncVarGameObject(value, ref portalObject, 1u, ref ___portalObjectNetId);
		}
	}

	private NetworkUser GetNetworkUserFromCandidate()
	{
		if ((bool)rebirthBody)
		{
			CharacterMaster master = rebirthBody.master;
			if ((bool)master)
			{
				PlayerCharacterMasterController playerCharacterMasterController = master.playerCharacterMasterController;
				if ((bool)playerCharacterMasterController)
				{
					NetworkUser networkUser = playerCharacterMasterController.networkUser;
					if ((bool)networkUser)
					{
						return networkUser;
					}
				}
			}
		}
		return null;
	}

	[Server]
	public void StoreRebirthItemChoice(int itemChoice)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.ShrineRebirthController::StoreRebirthItemChoice(System.Int32)' called on client");
			return;
		}
		PickupDef pickupDef = PickupCatalog.GetPickupDef(new PickupIndex(itemChoice));
		_ = rebirthCandidate == null;
		if (pickupDef == null || !rebirthCandidate || !rebirthBody || !rebirthBody.inventory)
		{
			return;
		}
		rebirthBody.inventory.RemoveItem(pickupDef.itemIndex);
		itemsGiven++;
		StoreRebirthItem(pickupDef);
		if (!MaxRebirthItemsGiven())
		{
			return;
		}
		foreach (NetworkUser readOnlyInstances in NetworkUser.readOnlyInstancesList)
		{
			readOnlyInstances.CallCmdStoreRebirthItems(rebirthIndex);
		}
		onRebirthChoiceFinished?.Invoke();
		stateMachine.SetNextState(new RebirthChoice());
	}

	private bool MaxRebirthItemsGiven()
	{
		bool result = false;
		if (itemsGiven >= maxItemsTogive)
		{
			result = true;
		}
		return result;
	}

	private void StoreRebirthItem(PickupDef pickupDef)
	{
		if ((bool)GetNetworkUserFromCandidate())
		{
			if (!RebirthItemGiven)
			{
				RebirthItemGiven = true;
				rebirthIndex = pickupDef.itemIndex;
			}
			if ((bool)rebirthCandidate)
			{
				Run.instance.GrantUnlockToAllParticipatingPlayers(UnlockableCatalog.GetUnlockableDef("Artifacts.Rebirth"));
			}
		}
	}

	public void AssignPotentialInteractor(Interactor potentialInteractor)
	{
		if (!rebirthCandidate)
		{
			rebirthCandidate = potentialInteractor;
			rebirthBody = rebirthCandidate.GetComponent<CharacterBody>();
		}
	}

	private void Start()
	{
		_pickupPickerController = base.gameObject.GetComponent<PickupPickerController>();
		_netUIPromptController = GetComponent<NetworkUIPromptController>();
		_netUIPromptController.enabled = false;
		_pickupPickerController.enabled = false;
	}

	[ClientRpc]
	public void RpcChangeInteractScript()
	{
		UnityEngine.Object.Destroy(base.gameObject.GetComponent<PurchaseInteraction>());
		_netUIPromptController.enabled = true;
		_pickupPickerController.enabled = true;
		_pickupPickerController.SetAvailable(newAvailable: true);
		portalObject.SetActive(value: true);
	}

	public void SetEntityStateMachineToRebirthOrPortal()
	{
		stateMachine.SetNextState(new RebirthOrPortalChoice());
	}

	public string GetDisplayName()
	{
		return Language.GetString("SHRINE_REBIRTH_NAME");
	}

	private void UNetVersion()
	{
	}

	protected static void InvokeRpcRpcChangeInteractScript(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcChangeInteractScript called on server.");
		}
		else
		{
			((ShrineRebirthController)obj).RpcChangeInteractScript();
		}
	}

	public void CallRpcChangeInteractScript()
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcChangeInteractScript called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcChangeInteractScript);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		SendRPCInternal(networkWriter, 0, "RpcChangeInteractScript");
	}

	static ShrineRebirthController()
	{
		kRpcRpcChangeInteractScript = 1007694444;
		NetworkBehaviour.RegisterRpcDelegate(typeof(ShrineRebirthController), kRpcRpcChangeInteractScript, InvokeRpcRpcChangeInteractScript);
		NetworkCRC.RegisterBehaviour("ShrineRebirthController", 0);
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.Write(portalObject);
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
			writer.Write(portalObject);
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
			___portalObjectNetId = reader.ReadNetworkId();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			portalObject = reader.ReadGameObject();
		}
	}

	public override void PreStartClient()
	{
		if (!___portalObjectNetId.IsEmpty())
		{
			NetworkportalObject = ClientScene.FindLocalObject(___portalObjectNetId);
		}
	}
}
