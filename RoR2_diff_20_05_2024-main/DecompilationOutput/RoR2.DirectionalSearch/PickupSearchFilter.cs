namespace RoR2.DirectionalSearch;

public struct PickupSearchFilter : IGenericDirectionalSearchFilter<GenericPickupController>
{
	public bool requireTransmutable;

	public bool PassesFilter(GenericPickupController genericPickupController)
	{
		if (requireTransmutable)
		{
			return PickupTransmutationManager.GetAvailableGroupFromPickupIndex(genericPickupController.pickupIndex) != null;
		}
		return true;
	}
}
