using EntityStates;
using EntityStates.Barrel;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class OptionChestBehavior : NetworkBehaviour, IChestBehavior
{
	[SerializeField]
	private PickupDropTable dropTable;

	[SerializeField]
	private Transform dropTransform;

	[SerializeField]
	private float dropUpVelocityStrength = 20f;

	[SerializeField]
	private float dropForwardVelocityStrength = 2f;

	[SerializeField]
	private SerializableEntityStateType openState = new SerializableEntityStateType(typeof(Opening));

	[SerializeField]
	private GameObject pickupPrefab;

	[SerializeField]
	private int numOptions;

	[SerializeField]
	private ItemTier displayTier;

	private PickupIndex[] generatedDrops;

	private Xoroshiro128Plus rng;

	public override int GetNetworkChannel()
	{
		return QosChannelIndex.defaultReliable.intVal;
	}

	[Server]
	public void Roll()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.OptionChestBehavior::Roll()' called on client");
		}
		else
		{
			generatedDrops = dropTable.GenerateUniqueDrops(numOptions, rng);
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
			Roll();
		}
	}

	[Server]
	public void Open()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.OptionChestBehavior::Open()' called on client");
			return;
		}
		EntityStateMachine component = GetComponent<EntityStateMachine>();
		if ((bool)component)
		{
			component.SetNextState(EntityStateCatalog.InstantiateState(ref openState));
		}
	}

	[Server]
	public void ItemDrop()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.OptionChestBehavior::ItemDrop()' called on client");
		}
		else if (generatedDrops != null && generatedDrops.Length != 0)
		{
			GenericPickupController.CreatePickupInfo createPickupInfo = default(GenericPickupController.CreatePickupInfo);
			createPickupInfo.pickerOptions = PickupPickerController.GenerateOptionsFromArray(generatedDrops);
			createPickupInfo.prefabOverride = pickupPrefab;
			createPickupInfo.position = dropTransform.position;
			createPickupInfo.rotation = Quaternion.identity;
			createPickupInfo.pickupIndex = PickupCatalog.FindPickupIndex(displayTier);
			GenericPickupController.CreatePickupInfo pickupInfo = createPickupInfo;
			PickupDropletController.CreatePickupDroplet(pickupInfo, pickupInfo.position, Vector3.up * dropUpVelocityStrength + dropTransform.forward * dropForwardVelocityStrength);
			generatedDrops = null;
		}
	}

	public bool HasRolledPickup(PickupIndex pickupIndex)
	{
		if (generatedDrops != null)
		{
			PickupIndex[] array = generatedDrops;
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i] == pickupIndex)
				{
					return true;
				}
			}
		}
		return false;
	}

	private void UNetVersion()
	{
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
