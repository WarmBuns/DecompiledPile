using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using RoR2.EntitlementManagement;
using RoR2.ExpansionManagement;
using RoR2.Networking;
using Unity;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class WokController : NetworkBehaviour, IInteractable, IDisplayNameProvider
{
	[Serializable]
	public class WokIngredientData : IEquatable<WokIngredientData>
	{
		public NetworkInstanceId playerNetId;

		public bool unlockedChef;

		public bool previouslyStoredIngredient1;

		public bool previouslyStoredIngredient2;

		public bool previouslyStoredIngredient3;

		public WokIngredientData()
		{
			playerNetId = default(NetworkInstanceId);
			unlockedChef = false;
			previouslyStoredIngredient1 = false;
			previouslyStoredIngredient2 = false;
			previouslyStoredIngredient3 = false;
		}

		public WokIngredientData(NetworkInstanceId _playerNetId)
		{
			playerNetId = _playerNetId;
			unlockedChef = false;
			previouslyStoredIngredient1 = false;
			previouslyStoredIngredient2 = false;
			previouslyStoredIngredient3 = false;
		}

		public WokIngredientData(PlayerCharacterMasterController playerController)
		{
			playerNetId = playerController.networkUser.netId;
			if (playerController.hasAuthority)
			{
				UserProfile userProfile = playerController.networkUser.localUser.userProfile;
				if (userProfile != null)
				{
					previouslyStoredIngredient1 = userProfile.chefAchievementIngredient1;
					previouslyStoredIngredient2 = userProfile.chefAchievementIngredient2;
					previouslyStoredIngredient3 = userProfile.chefAchievementIngredient3;
					UnlockableDef unlockableDef = UnlockableCatalog.GetUnlockableDef("Characters.Chef");
					unlockedChef = userProfile.HasUnlockable(unlockableDef);
				}
			}
		}

		public bool Equals(WokIngredientData other)
		{
			if (playerNetId == other.playerNetId && previouslyStoredIngredient1 == other.previouslyStoredIngredient1 && previouslyStoredIngredient2 == other.previouslyStoredIngredient2 && previouslyStoredIngredient3 == other.previouslyStoredIngredient3)
			{
				return unlockedChef == other.unlockedChef;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (int)playerNetId.Value;
		}
	}

	[SerializeField]
	private ItemDef Ingredient1;

	[SerializeField]
	private ItemDef Ingredient2;

	[SerializeField]
	private ItemDef Ingredient3;

	[SerializeField]
	private GameObject IngredientObject1;

	[SerializeField]
	private GameObject IngredientObject1Received;

	[SerializeField]
	private GameObject IngredientObject2;

	[SerializeField]
	private GameObject IngredientObject2Received;

	[SerializeField]
	private GameObject IngredientObject3;

	[SerializeField]
	private GameObject IngredientObject3Received;

	[SerializeField]
	private ChildLocator childLocator;

	[SerializeField]
	private GameObject dissolveVFX;

	[SerializeField]
	private GameObject wokCookingVFX;

	[SerializeField]
	private GameObject chefUnlockVFX;

	[SerializeField]
	private GameObject chefModelToAnimate;

	[SerializeField]
	public ExpansionDef requiredExpansion;

	private bool Ingredient1Received;

	private bool Ingredient2Received;

	private bool Ingredient3Received;

	private Interactor cachedCookInteractor;

	private CharacterBody cookBody;

	[SerializeField]
	private string chefAchievement = "ActivateChef";

	[SerializeField]
	private string chefUnlockable = "Characters.Chef";

	private int validRecipe;

	public string displayNameToken = "WOK_NAME";

	public string contextToken = "WOK_CONTEXT";

	private bool receivedAllIngredients;

	private bool storingIngredient = true;

	[SerializeField]
	private float storingIngredientDuration = 0.75f;

	private WokIngredientData localWokData;

	private bool serverDisableHidingChef;

	[SyncVar]
	private PickupIndex pickupIndex;

	private float fixedAge;

	[SerializeField]
	private float mixingDuration = 1.5f;

	[SyncVar]
	private bool mixingBegin;

	[SyncVar]
	private bool mixingFinished;

	private bool interactorPresent;

	private float interactorDelay = 3f;

	private static Dictionary<NetworkInstanceId, WokIngredientData> wokDataDictionary;

	[SerializeField]
	public Animator WokAnimator;

	private static int kRpcRpcSetRecipeVariables;

	private static int kRpcRpcHideChef;

	private static int kRpcRpcUpdateLocalClientData;

	private static int kCmdCmdTransmitLocalPlayerWokDataToServer;

	public PickupIndex NetworkpickupIndex
	{
		get
		{
			return pickupIndex;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref pickupIndex, 1u);
		}
	}

	public bool NetworkmixingBegin
	{
		get
		{
			return mixingBegin;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref mixingBegin, 2u);
		}
	}

	public bool NetworkmixingFinished
	{
		get
		{
			return mixingFinished;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref mixingFinished, 4u);
		}
	}

	public static event Action<Interactor> onOpened;

	public static event Action<Interactor> chefRecipeComplete;

	public void OnInteractionBegin(Interactor activator)
	{
		cachedCookInteractor = activator;
		cookBody = cachedCookInteractor.GetComponent<CharacterBody>();
		if (!(cookBody == null))
		{
			NetworkInstanceId key = cookBody.master.playerCharacterMasterController.networkUser.netId;
			if (wokDataDictionary.ContainsKey(key))
			{
				ReceiveIngredient(cookBody, wokDataDictionary[key]);
			}
		}
	}

	[ClientRpc]
	private void RpcSetRecipeVariables(bool _storingIngredient, bool _receivedAllIngredients)
	{
		if (!NetworkServer.active)
		{
			storingIngredient |= _storingIngredient;
			receivedAllIngredients = _receivedAllIngredients;
			if (_storingIngredient || _receivedAllIngredients)
			{
				fixedAge = 0f;
			}
		}
	}

	[ClientRpc]
	private void RpcHideChef()
	{
		WokAnimator.gameObject.SetActive(value: false);
	}

	[Server]
	private void EvaluateChefVisibility()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.WokController::EvaluateChefVisibility()' called on client");
		}
		else if (!serverDisableHidingChef && CheckForCompletePlayerUnlock())
		{
			WokAnimator.gameObject.SetActive(value: false);
			serverDisableHidingChef = true;
			CallRpcHideChef();
		}
	}

	private bool CheckForCompletePlayerUnlock()
	{
		foreach (KeyValuePair<NetworkInstanceId, WokIngredientData> item in wokDataDictionary)
		{
			if (!item.Value.unlockedChef)
			{
				return false;
			}
		}
		return true;
	}

	private void FixedUpdate()
	{
		fixedAge += Time.deltaTime;
		if (storingIngredient && fixedAge > storingIngredientDuration)
		{
			storingIngredient = false;
			UpdateIngredientVisibility(localWokData);
			if (NetworkServer.active)
			{
				EvaluateChefVisibility();
			}
		}
		if (!receivedAllIngredients || fixedAge < mixingDuration)
		{
			return;
		}
		if (!mixingBegin)
		{
			NetworkmixingBegin = true;
			MixIngredients();
			fixedAge = 0f;
			mixingDuration = 0.25f;
		}
		else if (!mixingFinished)
		{
			NetworkmixingFinished = true;
			RecipeComplete();
			if ((bool)WokAnimator)
			{
				WokAnimator.SetBool("Activated", value: true);
			}
			if (NetworkServer.active)
			{
				WokController.chefRecipeComplete?.Invoke(cachedCookInteractor);
			}
			DisableAllIngredientVisibility();
		}
	}

	[Server]
	private void ReceiveIngredient(CharacterBody cookBody, WokIngredientData wokData)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.WokController::ReceiveIngredient(RoR2.CharacterBody,RoR2.WokController/WokIngredientData)' called on client");
		}
		else if (TryGrabIngredient(cookBody, wokData))
		{
			serverDisableHidingChef = true;
			fixedAge = 0f;
			storingIngredient = true;
			if (CheckRecipeComplete(wokData))
			{
				wokData.unlockedChef = true;
				fixedAge = 0f;
				receivedAllIngredients = true;
				cachedCookInteractor = cookBody.GetComponent<Interactor>();
			}
			CallRpcSetRecipeVariables(storingIngredient, receivedAllIngredients);
			wokDataDictionary[wokData.playerNetId] = wokData;
			CallRpcUpdateLocalClientData(wokData);
		}
	}

	private bool TryGrabIngredient(CharacterBody cookBody, WokIngredientData wokData)
	{
		if (Ingredient1 != null && TryTakeSpecificIngredient(Ingredient1.itemIndex, IngredientObject1, ref wokData.previouslyStoredIngredient1))
		{
			return true;
		}
		if (Ingredient2 != null && TryTakeSpecificIngredient(Ingredient2.itemIndex, IngredientObject2, ref wokData.previouslyStoredIngredient2))
		{
			return true;
		}
		if (Ingredient3 != null && TryTakeSpecificIngredient(Ingredient3.itemIndex, IngredientObject3, ref wokData.previouslyStoredIngredient3))
		{
			return true;
		}
		return false;
		bool TryTakeSpecificIngredient(ItemIndex itemIndex, GameObject ingredientObject, ref bool wokContainsIngredient)
		{
			if (wokContainsIngredient)
			{
				return false;
			}
			if (!cookBody.inventory.itemAcquisitionOrder.Contains(itemIndex))
			{
				return false;
			}
			wokContainsIngredient = true;
			cookBody.inventory.RemoveItem(itemIndex);
			return true;
		}
	}

	private void UpdateIngredientVisibility(WokIngredientData wokData)
	{
		if (wokData != null)
		{
			UpdateIngredientAppearance(IngredientObject1Received, IngredientObject1, wokData.unlockedChef || wokData.previouslyStoredIngredient1);
			UpdateIngredientAppearance(IngredientObject2Received, IngredientObject2, wokData.unlockedChef || wokData.previouslyStoredIngredient2);
			UpdateIngredientAppearance(IngredientObject3Received, IngredientObject3, wokData.unlockedChef || wokData.previouslyStoredIngredient3);
		}
		static void UpdateIngredientAppearance(GameObject ingredientObjReceived, GameObject ingredientObj, bool shouldUnlock)
		{
			ingredientObjReceived.SetActive(shouldUnlock);
			ingredientObj.SetActive(!shouldUnlock);
		}
	}

	private void DisableAllIngredientVisibility()
	{
		IngredientObject1Received.SetActive(value: false);
		IngredientObject1.SetActive(value: false);
		IngredientObject2Received.SetActive(value: false);
		IngredientObject2.SetActive(value: false);
		IngredientObject3Received.SetActive(value: false);
		IngredientObject3.SetActive(value: false);
	}

	private void MixIngredients()
	{
		IngredientObject1Received.SetActive(value: false);
		EffectData effectData = new EffectData
		{
			origin = IngredientObject1.transform.position,
			genericFloat = mixingDuration,
			genericUInt = (uint)(Ingredient1.itemIndex + 1)
		};
		effectData.SetNetworkedObjectReference(base.gameObject);
		EffectManager.SpawnEffect(dissolveVFX, effectData, transmit: false);
		IngredientObject2Received.SetActive(value: false);
		EffectData effectData2 = new EffectData
		{
			origin = IngredientObject2.transform.position,
			genericFloat = mixingDuration,
			genericUInt = (uint)(Ingredient2.itemIndex + 1)
		};
		effectData2.SetNetworkedObjectReference(base.gameObject);
		EffectManager.SpawnEffect(dissolveVFX, effectData2, transmit: false);
		IngredientObject3Received.SetActive(value: false);
		EffectData effectData3 = new EffectData
		{
			origin = IngredientObject3.transform.position,
			genericFloat = mixingDuration,
			genericUInt = (uint)(Ingredient3.itemIndex + 1)
		};
		effectData3.SetNetworkedObjectReference(base.gameObject);
		EffectManager.SpawnEffect(dissolveVFX, effectData3, transmit: false);
		Transform transform = childLocator.FindChild("ModelPivot");
		EffectData effectData4 = new EffectData
		{
			origin = transform.position,
			genericFloat = mixingDuration,
			genericUInt = (uint)(Ingredient1.itemIndex + 1)
		};
		EffectManager.SpawnEffect(wokCookingVFX, effectData4, transmit: false);
		Util.PlaySound("Play_obj_chefWok_cook_all", base.gameObject);
	}

	private void RecipeComplete()
	{
		Transform transform = childLocator.FindChild("ModelPivot");
		EffectData effectData = new EffectData
		{
			origin = transform.position,
			genericFloat = 1.5f,
			genericUInt = (uint)(Ingredient1.itemIndex + 1)
		};
		EffectManager.SpawnEffect(chefUnlockVFX, effectData, transmit: false);
		Util.PlaySound("Play_obj_chefWok_cook_complete", base.gameObject);
		if (NetworkServer.active)
		{
			serverDisableHidingChef = true;
		}
	}

	private bool CheckRecipeComplete(WokIngredientData wokData)
	{
		if (wokData.previouslyStoredIngredient1 && wokData.previouslyStoredIngredient2)
		{
			return wokData.previouslyStoredIngredient3;
		}
		return false;
	}

	public string GetContextString(Interactor activator)
	{
		return Language.GetString(contextToken);
	}

	public override int GetNetworkChannel()
	{
		return QosChannelIndex.defaultReliable.intVal;
	}

	public Interactability GetInteractability(Interactor activator)
	{
		cookBody = activator.GetComponent<CharacterBody>();
		Run instance = Run.instance;
		if ((bool)instance && (bool)requiredExpansion)
		{
			if (!instance.IsExpansionEnabled(requiredExpansion))
			{
				return Interactability.Disabled;
			}
			EntitlementDef requiredEntitlement = requiredExpansion.requiredEntitlement;
			if ((bool)requiredEntitlement)
			{
				PlayerCharacterMasterController playerCharacterMasterController = cookBody.master.playerCharacterMasterController;
				if (!playerCharacterMasterController)
				{
					return Interactability.Disabled;
				}
				PlayerCharacterMasterControllerEntitlementTracker component = playerCharacterMasterController.GetComponent<PlayerCharacterMasterControllerEntitlementTracker>();
				if (!component)
				{
					Debug.LogWarning("Rejecting interact because the playerCharacterMasterController doesn't have a sibling PlayerCharacterMasterControllerEntitlementTracker");
					return Interactability.Disabled;
				}
				if (!component.HasEntitlement(requiredEntitlement))
				{
					Debug.LogWarning("Rejecting interact because the player doesn't have entitlement " + requiredEntitlement.name);
					return Interactability.Disabled;
				}
			}
		}
		if (receivedAllIngredients || storingIngredient)
		{
			return Interactability.Disabled;
		}
		if (cookBody == null)
		{
			return Interactability.Disabled;
		}
		NetworkInstanceId key = cookBody.master.playerCharacterMasterController.networkUser.netId;
		if (!wokDataDictionary.ContainsKey(key))
		{
			return Interactability.Disabled;
		}
		WokIngredientData wokIngredientData = wokDataDictionary[key];
		if (wokIngredientData.unlockedChef)
		{
			return Interactability.Disabled;
		}
		if (!((cookBody.inventory.itemAcquisitionOrder.Contains(Ingredient1.itemIndex) && !wokIngredientData.previouslyStoredIngredient1) | (cookBody.inventory.itemAcquisitionOrder.Contains(Ingredient2.itemIndex) && !wokIngredientData.previouslyStoredIngredient2) | (cookBody.inventory.itemAcquisitionOrder.Contains(Ingredient3.itemIndex) && !wokIngredientData.previouslyStoredIngredient3)))
		{
			return Interactability.Disabled;
		}
		return Interactability.Available;
	}

	public string GetDisplayName()
	{
		return Language.GetString(displayNameToken);
	}

	public bool ShouldIgnoreSpherecastForInteractibility(Interactor activator)
	{
		return false;
	}

	public void OnEnable()
	{
		InstanceTracker.Add(this);
		UpdateAndSendLocalPlayerDataToServer();
	}

	public void OnDisable()
	{
		InstanceTracker.Remove(this);
	}

	public bool ShouldShowOnScanner()
	{
		return !receivedAllIngredients;
	}

	public bool ShouldProximityHighlight()
	{
		return false;
	}

	public void UpdateAndSendLocalPlayerDataToServer()
	{
		foreach (PlayerCharacterMasterController instance in PlayerCharacterMasterController.instances)
		{
			if (!(instance == null) && instance.networkUser.hasAuthority && instance.networkUser.localUser.userProfile != null)
			{
				WokIngredientData wokData = new WokIngredientData(instance);
				UpdateIngredientVisibility(wokData);
				CacheAndSendWokData(wokData);
				localWokData = wokData;
			}
		}
	}

	private void CacheAndSendWokData(WokIngredientData wokData)
	{
		wokDataDictionary[wokData.playerNetId] = wokData;
		if (!NetworkServer.active)
		{
			NetworkWriter networkWriter = new NetworkWriter();
			networkWriter.StartMessage(80);
			networkWriter.Write(wokData.playerNetId.Value);
			networkWriter.Write(wokData.unlockedChef);
			networkWriter.Write(wokData.previouslyStoredIngredient1);
			networkWriter.Write(wokData.previouslyStoredIngredient2);
			networkWriter.Write(wokData.previouslyStoredIngredient3);
			networkWriter.FinishMessage();
			ClientScene.readyConnection.SendWriter(networkWriter, QosChannelIndex.defaultReliable.intVal);
		}
	}

	[NetworkMessageHandler(msgType = 80, server = true)]
	private static void ServerReceiveWokData(NetworkMessage netMsg)
	{
		WokIngredientData wokIngredientData = new WokIngredientData(new NetworkInstanceId(netMsg.reader.ReadUInt32()));
		wokIngredientData.unlockedChef = netMsg.reader.ReadBoolean();
		wokIngredientData.previouslyStoredIngredient1 = netMsg.reader.ReadBoolean();
		wokIngredientData.previouslyStoredIngredient2 = netMsg.reader.ReadBoolean();
		wokIngredientData.previouslyStoredIngredient3 = netMsg.reader.ReadBoolean();
		wokDataDictionary[wokIngredientData.playerNetId] = wokIngredientData;
	}

	private PlayerCharacterMasterController FindLocalPlayerCharacterMasterControllerByNetId(NetworkInstanceId netId)
	{
		foreach (PlayerCharacterMasterController instance in PlayerCharacterMasterController.instances)
		{
			if (!(instance == null) && instance.networkUser.netId == netId && instance.networkUser.hasAuthority)
			{
				return instance;
			}
		}
		return null;
	}

	[ClientRpc]
	public void RpcUpdateLocalClientData(WokIngredientData wokData)
	{
		PlayerCharacterMasterController playerCharacterMasterController = FindLocalPlayerCharacterMasterControllerByNetId(wokData.playerNetId);
		if (playerCharacterMasterController == null)
		{
			return;
		}
		UserProfile userProfile = playerCharacterMasterController.networkUser?.localUser?.userProfile;
		if (userProfile != null)
		{
			bool flag = userProfile.HasAchievement(chefAchievement);
			wokData.unlockedChef |= flag;
			if (!wokData.unlockedChef || (wokData.unlockedChef && !flag))
			{
				GameObject playerObject = playerCharacterMasterController.master.GetBody()?.gameObject;
				TryAddNewIngredientClient(ref userProfile.chefAchievementIngredient1, wokData.previouslyStoredIngredient1, Ingredient1.itemIndex, IngredientObject1, playerObject);
				TryAddNewIngredientClient(ref userProfile.chefAchievementIngredient2, wokData.previouslyStoredIngredient2, Ingredient2.itemIndex, IngredientObject2, playerObject);
				TryAddNewIngredientClient(ref userProfile.chefAchievementIngredient3, wokData.previouslyStoredIngredient3, Ingredient3.itemIndex, IngredientObject3, playerObject);
			}
			else
			{
				userProfile.chefAchievementIngredient1 = false;
				userProfile.chefAchievementIngredient2 = false;
				userProfile.chefAchievementIngredient3 = false;
			}
			wokDataDictionary[wokData.playerNetId] = wokData;
			localWokData = wokData;
		}
	}

	private void TryAddNewIngredientClient(ref bool profileHasIngredient, bool wokDataHasIngredient, ItemIndex itemIndex, GameObject ingredientObject, GameObject playerObject)
	{
		if (profileHasIngredient != wokDataHasIngredient && wokDataHasIngredient)
		{
			profileHasIngredient = true;
			GameObject effectPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/DelusionItemDissolveVFX");
			EffectData effectData = new EffectData
			{
				origin = playerObject.transform.position,
				genericFloat = storingIngredientDuration,
				genericUInt = (uint)(itemIndex + 1)
			};
			if (playerObject != null)
			{
				effectData.SetNetworkedObjectReference(ingredientObject);
			}
			EffectManager.SpawnEffect(effectPrefab, effectData, transmit: false);
			Util.PlaySound("Play_obj_chefWok_item1_place", base.gameObject);
			fixedAge = 0f;
		}
	}

	[Command]
	public void CmdTransmitLocalPlayerWokDataToServer(WokIngredientData wokData)
	{
		wokDataDictionary[wokData.playerNetId] = wokData;
	}

	static WokController()
	{
		wokDataDictionary = new Dictionary<NetworkInstanceId, WokIngredientData>();
		kCmdCmdTransmitLocalPlayerWokDataToServer = -976766546;
		NetworkBehaviour.RegisterCommandDelegate(typeof(WokController), kCmdCmdTransmitLocalPlayerWokDataToServer, InvokeCmdCmdTransmitLocalPlayerWokDataToServer);
		kRpcRpcSetRecipeVariables = 429636140;
		NetworkBehaviour.RegisterRpcDelegate(typeof(WokController), kRpcRpcSetRecipeVariables, InvokeRpcRpcSetRecipeVariables);
		kRpcRpcHideChef = -1575737907;
		NetworkBehaviour.RegisterRpcDelegate(typeof(WokController), kRpcRpcHideChef, InvokeRpcRpcHideChef);
		kRpcRpcUpdateLocalClientData = -667434414;
		NetworkBehaviour.RegisterRpcDelegate(typeof(WokController), kRpcRpcUpdateLocalClientData, InvokeRpcRpcUpdateLocalClientData);
		NetworkCRC.RegisterBehaviour("WokController", 0);
	}

	private void UNetVersion()
	{
	}

	protected static void InvokeCmdCmdTransmitLocalPlayerWokDataToServer(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdTransmitLocalPlayerWokDataToServer called on client.");
		}
		else
		{
			((WokController)obj).CmdTransmitLocalPlayerWokDataToServer(GeneratedNetworkCode._ReadWokIngredientData_WokController(reader));
		}
	}

	public void CallCmdTransmitLocalPlayerWokDataToServer(WokIngredientData wokData)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("Command function CmdTransmitLocalPlayerWokDataToServer called on server.");
			return;
		}
		if (base.isServer)
		{
			CmdTransmitLocalPlayerWokDataToServer(wokData);
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)5);
		networkWriter.WritePackedUInt32((uint)kCmdCmdTransmitLocalPlayerWokDataToServer);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		GeneratedNetworkCode._WriteWokIngredientData_WokController(networkWriter, wokData);
		SendCommandInternal(networkWriter, 0, "CmdTransmitLocalPlayerWokDataToServer");
	}

	protected static void InvokeRpcRpcSetRecipeVariables(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcSetRecipeVariables called on server.");
		}
		else
		{
			((WokController)obj).RpcSetRecipeVariables(reader.ReadBoolean(), reader.ReadBoolean());
		}
	}

	protected static void InvokeRpcRpcHideChef(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcHideChef called on server.");
		}
		else
		{
			((WokController)obj).RpcHideChef();
		}
	}

	protected static void InvokeRpcRpcUpdateLocalClientData(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcUpdateLocalClientData called on server.");
		}
		else
		{
			((WokController)obj).RpcUpdateLocalClientData(GeneratedNetworkCode._ReadWokIngredientData_WokController(reader));
		}
	}

	public void CallRpcSetRecipeVariables(bool _storingIngredient, bool _receivedAllIngredients)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcSetRecipeVariables called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcSetRecipeVariables);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.Write(_storingIngredient);
		networkWriter.Write(_receivedAllIngredients);
		SendRPCInternal(networkWriter, 0, "RpcSetRecipeVariables");
	}

	public void CallRpcHideChef()
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcHideChef called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcHideChef);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		SendRPCInternal(networkWriter, 0, "RpcHideChef");
	}

	public void CallRpcUpdateLocalClientData(WokIngredientData wokData)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcUpdateLocalClientData called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcUpdateLocalClientData);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		GeneratedNetworkCode._WriteWokIngredientData_WokController(networkWriter, wokData);
		SendRPCInternal(networkWriter, 0, "RpcUpdateLocalClientData");
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			GeneratedNetworkCode._WritePickupIndex_None(writer, pickupIndex);
			writer.Write(mixingBegin);
			writer.Write(mixingFinished);
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
			writer.Write(mixingBegin);
		}
		if ((base.syncVarDirtyBits & 4u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(mixingFinished);
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
			mixingBegin = reader.ReadBoolean();
			mixingFinished = reader.ReadBoolean();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			pickupIndex = GeneratedNetworkCode._ReadPickupIndex_None(reader);
		}
		if (((uint)num & 2u) != 0)
		{
			mixingBegin = reader.ReadBoolean();
		}
		if (((uint)num & 4u) != 0)
		{
			mixingFinished = reader.ReadBoolean();
		}
	}

	public override void PreStartClient()
	{
	}
}
