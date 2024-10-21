using System.Runtime.InteropServices;
using HG;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class MultiShopController : NetworkBehaviour, IHologramContentProvider
{
	[Tooltip("The shop terminal prefab to instantiate.")]
	public GameObject terminalPrefab;

	[Tooltip("The positions at which to instantiate shop terminals.")]
	public Transform[] terminalPositions;

	[Tooltip("The number of terminals guaranteed to have their item revealed")]
	public int revealCount = 1;

	[Tooltip("The percentage chance that terminals after the reveal count will be hidden")]
	public float hiddenChance = 0.2f;

	[Header("Deprecated")]
	[Tooltip("The tier of items to drop")]
	public ItemTier itemTier;

	public bool doEquipmentInstead;

	[Tooltip("Whether or not there's a chance the item contents are replaced with a '?'")]
	private bool hideDisplayContent = true;

	private bool[] doCloseOnTerminalPurchase;

	private GameObject[] _terminalGameObjects;

	public ReadOnlyArray<GameObject> terminalGameObjects;

	[SyncVar]
	private bool available = true;

	public int baseCost;

	public CostTypeIndex costType;

	[SyncVar]
	private int cost;

	private Xoroshiro128Plus rng;

	private static int kRpcRpcSetTerminalPingable;

	public bool Networkavailable
	{
		get
		{
			return available;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref available, 1u);
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
			SetSyncVar(value, ref cost, 2u);
		}
	}

	private void Start()
	{
		if (!Run.instance || !NetworkServer.active)
		{
			return;
		}
		rng = new Xoroshiro128Plus(Run.instance.treasureRng.nextUlong);
		CreateTerminals();
		Networkcost = Run.instance.GetDifficultyScaledCost(baseCost);
		if (_terminalGameObjects != null)
		{
			GameObject[] array = _terminalGameObjects;
			for (int i = 0; i < array.Length; i++)
			{
				PurchaseInteraction component = array[i].GetComponent<PurchaseInteraction>();
				component.Networkcost = cost;
				component.costType = costType;
			}
		}
	}

	private void OnDestroy()
	{
		if (_terminalGameObjects != null)
		{
			for (int num = _terminalGameObjects.Length - 1; num >= 0; num--)
			{
				Object.Destroy(_terminalGameObjects[num]);
			}
			_terminalGameObjects = null;
		}
	}

	private void CreateTerminals()
	{
		doCloseOnTerminalPurchase = new bool[terminalPositions.Length];
		_terminalGameObjects = new GameObject[terminalPositions.Length];
		terminalGameObjects = new ReadOnlyArray<GameObject>(_terminalGameObjects);
		for (int i = 0; i < terminalPositions.Length; i++)
		{
			doCloseOnTerminalPurchase[i] = true;
			GameObject gameObject = Object.Instantiate(terminalPrefab, terminalPositions[i].position, terminalPositions[i].rotation);
			_terminalGameObjects[i] = gameObject;
			ShopTerminalBehavior component = gameObject.GetComponent<ShopTerminalBehavior>();
			component.serverMultiShopController = this;
			if (!component.selfGeneratePickup)
			{
				PickupIndex newPickupIndex = PickupIndex.none;
				if (doEquipmentInstead)
				{
					newPickupIndex = rng.NextElementUniform(Run.instance.availableEquipmentDropList);
				}
				else
				{
					switch (itemTier)
					{
					case ItemTier.Tier1:
						newPickupIndex = rng.NextElementUniform(Run.instance.availableTier1DropList);
						break;
					case ItemTier.Tier2:
						newPickupIndex = rng.NextElementUniform(Run.instance.availableTier2DropList);
						break;
					case ItemTier.Tier3:
						newPickupIndex = rng.NextElementUniform(Run.instance.availableTier3DropList);
						break;
					case ItemTier.Lunar:
						newPickupIndex = rng.NextElementUniform(Run.instance.availableLunarCombinedDropList);
						break;
					case ItemTier.VoidTier1:
						newPickupIndex = rng.NextElementUniform(Run.instance.availableVoidTier1DropList);
						break;
					case ItemTier.VoidTier2:
						newPickupIndex = rng.NextElementUniform(Run.instance.availableVoidTier2DropList);
						break;
					case ItemTier.VoidTier3:
						newPickupIndex = rng.NextElementUniform(Run.instance.availableVoidTier3DropList);
						break;
					case ItemTier.VoidBoss:
						newPickupIndex = rng.NextElementUniform(Run.instance.availableVoidBossDropList);
						break;
					}
				}
				bool newHidden = hideDisplayContent && i >= revealCount && rng.nextNormalizedFloat < hiddenChance;
				component.SetPickupIndex(newPickupIndex, newHidden);
			}
			else
			{
				component.SetHidden(i >= revealCount && rng.nextNormalizedFloat < hiddenChance);
			}
			NetworkServer.Spawn(gameObject);
		}
		GameObject[] array = _terminalGameObjects;
		foreach (GameObject gameObject2 in array)
		{
			PurchaseInteraction purchaseInteraction = gameObject2.GetComponent<PurchaseInteraction>();
			purchaseInteraction.onPurchase.AddListener(delegate(Interactor interactor)
			{
				OnPurchase(interactor, purchaseInteraction);
			});
		}
	}

	private void OnPurchase(Interactor interactor, PurchaseInteraction purchaseInteraction)
	{
		bool flag = false;
		Networkavailable = false;
		for (int i = 0; i < _terminalGameObjects.Length; i++)
		{
			GameObject gameObject = _terminalGameObjects[i];
			PurchaseInteraction component = gameObject.GetComponent<PurchaseInteraction>();
			if ((object)purchaseInteraction == component)
			{
				component.Networkavailable = false;
				gameObject.GetComponent<ShopTerminalBehavior>().SetNoPickup();
				CallRpcSetTerminalPingable(gameObject, value: false);
				flag = doCloseOnTerminalPurchase[i];
			}
			Networkavailable = available || component.available;
		}
		if (flag)
		{
			Networkavailable = false;
			GameObject[] array = _terminalGameObjects;
			foreach (GameObject gameObject2 in array)
			{
				gameObject2.GetComponent<PurchaseInteraction>().Networkavailable = false;
				gameObject2.GetComponent<ShopTerminalBehavior>().SetNoPickup();
				CallRpcSetTerminalPingable(gameObject2, value: false);
			}
		}
	}

	[ClientRpc]
	private void RpcSetTerminalPingable(GameObject terminalGO, bool value)
	{
		NetworkIdentity component = terminalGO.GetComponent<NetworkIdentity>();
		if ((bool)component)
		{
			component.isPingable = value;
		}
	}

	public bool ShouldDisplayHologram(GameObject viewer)
	{
		if (available)
		{
			if (cost != 0)
			{
				return costType != CostTypeIndex.None;
			}
			return false;
		}
		return false;
	}

	public GameObject GetHologramContentPrefab()
	{
		return LegacyResourcesAPI.Load<GameObject>("Prefabs/CostHologramContent");
	}

	public void UpdateHologramContent(GameObject hologramContentObject, Transform viewer)
	{
		CostHologramContent component = hologramContentObject.GetComponent<CostHologramContent>();
		if (!component)
		{
			return;
		}
		if ((bool)viewer && costType.Equals(CostTypeIndex.Money))
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
		component.costType = costType;
	}

	public void SetCloseOnTerminalPurchase(PurchaseInteraction terminalPurchaseInteraction, bool doCloseMultiShop)
	{
		for (int i = 0; i < _terminalGameObjects.Length; i++)
		{
			if ((object)_terminalGameObjects[i].GetComponent<PurchaseInteraction>() == terminalPurchaseInteraction)
			{
				doCloseOnTerminalPurchase[i] = doCloseMultiShop;
			}
		}
	}

	private void UNetVersion()
	{
	}

	protected static void InvokeRpcRpcSetTerminalPingable(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcSetTerminalPingable called on server.");
		}
		else
		{
			((MultiShopController)obj).RpcSetTerminalPingable(reader.ReadGameObject(), reader.ReadBoolean());
		}
	}

	public void CallRpcSetTerminalPingable(GameObject terminalGO, bool value)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcSetTerminalPingable called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcSetTerminalPingable);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.Write(terminalGO);
		networkWriter.Write(value);
		SendRPCInternal(networkWriter, 0, "RpcSetTerminalPingable");
	}

	static MultiShopController()
	{
		kRpcRpcSetTerminalPingable = -474614679;
		NetworkBehaviour.RegisterRpcDelegate(typeof(MultiShopController), kRpcRpcSetTerminalPingable, InvokeRpcRpcSetTerminalPingable);
		NetworkCRC.RegisterBehaviour("MultiShopController", 0);
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.Write(available);
			writer.WritePackedUInt32((uint)cost);
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
			writer.Write(available);
		}
		if ((base.syncVarDirtyBits & 2u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.WritePackedUInt32((uint)cost);
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
			available = reader.ReadBoolean();
			cost = (int)reader.ReadPackedUInt32();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			available = reader.ReadBoolean();
		}
		if (((uint)num & 2u) != 0)
		{
			cost = (int)reader.ReadPackedUInt32();
		}
	}

	public override void PreStartClient()
	{
	}
}
