namespace EntityStates.Treebot.Weapon;

public class ChargePlantSonicBoom : ChargeSonicBoom
{
	protected override EntityState GetNextState()
	{
		return new FirePlantSonicBoom();
	}
}
