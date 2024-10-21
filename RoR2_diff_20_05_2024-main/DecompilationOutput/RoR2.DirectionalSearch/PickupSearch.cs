namespace RoR2.DirectionalSearch;

public class PickupSearch : BaseDirectionalSearch<GenericPickupController, PickupSearchSelector, PickupSearchFilter>
{
	public bool requireTransmutable
	{
		get
		{
			return candidateFilter.requireTransmutable;
		}
		set
		{
			candidateFilter.requireTransmutable = value;
		}
	}

	public PickupSearch()
		: base(default(PickupSearchSelector), default(PickupSearchFilter))
	{
	}

	public PickupSearch(PickupSearchSelector selector, PickupSearchFilter candidateFilter)
		: base(selector, candidateFilter)
	{
	}
}
