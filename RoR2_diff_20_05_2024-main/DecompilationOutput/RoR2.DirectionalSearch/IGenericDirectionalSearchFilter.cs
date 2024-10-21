namespace RoR2.DirectionalSearch;

public interface IGenericDirectionalSearchFilter<TSource>
{
	bool PassesFilter(TSource candidateInfo);
}
