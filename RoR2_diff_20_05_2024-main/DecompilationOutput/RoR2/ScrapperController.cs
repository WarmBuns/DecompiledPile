using EntityStates.Scrapper;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class ScrapperController : NetworkBehaviour
{
	public PickupPickerController pickupPickerController;

	public EntityStateMachine esm;

	public int maxItemsToScrapAtATime = 10;

	private Interactor interactor;

	public ItemIndex lastScrappedItemIndex { get; private set; }

	public int itemsEaten { get; set; }

	private void Start()
	{
	}

	private void Update()
	{
	}

	[Server]
	public void AssignPotentialInteractor(Interactor potentialInteractor)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.ScrapperController::AssignPotentialInteractor(RoR2.Interactor)' called on client");
		}
		else
		{
			interactor = potentialInteractor;
		}
	}

	[Server]
	public void BeginScrapping(int intPickupIndex)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.ScrapperController::BeginScrapping(System.Int32)' called on client");
			return;
		}
		itemsEaten = 0;
		PickupDef pickupDef = PickupCatalog.GetPickupDef(new PickupIndex(intPickupIndex));
		if (pickupDef != null && (bool)interactor)
		{
			lastScrappedItemIndex = pickupDef.itemIndex;
			CharacterBody component = interactor.GetComponent<CharacterBody>();
			if ((bool)component && (bool)component.inventory)
			{
				int num = Mathf.Min(maxItemsToScrapAtATime, component.inventory.GetItemCount(pickupDef.itemIndex));
				if (num > 0)
				{
					component.inventory.RemoveItem(pickupDef.itemIndex, num);
					itemsEaten += num;
					for (int i = 0; i < num; i++)
					{
						CreateItemTakenOrb(component.corePosition, base.gameObject, pickupDef.itemIndex);
					}
				}
			}
		}
		if ((bool)esm)
		{
			esm.SetNextState(new WaitToBeginScrapping());
		}
	}

	[Server]
	public static void CreateItemTakenOrb(Vector3 effectOrigin, GameObject targetObject, ItemIndex itemIndex)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.ScrapperController::CreateItemTakenOrb(UnityEngine.Vector3,UnityEngine.GameObject,RoR2.ItemIndex)' called on client");
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
