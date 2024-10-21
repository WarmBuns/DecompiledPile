using System;
using System.Runtime.InteropServices;
using RoR2.Artifacts;
using Unity;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class PickupDropletController : NetworkBehaviour
{
	[NonSerialized]
	[SyncVar]
	public PickupIndex pickupIndex = PickupIndex.none;

	private bool alive = true;

	private GenericPickupController.CreatePickupInfo createPickupInfo;

	private static GameObject commandCubePrefab;

	private static bool isCommandChest;

	private static GameObject pickupDropletPrefab;

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

	public static void CreatePickupDroplet(PickupIndex pickupIndex, Vector3 position, Vector3 velocity)
	{
		GenericPickupController.CreatePickupInfo pickupInfo = default(GenericPickupController.CreatePickupInfo);
		pickupInfo.rotation = Quaternion.identity;
		pickupInfo.pickupIndex = pickupIndex;
		pickupInfo.position = position;
		CreatePickupDroplet(pickupInfo, position, velocity);
	}

	public static void CreatePickupDroplet(GenericPickupController.CreatePickupInfo pickupInfo, Vector3 position, Vector3 velocity)
	{
		if (CommandArtifactManager.IsCommandArtifactEnabled)
		{
			pickupInfo.artifactFlag |= GenericPickupController.PickupArtifactFlag.COMMAND;
		}
		GameObject obj = UnityEngine.Object.Instantiate(pickupDropletPrefab, position, Quaternion.identity);
		PickupDropletController component = obj.GetComponent<PickupDropletController>();
		if ((bool)component)
		{
			component.createPickupInfo = pickupInfo;
			component.NetworkpickupIndex = pickupInfo.pickupIndex;
		}
		Rigidbody component2 = obj.GetComponent<Rigidbody>();
		component2.velocity = velocity;
		component2.AddTorque(UnityEngine.Random.Range(150f, 120f) * UnityEngine.Random.onUnitSphere);
		NetworkServer.Spawn(obj);
	}

	[InitDuringStartup]
	private static void Init()
	{
		LegacyResourcesAPI.LoadAsyncCallback("Prefabs/NetworkedObjects/PickupDroplet", delegate(GameObject operationResult)
		{
			pickupDropletPrefab = operationResult;
		});
	}

	public static void IfCommandChestSpawned(bool value, PickupIndex pickupIndex, Vector3 position, Vector3 velocity)
	{
		isCommandChest = value;
		commandCubePrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/CommandCube");
		CreatePickupDroplet(pickupIndex, position, velocity);
	}

	public void OnCollisionEnter(Collision collision)
	{
		if (NetworkServer.active && alive)
		{
			alive = false;
			createPickupInfo.position = base.transform.position;
			CreatePickup();
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	private void Start()
	{
		GameObject gameObject = PickupCatalog.GetPickupDef(pickupIndex)?.dropletDisplayPrefab;
		if ((bool)gameObject)
		{
			UnityEngine.Object.Instantiate(gameObject, base.transform);
		}
	}

	private void CreatePickup()
	{
		if (createPickupInfo.artifactFlag.HasFlag(GenericPickupController.PickupArtifactFlag.COMMAND) && !createPickupInfo.artifactFlag.HasFlag(GenericPickupController.PickupArtifactFlag.DELUSION))
		{
			PickupDef pickupDef = PickupCatalog.GetPickupDef(createPickupInfo.pickupIndex);
			if (pickupDef != null && (pickupDef.itemIndex != ItemIndex.None || pickupDef.equipmentIndex != EquipmentIndex.None || pickupDef.itemTier != ItemTier.NoTier))
			{
				CreateCommandCube();
				return;
			}
		}
		GenericPickupController.CreatePickup(in createPickupInfo);
	}

	private void CreateCommandCube()
	{
		GameObject obj = UnityEngine.Object.Instantiate(CommandArtifactManager.commandCubePrefab, createPickupInfo.position, createPickupInfo.rotation);
		obj.GetComponent<PickupIndexNetworker>().NetworkpickupIndex = createPickupInfo.pickupIndex;
		PickupPickerController component = obj.GetComponent<PickupPickerController>();
		component.SetOptionsFromPickupForCommandArtifact(createPickupInfo.pickupIndex);
		component.chestGeneratedFrom = createPickupInfo.chest;
		NetworkServer.Spawn(obj);
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			GeneratedNetworkCode._WritePickupIndex_None(writer, pickupIndex);
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
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			pickupIndex = GeneratedNetworkCode._ReadPickupIndex_None(reader);
		}
	}

	public override void PreStartClient()
	{
	}
}
