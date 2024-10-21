using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using RoR2.EntitlementManagement;
using RoR2.ExpansionManagement;
using RoR2.Hologram;
using RoR2.Stats;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(Highlight))]
public sealed class PurchaseInteraction : NetworkBehaviour, IInteractable, IInspectable, ISupportsMultipleInspectZones, IHologramContentProvider, IDisplayNameProvider
{
	[SyncVar]
	public string displayNameToken;

	[SyncVar]
	public string contextToken;

	public CostTypeIndex costType;

	[SyncVar]
	public bool available = true;

	[SyncVar]
	public int cost;

	public int solitudeCost;

	public bool automaticallyScaleCostWithDifficulty;

	[Tooltip("The unlockable that a player must have to be able to interact with this terminal.")]
	public string requiredUnlockable = "";

	public bool ignoreSpherecastForInteractability;

	public string[] purchaseStatNames;

	public bool setUnavailableOnTeleporterActivated;

	public bool isShrine;

	public bool isGoldShrine;

	public bool shouldProximityHighlight = true;

	public bool saleStarCompatible;

	[SerializeField]
	public ExpansionDef requiredExpansion;

	private IInspectInfoProvider[] cachedInspectInfoProviders;

	[HideInInspector]
	public Interactor lastActivator;

	[SyncVar]
	public GameObject lockGameObject;

	private Xoroshiro128Plus rng;

	private GameObjectUnlockableFilter gameObjectUnlockableFilter;

	private static readonly StringBuilder sharedStringBuilder = new StringBuilder();

	public PurchaseEvent onPurchase;

	private Transform viewerTransform;

	private CharacterBody viewerBody;

	private int currentInspectIndex;

	private NetworkInstanceId ___lockGameObjectNetId;

	public string NetworkdisplayNameToken
	{
		get
		{
			return displayNameToken;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref displayNameToken, 1u);
		}
	}

	public string NetworkcontextToken
	{
		get
		{
			return contextToken;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref contextToken, 2u);
		}
	}

	public bool Networkavailable
	{
		get
		{
			return available;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref available, 4u);
		}
	}

	public int Networkcost
	{
		get
		{
			return cost;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref cost, 8u);
		}
	}

	public GameObject NetworklockGameObject
	{
		get
		{
			return lockGameObject;
		}
		[param: In]
		set
		{
			SetSyncVarGameObject(value, ref lockGameObject, 16u, ref ___lockGameObjectNetId);
		}
	}

	public static event Action<PurchaseInteraction, Interactor> onItemSpentOnPurchase;

	public static event Action<PurchaseInteraction, Interactor, EquipmentIndex> onEquipmentSpentOnPurchase;

	private void Awake()
	{
		if (NetworkServer.active)
		{
			if (automaticallyScaleCostWithDifficulty)
			{
				Networkcost = Run.instance.GetDifficultyScaledCost(cost);
				solitudeCost = cost;
			}
			rng = new Xoroshiro128Plus(Run.instance.treasureRng.nextUlong);
		}
		cachedInspectInfoProviders = GetComponents<IInspectInfoProvider>();
	}

	private void Start()
	{
		gameObjectUnlockableFilter = base.gameObject.GetComponent<GameObjectUnlockableFilter>();
	}

	[Server]
	public void SetAvailable(bool newAvailable)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.PurchaseInteraction::SetAvailable(System.Boolean)' called on client");
		}
		else
		{
			Networkavailable = newAvailable;
		}
	}

	[Server]
	public void SetUnavailableTemporarily(float time)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.PurchaseInteraction::SetUnavailableTemporarily(System.Single)' called on client");
			return;
		}
		Networkavailable = false;
		Invoke("SetAvailableTrue", time);
	}

	private void SetAvailableTrue()
	{
		Networkavailable = true;
	}

	public string GetDisplayName()
	{
		return Language.GetString(displayNameToken);
	}

	private static bool ActivatorHasUnlockable(Interactor activator, string unlockableName)
	{
		NetworkUser networkUser = Util.LookUpBodyNetworkUser(activator.gameObject);
		if ((bool)networkUser)
		{
			LocalUser localUser = networkUser.localUser;
			if (localUser != null)
			{
				return localUser.userProfile.HasUnlockable(unlockableName);
			}
		}
		return true;
	}

	public string GetContextString(Interactor activator)
	{
		sharedStringBuilder.Clear();
		sharedStringBuilder.Append(Language.GetString(contextToken));
		if (costType != 0)
		{
			sharedStringBuilder.Append(" <nobr>(");
			if (costType == CostTypeIndex.Money)
			{
				if (TeamManager.LongstandingSolitudesInParty() > 0)
				{
					CostTypeCatalog.GetCostTypeDef(costType).BuildCostStringStyled(cost * (TeamManager.LongstandingSolitudesInParty() + 1), sharedStringBuilder, forWorldDisplay: false);
				}
				else
				{
					CostTypeCatalog.GetCostTypeDef(costType).BuildCostStringStyled(cost, sharedStringBuilder, forWorldDisplay: false);
				}
			}
			else
			{
				CostTypeCatalog.GetCostTypeDef(costType).BuildCostStringStyled(cost, sharedStringBuilder, forWorldDisplay: false);
			}
			sharedStringBuilder.Append(")</nobr>");
		}
		return sharedStringBuilder.ToString();
	}

	public Interactability GetInteractability(Interactor activator)
	{
		if (!string.IsNullOrEmpty(requiredUnlockable) && !ActivatorHasUnlockable(activator, requiredUnlockable))
		{
			return Interactability.Disabled;
		}
		if (!available || (bool)lockGameObject)
		{
			return Interactability.Disabled;
		}
		if (gameObjectUnlockableFilter != null)
		{
			NetworkUser networkUser = Util.LookUpBodyNetworkUser(activator.gameObject);
			if ((bool)networkUser)
			{
				LocalUser localUser = networkUser.localUser;
				if (localUser != null && localUser.userProfile.HasAchievement(gameObjectUnlockableFilter.AchievementToDisable))
				{
					return Interactability.Disabled;
				}
			}
		}
		Run instance = Run.instance;
		if ((bool)instance && (bool)requiredExpansion)
		{
			if (!instance.IsExpansionEnabled(requiredExpansion))
			{
				return Interactability.Disabled;
			}
			EntitlementDef requiredEntitlement = requiredExpansion.requiredEntitlement;
			CharacterBody component = activator.GetComponent<CharacterBody>();
			if ((bool)component && (bool)requiredEntitlement)
			{
				PlayerCharacterMasterController playerCharacterMasterController = component.master.playerCharacterMasterController;
				if (!playerCharacterMasterController)
				{
					return Interactability.Disabled;
				}
				PlayerCharacterMasterControllerEntitlementTracker component2 = playerCharacterMasterController.GetComponent<PlayerCharacterMasterControllerEntitlementTracker>();
				if (!component2)
				{
					Debug.LogWarning("Rejecting interact because the playerCharacterMasterController doesn't have a sibling PlayerCharacterMasterControllerEntitlementTracker");
					return Interactability.Disabled;
				}
				if (!component2.HasEntitlement(requiredEntitlement))
				{
					Debug.LogWarning("Rejecting interact because the player doesn't have entitlement " + requiredEntitlement.name);
					return Interactability.Disabled;
				}
			}
		}
		if (!CanBeAffordedByInteractor(activator))
		{
			return Interactability.ConditionsNotMet;
		}
		return Interactability.Available;
	}

	public bool CanBeAffordedByInteractor(Interactor activator)
	{
		int num = cost;
		if (costType == CostTypeIndex.Money)
		{
			num = ((activator.GetComponent<CharacterBody>().GetBuffCount(DLC2Content.Buffs.FreeUnlocks) <= 0) ? (cost * (TeamManager.LongstandingSolitudesInParty() + 1)) : 0);
		}
		return CostTypeCatalog.GetCostTypeDef(costType).IsAffordable(num, activator);
	}

	public void OnInteractionBegin(Interactor activator)
	{
		if (!CanBeAffordedByInteractor(activator))
		{
			return;
		}
		CharacterBody component = activator.GetComponent<CharacterBody>();
		CostTypeDef costTypeDef = CostTypeCatalog.GetCostTypeDef(costType);
		ItemIndex itemIndex = ItemIndex.None;
		ShopTerminalBehavior component2 = GetComponent<ShopTerminalBehavior>();
		if ((bool)component2)
		{
			itemIndex = PickupCatalog.GetPickupDef(component2.CurrentPickupIndex())?.itemIndex ?? ItemIndex.None;
		}
		int num = cost;
		if (costType == CostTypeIndex.Money)
		{
			if (activator.GetComponent<CharacterBody>().GetBuffCount(DLC2Content.Buffs.FreeUnlocks) > 0 && !GetComponent<MultiShopController>())
			{
				num = 0;
				activator.GetComponent<CharacterBody>().RemoveBuff(DLC2Content.Buffs.FreeUnlocks);
				Util.PlaySound("Play_item_proc_onLevelUpFreeUnlock_activate", base.gameObject);
			}
			else
			{
				num = cost * (TeamManager.LongstandingSolitudesInParty() + 1);
			}
		}
		if (component.inventory.GetItemCount(DLC2Content.Items.LowerPricedChests) > 0 && saleStarCompatible)
		{
			int itemCount = activator.GetComponent<CharacterBody>().inventory.GetItemCount(DLC2Content.Items.LowerPricedChests);
			int num2 = 0;
			float num3 = 1f;
			if (itemCount == 1)
			{
				num2 = 0;
			}
			else
			{
				for (int i = 1; i < 4; i++)
				{
					switch (i)
					{
					case 1:
						num3 = 30f;
						break;
					case 2:
						num3 = 15f;
						break;
					case 3:
						num3 = 1f;
						break;
					}
					if (itemCount >= 3)
					{
						float num4 = 1f - 1f / ((float)itemCount * 0.05f + 1f);
						if (!Util.CheckRoll(num3 + num4 * 100f, activator.GetComponent<CharacterBody>().master))
						{
							break;
						}
						num2 = i;
					}
					else
					{
						if (!Util.CheckRoll(num3, activator.GetComponent<CharacterBody>().master))
						{
							break;
						}
						num2 = i;
					}
				}
			}
			if ((bool)GetComponent<ChestBehavior>())
			{
				GetComponent<ChestBehavior>().dropCount = 2 + num2;
			}
			else if ((bool)GetComponent<RouletteChestController>())
			{
				GetComponent<RouletteChestController>().dropCount = 2 + num2;
			}
			component.inventory.RemoveItem(DLC2Content.Items.LowerPricedChests, activator.GetComponent<CharacterBody>().inventory.GetItemCount(DLC2Content.Items.LowerPricedChests));
			component.inventory.GiveItem(DLC2Content.Items.LowerPricedChestsConsumed, itemCount);
			CharacterMasterNotificationQueue.SendTransformNotification(component.master, DLC2Content.Items.LowerPricedChests.itemIndex, DLC2Content.Items.LowerPricedChestsConsumed.itemIndex, CharacterMasterNotificationQueue.TransformationType.SaleStarRegen);
			Util.PlaySound("Play_item_proc_lowerPricedChest", base.gameObject);
		}
		CostTypeDef.PayCostResults payCostResults = costTypeDef.PayCost(num, activator, base.gameObject, rng, itemIndex);
		foreach (ItemIndex item in payCostResults.itemsTaken)
		{
			CreateItemTakenOrb(component.corePosition, base.gameObject, item);
			if (item != itemIndex)
			{
				PurchaseInteraction.onItemSpentOnPurchase?.Invoke(this, activator);
			}
		}
		foreach (EquipmentIndex item2 in payCostResults.equipmentTaken)
		{
			PurchaseInteraction.onEquipmentSpentOnPurchase?.Invoke(this, activator, item2);
		}
		IEnumerable<StatDef> statDefsToIncrement = purchaseStatNames.Select(StatDef.Find);
		StatManager.OnPurchase(component, costType, statDefsToIncrement);
		onPurchase.Invoke(activator);
		lastActivator = activator;
	}

	[Server]
	public static void CreateItemTakenOrb(Vector3 effectOrigin, GameObject targetObject, ItemIndex itemIndex)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.PurchaseInteraction::CreateItemTakenOrb(UnityEngine.Vector3,UnityEngine.GameObject,RoR2.ItemIndex)' called on client");
			return;
		}
		GameObject effectPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/OrbEffects/ItemTakenOrbEffect");
		EffectData effectData = new EffectData
		{
			origin = effectOrigin,
			genericFloat = 1.5f,
			genericUInt = (uint)(itemIndex + 1)
		};
		effectData.SetNetworkedObjectReference(targetObject);
		EffectManager.SpawnEffect(effectPrefab, effectData, transmit: true);
	}

	public bool ShouldDisplayHologram(GameObject viewer)
	{
		return available;
	}

	public GameObject GetHologramContentPrefab()
	{
		return LegacyResourcesAPI.Load<GameObject>("Prefabs/CostHologramContent");
	}

	private void CacheViewerBody(Transform _viewer)
	{
		if (!(_viewer == null) && (!viewerBody || !(viewerTransform == _viewer)) && _viewer.TryGetComponent<CharacterBody>(out var component))
		{
			viewerTransform = _viewer;
			viewerBody = component;
		}
	}

	public void ScaleCost(float scalar)
	{
		Networkcost = (int)Mathf.Floor((float)cost * scalar);
	}

	public void UpdateHologramContent(GameObject hologramContentObject, Transform viewer)
	{
		CostHologramContent component = hologramContentObject.GetComponent<CostHologramContent>();
		GetComponent<HologramProjector>();
		CacheViewerBody(viewer);
		if (!component)
		{
			return;
		}
		if ((bool)viewerBody && costType.Equals(CostTypeIndex.Money))
		{
			if (TeamManager.LongstandingSolitudesInParty() > 0)
			{
				component.displayValue = cost * (TeamManager.LongstandingSolitudesInParty() + 1);
			}
			else
			{
				component.displayValue = cost;
			}
		}
		else
		{
			component.displayValue = cost;
		}
		component.costType = costType;
	}

	public bool ShouldIgnoreSpherecastForInteractibility(Interactor activator)
	{
		return false;
	}

	public bool ShouldShowOnScanner()
	{
		return available;
	}

	public bool ShouldProximityHighlight()
	{
		return shouldProximityHighlight;
	}

	private void OnEnable()
	{
		InstanceTracker.Add(this);
	}

	private void OnDisable()
	{
		InstanceTracker.Remove(this);
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		TeleporterInteraction.onTeleporterBeginChargingGlobal += OnTeleporterBeginCharging;
	}

	private static void OnTeleporterBeginCharging(TeleporterInteraction teleporterInteraction)
	{
		if (!NetworkServer.active)
		{
			return;
		}
		foreach (PurchaseInteraction instances in InstanceTracker.GetInstancesList<PurchaseInteraction>())
		{
			if (instances.setUnavailableOnTeleporterActivated)
			{
				instances.SetAvailable(newAvailable: false);
				instances.CancelInvoke("SetUnavailableTemporarily");
			}
		}
	}

	public IInspectInfoProvider GetInspectInfoProvider()
	{
		if (currentInspectIndex < cachedInspectInfoProviders.Length)
		{
			return cachedInspectInfoProviders[currentInspectIndex];
		}
		return null;
	}

	public void SetInspectInfoProvider(int newInspectIndex)
	{
		currentInspectIndex = newInspectIndex;
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.Write(displayNameToken);
			writer.Write(contextToken);
			writer.Write(available);
			writer.WritePackedUInt32((uint)cost);
			writer.Write(lockGameObject);
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
			writer.Write(displayNameToken);
		}
		if ((base.syncVarDirtyBits & 2u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(contextToken);
		}
		if ((base.syncVarDirtyBits & 4u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(available);
		}
		if ((base.syncVarDirtyBits & 8u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.WritePackedUInt32((uint)cost);
		}
		if ((base.syncVarDirtyBits & 0x10u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(lockGameObject);
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
			displayNameToken = reader.ReadString();
			contextToken = reader.ReadString();
			available = reader.ReadBoolean();
			cost = (int)reader.ReadPackedUInt32();
			___lockGameObjectNetId = reader.ReadNetworkId();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			displayNameToken = reader.ReadString();
		}
		if (((uint)num & 2u) != 0)
		{
			contextToken = reader.ReadString();
		}
		if (((uint)num & 4u) != 0)
		{
			available = reader.ReadBoolean();
		}
		if (((uint)num & 8u) != 0)
		{
			cost = (int)reader.ReadPackedUInt32();
		}
		if (((uint)num & 0x10u) != 0)
		{
			lockGameObject = reader.ReadGameObject();
		}
	}

	public override void PreStartClient()
	{
		if (!___lockGameObjectNetId.IsEmpty())
		{
			NetworklockGameObject = ClientScene.FindLocalObject(___lockGameObjectNetId);
		}
	}
}
