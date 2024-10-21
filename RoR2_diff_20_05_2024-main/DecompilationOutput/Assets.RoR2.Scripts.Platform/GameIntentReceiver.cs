namespace Assets.RoR2.Scripts.Platform;

public static class GameIntentReceiver
{
	public static GameIntentAdapter GameIntentAdapter { get; private set; } = new GameIntentAdapter();

	public static void Initialize()
	{
		GameIntentAdapter.Initialize();
	}
}
