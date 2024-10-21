using System.Collections;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.CharacterAI;

public class LemurianEggController : NetworkBehaviour
{
	private static float itemVFXDuration;

	[Tooltip("The master to spawn")]
	public GameObject masterPrefab;

	public SfxLocator sfxLocator;

	private Interactor interactor;

	private static int kCmdCmdOfferItem;

	public void SetInteractability(bool interactable)
	{
		PickupPickerController component = GetComponent<PickupPickerController>();
		if ((bool)component)
		{
			component.SetAvailable(interactable);
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

	[Server]
	public void AssignPotentialInteractor(Interactor potentialInteractor)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.CharacterAI.LemurianEggController::AssignPotentialInteractor(RoR2.Interactor)' called on client");
		}
		else
		{
			interactor = potentialInteractor;
		}
	}

	public override int GetNetworkChannel()
	{
		return QosChannelIndex.defaultReliable.intVal;
	}

	[Server]
	private void OfferItem(int intPickupIndex)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.CharacterAI.LemurianEggController::OfferItem(System.Int32)' called on client");
			return;
		}
		PickupIndex pickupIndex = new PickupIndex(intPickupIndex);
		PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
		if (pickupDef != null && (bool)interactor)
		{
			CharacterBody component = interactor.GetComponent<CharacterBody>();
			if ((bool)component && (bool)component.inventory && component.inventory.GetItemCount(pickupDef.itemIndex) > 0)
			{
				component.inventory.RemoveItem(pickupDef.itemIndex);
				CreateItemTakenOrb(component.corePosition, base.gameObject, pickupDef.itemIndex);
				GetComponent<PickupPickerController>().SetAvailable(newAvailable: false);
				StartCoroutine(WaitToSummonLemurian(pickupIndex));
			}
		}
	}

	[Command]
	public void CmdOfferItem(int intPickupIndex)
	{
		OfferItem(intPickupIndex);
	}

	[Server]
	public void CreateItemTakenOrb(Vector3 effectOrigin, GameObject targetObject, ItemIndex itemIndex)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.CharacterAI.LemurianEggController::CreateItemTakenOrb(UnityEngine.Vector3,UnityEngine.GameObject,RoR2.ItemIndex)' called on client");
			return;
		}
		EffectData effectData = new EffectData
		{
			origin = effectOrigin,
			genericFloat = itemVFXDuration,
			genericUInt = (uint)(itemIndex + 1)
		};
		effectData.SetNetworkedObjectReference(targetObject);
		EffectManager.SpawnEffect(DevotionInventoryController.s_effectPrefab, effectData, transmit: true);
	}

	private IEnumerator WaitToSummonLemurian(PickupIndex pickupIndex)
	{
		yield return new WaitForSeconds(itemVFXDuration);
		SummonLemurian(pickupIndex);
	}

	[Server]
	private void SummonLemurian(PickupIndex pickupIndex)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.CharacterAI.LemurianEggController::SummonLemurian(RoR2.PickupIndex)' called on client");
			return;
		}
		float num = 0f;
		CharacterMaster characterMaster = new MasterSummon
		{
			masterPrefab = masterPrefab,
			position = base.transform.position + Vector3.up * num,
			rotation = base.transform.rotation,
			summonerBodyObject = interactor?.gameObject,
			ignoreTeamMemberLimit = true,
			useAmbientLevel = true
		}.Perform();
		EffectData effectData = new EffectData
		{
			origin = base.gameObject.transform.position
		};
		if ((bool)characterMaster)
		{
			interactor.GetComponent<CharacterBody>();
			DevotionInventoryController orCreateDevotionInventoryController = DevotionInventoryController.GetOrCreateDevotionInventoryController(interactor);
			characterMaster.GetComponent<DevotedLemurianController>().InitializeDevotedLemurian(pickupIndex.itemIndex, orCreateDevotionInventoryController);
			orCreateDevotionInventoryController.GiveItem(pickupIndex.itemIndex);
			orCreateDevotionInventoryController.UpdateAllMinions();
			EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/LemurianEggHatching"), effectData, transmit: true);
		}
		Object.Destroy(base.gameObject);
	}

	static LemurianEggController()
	{
		itemVFXDuration = 1.5f;
		kCmdCmdOfferItem = -1179441837;
		NetworkBehaviour.RegisterCommandDelegate(typeof(LemurianEggController), kCmdCmdOfferItem, InvokeCmdCmdOfferItem);
		NetworkCRC.RegisterBehaviour("LemurianEggController", 0);
	}

	private void UNetVersion()
	{
	}

	protected static void InvokeCmdCmdOfferItem(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdOfferItem called on client.");
		}
		else
		{
			((LemurianEggController)obj).CmdOfferItem((int)reader.ReadPackedUInt32());
		}
	}

	public void CallCmdOfferItem(int intPickupIndex)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("Command function CmdOfferItem called on server.");
			return;
		}
		if (base.isServer)
		{
			CmdOfferItem(intPickupIndex);
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)5);
		networkWriter.WritePackedUInt32((uint)kCmdCmdOfferItem);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.WritePackedUInt32((uint)intPickupIndex);
		SendCommandInternal(networkWriter, 0, "CmdOfferItem");
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
