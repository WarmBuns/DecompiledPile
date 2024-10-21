using RoR2;
using UnityEngine;

namespace EntityStates.CaptainSupplyDrop;

public class EquipmentRestockMainState : BaseMainState
{
	[SerializeField]
	public float activationCost;

	protected override bool shouldShowEnergy => true;

	protected override string GetContextString(Interactor activator)
	{
		return Language.GetString("CAPTAIN_SUPPLY_EQUIPMENT_RESTOCK_INTERACTION");
	}

	protected override Interactability GetInteractability(Interactor activator)
	{
		CharacterBody component = activator.GetComponent<CharacterBody>();
		Inventory inventory;
		if (!component || !(inventory = component.inventory))
		{
			return Interactability.Disabled;
		}
		if (activationCost >= energyComponent.energy)
		{
			return Interactability.ConditionsNotMet;
		}
		if (inventory.GetEquipmentRestockableChargeCount(inventory.activeEquipmentSlot) <= 0)
		{
			return Interactability.ConditionsNotMet;
		}
		return Interactability.Available;
	}

	protected override void OnInteractionBegin(Interactor activator)
	{
		energyComponent.TakeEnergy(activationCost);
		Inventory inventory = activator.GetComponent<CharacterBody>().inventory;
		inventory.RestockEquipmentCharges(inventory.activeEquipmentSlot, 1);
	}
}
