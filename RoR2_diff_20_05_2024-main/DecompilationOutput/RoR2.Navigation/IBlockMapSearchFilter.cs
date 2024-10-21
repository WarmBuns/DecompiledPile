namespace RoR2.Navigation;

public interface IBlockMapSearchFilter<TItem>
{
	bool CheckItem(TItem item, ref bool shouldFinish);
}
