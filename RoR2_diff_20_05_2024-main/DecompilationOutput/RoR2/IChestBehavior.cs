namespace RoR2;

public interface IChestBehavior
{
	bool HasRolledPickup(PickupIndex pickupIndex);

	void Roll();

	void ItemDrop();
}
