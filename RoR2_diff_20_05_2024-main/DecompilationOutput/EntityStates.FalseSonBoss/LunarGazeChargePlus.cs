namespace EntityStates.FalseSonBoss;

public class LunarGazeChargePlus : LunarGazeCharge
{
	public override void OnEnter()
	{
		base.OnEnter();
		fireState = new LunarGazeFirePlus();
	}
}
