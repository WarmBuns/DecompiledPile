namespace RoR2.WwiseUtils;

public static class CommonWwiseIds
{
	public static uint none;

	public static uint alive;

	public static uint bossfight;

	public static uint dead;

	public static uint gameplay;

	public static uint menu;

	public static uint main;

	public static uint logbook;

	public static uint secretLevel;

	[InitDuringStartup]
	public static void Init()
	{
		Assign(ref none, "None");
		Assign(ref alive, "alive");
		Assign(ref dead, "dead");
		Assign(ref bossfight, "Bossfight");
		Assign(ref gameplay, "Gameplay");
		Assign(ref menu, "Menu");
		Assign(ref main, "Main");
		Assign(ref logbook, "Logbook");
		Assign(ref secretLevel, "SecretLevel");
		static void Assign(ref uint field, string name)
		{
			field = AkSoundEngine.GetIDFromString(name);
		}
	}
}
